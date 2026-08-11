'use strict';

// Minimal DOM + WebView host harness for the four production scripts under
// src/TranslateReader/Resources/Raw/wwwroot/js. No external dependency: node:vm
// runs each production file in a fresh context whose global IS `window`, and the
// file is compiled with `filename` pointing at the real path so that
// --experimental-test-coverage attributes coverage to the production file.

const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const SCRIPT_DIR = path.resolve(
    __dirname, '..', '..', 'src', 'TranslateReader', 'Resources', 'Raw', 'wwwroot', 'js');

const VOID_TAGS = new Set(['br', 'hr', 'img', 'input', 'link', 'meta']);
const TAG_RE = /<(\/)?([a-zA-Z][a-zA-Z0-9-]*)((?:\s+[a-zA-Z-][a-zA-Z0-9-]*(?:="[^"]*")?)*)\s*(\/)?>/g;
const ATTR_RE = /([a-zA-Z-][a-zA-Z0-9-]*)(?:="([^"]*)")?/g;
const SELECTOR_PART_RE = /\.([\w-]+)|\[([\w-]+)(?:="([^"]*)")?\]/g;
const TEXT_NODE = 3;
const ELEMENT_NODE = 1;
const COMMENT_NODE = 8;
const SVG_NS = 'http://www.w3.org/2000/svg';

function toCamelCase(name) {
    return name.replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
}

class FakeNode {
    constructor(ownerDocument) {
        this.ownerDocument = ownerDocument;
        this.parentNode = null;
        this.childNodes = [];
    }

    get firstChild() {
        return this.childNodes.length > 0 ? this.childNodes[0] : null;
    }

    appendChild(node) {
        this.#adopt(node);
        this.childNodes.push(node);
        return node;
    }

    prepend(node) {
        this.#adopt(node);
        this.childNodes.unshift(node);
        return node;
    }

    removeChild(node) {
        const index = this.childNodes.indexOf(node);
        if (index >= 0) {
            this.childNodes.splice(index, 1);
            node.parentNode = null;
        }
        return node;
    }

    remove() {
        if (this.parentNode) {
            this.parentNode.removeChild(this);
        }
    }

    // Standard Node.contains: true for the node itself or any descendant, walking up via
    // parentNode. snippets.js uses this to find which snippet root owns an arbitrary element
    // without hardcoding that root's own selector anywhere outside _snippetRoots itself.
    contains(node) {
        for (let current = node; current !== null; current = current.parentNode) {
            if (current === this) {
                return true;
            }
        }
        return false;
    }

    #adopt(node) {
        if (node.parentNode) {
            node.parentNode.removeChild(node);
        }
        node.parentNode = this;
    }
}

class FakeText extends FakeNode {
    constructor(ownerDocument, text) {
        super(ownerDocument);
        this.nodeType = TEXT_NODE;
        this.data = String(text);
    }

    get textContent() {
        return this.data;
    }

    set textContent(value) {
        this.data = String(value);
    }

    // Mirrors Node.splitText: truncates this node's data to `offset` and inserts a NEW text node
    // holding the rest as the very next sibling, still attached to the same parent. snippets.js uses
    // this to carve period spans out of a paragraph's text nodes around inline markup without ever
    // serializing and reparsing it (csharp.md §4: book HTML is untrusted input). Spec-faithful on the
    // bound that matters here: an `offset` past the node's own length throws an IndexSizeError
    // DOMException, exactly like a real WebView (Chrome/WebView2) does — a silently clamping fake
    // here previously masked an entire class of production bug (B-1) where a caller computed an
    // offset against the wrong node's length.
    splitText(offset) {
        if (offset > this.data.length) {
            throw new DOMException(
                `Failed to execute 'splitText' on 'Text': The offset ${offset} is larger than the Text node's length.`,
                'IndexSizeError');
        }
        const tail = new FakeText(this.ownerDocument, this.data.slice(offset));
        this.data = this.data.slice(0, offset);
        if (this.parentNode) {
            const siblings = this.parentNode.childNodes;
            siblings.splice(siblings.indexOf(this) + 1, 0, tail);
            tail.parentNode = this.parentNode;
        }
        return tail;
    }

    cloneNode() {
        return new FakeText(this.ownerDocument, this.data);
    }
}

// Mirrors Comment: has `.data` like a Text node, but deliberately no `tagName` (so production code's
// `node.tagName` check still treats it as "not an element") and no `splitText` (so
// `_isSplittableText` in snippets.js correctly tells it apart from a genuine Text node — B-3). No
// `textContent` getter on `collectText`'s traversal path is needed to make a parent skip it: an
// empty `childNodes` array already makes the recursive collectText below contribute nothing for it,
// exactly like a real DOM comment contributes nothing to its parent's textContent.
class FakeComment extends FakeNode {
    constructor(ownerDocument, data) {
        super(ownerDocument);
        this.nodeType = COMMENT_NODE;
        this.data = String(data);
    }

    get textContent() {
        return this.data;
    }

    set textContent(value) {
        this.data = String(value);
    }

    cloneNode() {
        return new FakeComment(this.ownerDocument, this.data);
    }
}

class FakeElement extends FakeNode {
    constructor(ownerDocument, tagName) {
        super(ownerDocument);
        this.nodeType = ELEMENT_NODE;
        this.tagName = tagName.toUpperCase();
        this.id = '';
        this.className = '';
        this.style = {};
        this.dataset = {};
        this.attributes = new Map();
        this.offsetWidth = 0;
        this.offsetHeight = 0;
        this.offsetTop = 0;
        this.offsetLeft = 0;
        this.scrollWidth = 0;
        this.rect = { top: 0, left: 0, right: 0, bottom: 0, width: 0, height: 0 };
        this.listeners = new Map();
    }

    setAttribute(name, value) {
        if (name.startsWith('data-')) {
            this.dataset[toCamelCase(name.slice(5))] = value;
            return;
        }
        this.attributes.set(name, value);
        if (name === 'id') {
            this.id = value;
        } else if (name === 'class') {
            this._setClassName(value);
        }
    }

    // Overridden by FakeSvgElement, whose className is not a plain string even in a real DOM.
    _setClassName(value) {
        this.className = value;
    }

    getAttribute(name) {
        return this.attributes.has(name) ? this.attributes.get(name) : null;
    }

    getBoundingClientRect() {
        return this.rect;
    }

    // Multi-line spans (a period wrapping across a line break) need one rect per line; a single
    // rect would collapse the wrap and break the blob's line-grouping math in snippets.js.
    getClientRects() {
        return Array.isArray(this.rects) ? this.rects : [this.rect];
    }

    // Selector groups reuse the same parser/matcher as querySelectorAll, so `closest` fails CLOSED
    // on a selector the harness cannot read instead of silently reporting "no ancestor matches".
    closest(selector) {
        const parsedParts = parseSelector(selector);
        for (let node = this; node !== null; node = node.parentNode) {
            if (node.nodeType === ELEMENT_NODE && matchesAnyPart(node, parsedParts)) {
                return node;
            }
        }
        return null;
    }

    addEventListener(type, handler) {
        addListener(this.listeners, type, handler);
    }

    get textContent() {
        return collectText(this);
    }

    set textContent(value) {
        detachChildren(this);
        this.appendChild(new FakeText(this.ownerDocument, value));
    }

    get innerHTML() {
        return serialize(this);
    }

    set innerHTML(html) {
        detachChildren(this);
        parseInto(this, String(html));
    }

    querySelectorAll(selector) {
        return matchDescendants(this, selector);
    }

    querySelector(selector) {
        const found = this.querySelectorAll(selector);
        return found.length > 0 ? found[0] : null;
    }

    // Mirrors Node.cloneNode: a NEW element of the same tag carrying the same attributes (real ones
    // via setAttribute, `data-*` ones via dataset directly — setAttribute never populates
    // `.attributes` for those) but never the original's event listeners, exactly like a real DOM
    // clone. snippets.js only ever clones shallow (`deep` falsy) to divide a book-content element
    // that turned out to contain a sentence boundary (untrusted HTML, csharp.md §4) — `this.constructor`
    // keeps this correct for the FakeSvgElement subclass too, should a caller ever clone one of those.
    cloneNode(deep) {
        const clone = new this.constructor(this.ownerDocument, this.tagName);
        for (const [name, value] of this.attributes) {
            clone.setAttribute(name, value);
        }
        Object.assign(clone.dataset, this.dataset);
        if (deep) {
            for (const child of this.childNodes) {
                clone.appendChild(child.cloneNode(true));
            }
        }
        return clone;
    }
}

// Mimics the one SVG hazard that matters to snippets.js (B-2): a real SVGElement's className is a
// read-only SVGAnimatedString object, never a plain string, so code that calls `.indexOf` on it
// throws where the same call on an HTML element's className would work fine. getAttribute('class')
// stays a normal string on both, exactly like a real DOM, which is what lets production code read
// classes safely regardless of which kind of element it is holding.
class FakeSvgElement extends FakeElement {
    constructor(ownerDocument, tagName) {
        super(ownerDocument, tagName);
        this.className = { baseVal: '', animVal: '' };
    }

    _setClassName(value) {
        this.className.baseVal = value;
        this.className.animVal = value;
    }
}

class FakeDocument {
    constructor(readyState) {
        this.readyState = readyState;
        this.listeners = new Map();
        this.documentElement = new FakeElement(this, 'html');
        this.body = new FakeElement(this, 'body');
        this.documentElement.appendChild(this.body);
    }

    createElement(tagName) {
        return new FakeElement(this, tagName);
    }

    // Deliberately hands back a FakeSvgElement for the SVG namespace instead of a plain FakeElement,
    // so a node built this way exercises the same className shape a real WebView produces (B-2)
    // rather than silently degrading to a harmless plain-string element.
    createElementNS(namespaceUri, tagName) {
        return namespaceUri === SVG_NS ? new FakeSvgElement(this, tagName) : new FakeElement(this, tagName);
    }

    createTextNode(text) {
        return new FakeText(this, text);
    }

    createComment(data) {
        return new FakeComment(this, data);
    }

    getElementById(id) {
        for (const element of descendantElements(this.documentElement)) {
            if (element.id === id) {
                return element;
            }
        }
        return null;
    }

    querySelectorAll(selector) {
        return matchDescendants(this.documentElement, selector);
    }

    querySelector(selector) {
        const found = this.querySelectorAll(selector);
        return found.length > 0 ? found[0] : null;
    }

    // Later elements paint on top of earlier ones in document order, so the LAST rect containing
    // the point is what a real hit-test at (x, y) would return — needed for pointermove drag-select.
    elementFromPoint(x, y) {
        let found = null;
        for (const element of descendantElements(this.documentElement)) {
            const rect = element.rect;
            if (rect.width > 0 && rect.height > 0 &&
                x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom) {
                found = element;
            }
        }
        return found;
    }

    addEventListener(type, handler) {
        addListener(this.listeners, type, handler);
    }
}

function addListener(map, type, handler) {
    if (!map.has(type)) {
        map.set(type, []);
    }
    map.get(type).push(handler);
}

function detachChildren(element) {
    for (const child of element.childNodes) {
        child.parentNode = null;
    }
    element.childNodes = [];
}

function collectText(node) {
    if (node.nodeType === TEXT_NODE) {
        return node.data;
    }
    let text = '';
    for (const child of node.childNodes) {
        text += collectText(child);
    }
    return text;
}

function serialize(node) {
    let html = '';
    for (const child of node.childNodes) {
        if (child.nodeType === TEXT_NODE) {
            html += child.data;
            continue;
        }
        const tag = child.tagName.toLowerCase();
        html += `<${tag}>${serialize(child)}</${tag}>`;
    }
    return html;
}

function parseAttributes(element, source) {
    ATTR_RE.lastIndex = 0;
    let match = ATTR_RE.exec(source);
    while (match !== null) {
        element.setAttribute(match[1], match[2] === undefined ? '' : match[2]);
        match = ATTR_RE.exec(source);
    }
}

function parseInto(parent, html) {
    const stack = [parent];
    let cursor = 0;
    TAG_RE.lastIndex = 0;
    let match = TAG_RE.exec(html);
    while (match !== null) {
        const current = stack[stack.length - 1];
        if (match.index > cursor) {
            current.appendChild(new FakeText(parent.ownerDocument, html.slice(cursor, match.index)));
        }
        cursor = TAG_RE.lastIndex;
        if (match[1]) {
            if (stack.length > 1) {
                stack.pop();
            }
        } else {
            const element = parent.ownerDocument.createElement(match[2]);
            parseAttributes(element, match[3]);
            current.appendChild(element);
            if (!match[4] && !VOID_TAGS.has(match[2].toLowerCase())) {
                stack.push(element);
            }
        }
        match = TAG_RE.exec(html);
    }
    if (cursor < html.length) {
        stack[stack.length - 1].appendChild(new FakeText(parent.ownerDocument, html.slice(cursor)));
    }
}

function* descendantElements(root) {
    for (const child of root.childNodes) {
        if (child.nodeType !== ELEMENT_NODE) {
            continue;
        }
        yield child;
        yield* descendantElements(child);
    }
}

// A comma only separates a selector group at the top level: scroll.js builds
// `[data-chapter-href="<href>"]` from an EPUB href, which is untrusted input, so a href holding a
// comma must stay one selector instead of silently becoming two broken ones.
function splitSelectorGroup(selector) {
    const parts = [];
    let start = 0;
    let depth = 0;
    let quoted = false;
    for (let i = 0; i < selector.length; i++) {
        const character = selector[i];
        if (character === '"') {
            quoted = !quoted;
        } else if (quoted) {
            continue;
        } else if (character === '[') {
            depth++;
        } else if (character === ']') {
            depth--;
        } else if (character === ',' && depth === 0) {
            parts.push(selector.slice(start, i));
            start = i + 1;
        }
    }
    parts.push(selector.slice(start));
    return parts;
}

// Every character has to be accounted for. Skipping over text the parser cannot read produced an
// empty matcher, and an empty matcher matches EVERY element: the harness failed OPEN. scroll.js
// builds `[data-chapter-href="<href>"]` out of an EPUB href, which is untrusted input, so a quote
// in that href yields a selector a real WebView rejects — the harness must reject it too instead of
// green-lighting code that selects the wrong chapter.
function parseSimpleSelector(selector) {
    const trimmed = selector.trim();
    const tagMatch = /^[a-zA-Z][\w-]*/.exec(trimmed);
    const tag = tagMatch === null ? null : tagMatch[0].toUpperCase();
    const rest = tagMatch === null ? trimmed : trimmed.slice(tagMatch[0].length);
    const classes = [];
    const attributes = [];
    SELECTOR_PART_RE.lastIndex = 0;
    let cursor = 0;
    let match = SELECTOR_PART_RE.exec(rest);
    while (match !== null && match.index === cursor) {
        if (match[1] === undefined) {
            attributes.push({ name: match[2], value: match[3] });
        } else {
            classes.push(match[1]);
        }
        cursor = SELECTOR_PART_RE.lastIndex;
        match = SELECTOR_PART_RE.exec(rest);
    }
    if (cursor !== rest.length || (tag === null && cursor === 0)) {
        throw new SyntaxError(`harness cannot parse selector '${selector}'`);
    }
    return { tag, classes, attributes };
}

function parseSelector(selector) {
    return splitSelectorGroup(selector).map(parseSimpleSelector);
}

function readAttribute(element, name) {
    if (name.startsWith('data-')) {
        const key = toCamelCase(name.slice(5));
        return Object.hasOwn(element.dataset, key) ? element.dataset[key] : undefined;
    }
    return element.attributes.has(name) ? element.attributes.get(name) : undefined;
}

// className is a plain string on every element createElement builds, but createElementNS can hand
// back a FakeSvgElement whose className is deliberately not a string (see above). Every selector
// match still needs a class list, so this reads the reflected "class" attribute in that case
// instead of assuming a string — the harness would otherwise throw on ANY selector check once a
// blob's svg node exists anywhere in the tree, not just one asking for an SVG-specific class.
function classTokensOf(element) {
    const raw = typeof element.className === 'string' ? element.className : element.getAttribute('class');
    return typeof raw === 'string' ? raw.split(' ') : [];
}

function matches(element, parsed) {
    if (parsed.tag !== null && element.tagName !== parsed.tag) {
        return false;
    }
    const classes = classTokensOf(element);
    for (const name of parsed.classes) {
        if (!classes.includes(name)) {
            return false;
        }
    }
    for (const attribute of parsed.attributes) {
        const value = readAttribute(element, attribute.name);
        if (value === undefined) {
            return false;
        }
        if (attribute.value !== undefined && value !== attribute.value) {
            return false;
        }
    }
    return true;
}

function matchesAnyPart(element, parsedParts) {
    for (const parsed of parsedParts) {
        if (matches(element, parsed)) {
            return true;
        }
    }
    return false;
}

// ONE walk over the descendants, testing every part of the group against each element. Looping the
// group outside and concatenating would return every `p` and then every `div` instead of document
// order, and translation.js pairs a paragraph with its translation by index into this very list.
function matchDescendants(root, selector) {
    const parsedParts = parseSelector(selector);
    const found = [];
    for (const element of descendantElements(root)) {
        if (matchesAnyPart(element, parsedParts)) {
            found.push(element);
        }
    }
    return found;
}

/**
 * Creates an isolated environment (fresh vm context + fresh fake DOM) in which
 * production scripts can be loaded. `window` and the context global are the same
 * object, so the free variables the scripts share (`_stepW`, `_currentPage`)
 * stay visible across every script loaded through the same environment.
 */
function createEnv(options = {}) {
    const document = new FakeDocument(options.readyState ?? 'complete');
    const context = vm.createContext({});
    vm.runInContext('globalThis.window = globalThis;', context);
    const window = vm.runInContext('globalThis', context);

    const logs = [];
    const timers = [];
    const scrollCalls = [];
    const listeners = new Map();

    window.document = document;
    window.innerWidth = options.innerWidth ?? 800;
    // Real DOM: document.documentElement.clientWidth approximates the viewport width regardless of
    // window.innerWidth's scrollbar quirks. A test simulating a narrow WebView overrides this
    // directly (`env.document.documentElement.clientWidth = ...`) after createEnv returns.
    document.documentElement.clientWidth = options.innerWidth ?? 800;
    window.innerHeight = options.innerHeight ?? 600;
    window.scrollY = 0;
    window.pageYOffset = 0;
    window.console = {
        log: (message) => logs.push({ level: 'log', message: String(message) }),
        warn: (message) => logs.push({ level: 'warn', message: String(message) }),
        error: (message, detail) => logs.push({
            level: 'error',
            message: detail === undefined ? String(message) : `${message} ${detail}`,
        }),
    };
    window.setTimeout = (handler, delay) => {
        timers.push({ handler, delay });
        return timers.length;
    };
    window.scrollTo = (x, y) => scrollCalls.push({ x, y });
    window.addEventListener = (type, handler) => addListener(listeners, type, handler);
    // Real getComputedStyle also reflects stylesheet rules; this harness has no CSS cascade engine,
    // so it only reports what the element's own inline style set, defaulting to 'static' exactly
    // like an unset position does in a real DOM - enough for snippets.js's one use (deciding whether
    // a snippet root needs to claim its own positioning context for the glass layer).
    window.getComputedStyle = (element) => ({ position: element.style.position || 'static' });

    // Absent by default, same as a real Node process: snippets.js guards every use with
    // `typeof ResizeObserver !== 'undefined'`, so most tests exercise that "unsupported host" path
    // for free. A test proving the SUPPORTED path opts in via `{ resizeObserver: true }`.
    const resizeObserverInstances = [];
    if (options.resizeObserver) {
        window.ResizeObserver = class {
            constructor(callback) {
                this.callback = callback;
                this.targets = [];
                this.disconnected = false;
                resizeObserverInstances.push(this);
            }
            observe(target) {
                if (!this.targets.includes(target)) this.targets.push(target);
            }
            unobserve(target) {
                const index = this.targets.indexOf(target);
                if (index >= 0) this.targets.splice(index, 1);
            }
            disconnect() {
                this.targets = [];
                this.disconnected = true;
            }
        };
    }

    // Also absent by default: snippets.js guards every use with `if (document.fonts)`. A test
    // proving the SUPPORTED path opts in via `{ fonts: { ready: <a promise> } }`; omitting `ready`
    // defaults to an already-settled promise.
    if (options.fonts) {
        document.fonts = { ready: options.fonts.ready ?? Promise.resolve() };
    }

    const env = {
        window,
        document,
        logs,
        timers,
        scrollCalls,
        listeners,
        resizeObserverInstances,

        /** Compiles and runs a production script in this environment's context. */
        load(name) {
            const file = path.join(SCRIPT_DIR, name);
            const code = fs.readFileSync(file, 'utf8');
            const script = new vm.Script(code, { filename: file });
            return script.runInContext(context);
        },

        /** Runs every `window` listener registered for `type`. */
        fireWindow(type) {
            for (const handler of listeners.get(type) ?? []) {
                handler({ type });
            }
        },

        /** Runs every `document` listener registered for `type`. */
        fireDocument(type) {
            for (const handler of document.listeners.get(type) ?? []) {
                handler({ type });
            }
        },

        /** Drains the pending fake timers once (queue snapshot, so retries stack up). */
        runTimers() {
            const pending = timers.splice(0, timers.length);
            for (const timer of pending) {
                timer.handler();
            }
            return pending.length;
        },

        /** Appends an element to the fake body. */
        appendToBody(tagName, properties = {}) {
            const element = document.createElement(tagName);
            Object.assign(element, properties);
            document.body.appendChild(element);
            return element;
        },

        /** True when any log entry of `level` contains `fragment`. */
        logged(level, fragment) {
            return logs.some((entry) => entry.level === level && entry.message.includes(fragment));
        },
    };

    return env;
}

module.exports = { createEnv, SCRIPT_DIR };
