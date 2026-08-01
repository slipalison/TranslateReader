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
            this.className = value;
        }
    }

    getAttribute(name) {
        return this.attributes.has(name) ? this.attributes.get(name) : null;
    }

    getBoundingClientRect() {
        return this.rect;
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

    createTextNode(text) {
        return new FakeText(this, text);
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

function parseSimpleSelector(selector) {
    const trimmed = selector.trim();
    const tagMatch = /^[a-zA-Z][\w-]*/.exec(trimmed);
    const tag = tagMatch === null ? null : tagMatch[0].toUpperCase();
    const rest = tagMatch === null ? trimmed : trimmed.slice(tagMatch[0].length);
    const classes = [];
    const attributes = [];
    SELECTOR_PART_RE.lastIndex = 0;
    let match = SELECTOR_PART_RE.exec(rest);
    while (match !== null) {
        if (match[1] === undefined) {
            attributes.push({ name: match[2], value: match[3] });
        } else {
            classes.push(match[1]);
        }
        match = SELECTOR_PART_RE.exec(rest);
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

function matches(element, parsed) {
    if (parsed.tag !== null && element.tagName !== parsed.tag) {
        return false;
    }
    const classes = element.className.split(' ');
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

    const env = {
        window,
        document,
        logs,
        timers,
        scrollCalls,
        listeners,

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
