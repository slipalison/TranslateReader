'use strict';

const test = require('node:test');
const assert = require('node:assert');
const { createEnv } = require('./harness');

const SNIP_HASH_GOLDEN = '9d2a73a5';

// snippets.js reads `_currentMode`, which bridge.js declares: both files must share one context,
// exactly as they do in the WebView.
function loadSnippets(options) {
    const env = createEnv(options);
    env.load('bridge.js');
    env.load('snippets.js');
    return env;
}

function makeParagraph(env, rect) {
    const paragraph = env.document.createElement('p');
    paragraph.dataset.pi = '0';
    paragraph.rect = rect;
    env.document.body.appendChild(paragraph);
    return paragraph;
}

function makeSpan(env, parent, rect) {
    const span = env.document.createElement('span');
    span.rect = rect;
    parent.appendChild(span);
    return span;
}

// A minimal stand-in for _buildPill()'s output, carrying only the parts _fitPill inspects: an
// optional tip note and the primary button with its label span.
function makeFakePill(env) {
    const pill = env.document.createElement('div');
    const tip = env.document.createElement('span');
    tip.className = 'tr-pill-tip';
    pill.appendChild(tip);
    const primary = env.document.createElement('button');
    primary.className = 'tr-pill-primary';
    const label = env.document.createElement('span');
    label.textContent = 'x';
    primary.appendChild(label);
    pill.appendChild(primary);
    return pill;
}

// env.window functions run inside the vm context, so their arrays and objects carry that realm's
// prototypes and deepStrictEqual would reject them against a plain literal here. Array.from called
// on the OUTER Array (and object literals built in this file's arrow functions) rebuild the result
// in this realm before comparing — the same pattern translation.test.js uses.
function splitSentences(env, text) {
    return Array.from(env.window._splitSentences(text));
}

function runsOf(env, indices) {
    return Array.from(env.window._runsOf(indices), (run) => ({ a: run.a, b: run.b }));
}

test('splitSentences: splits two full sentences at the sentence boundary', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(splitSentences(env, 'Ela chegou. Ele saiu.'), ['Ela chegou.', 'Ele saiu.']);
});

test('splitSentences: keeps a closing quote attached to the sentence before splitting', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(
        splitSentences(env, 'Ele disse "oi." Depois saiu.'), ['Ele disse "oi."', 'Depois saiu.']);
});

test('splitSentences: an ellipsis followed by a capital starts a new sentence', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(splitSentences(env, 'Espera... Ele chegou.'), ['Espera...', 'Ele chegou.']);
});

test('splitSentences: an abbreviation followed by a lowercase word is not a sentence boundary', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(splitSentences(env, 'Dr. silva chegou cedo.'), ['Dr. silva chegou cedo.']);
});

test('splitSentences: an empty string yields no sentences', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(splitSentences(env, ''), []);
});

test('splitSentences: a single sentence yields one entry', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(splitSentences(env, 'Um periodo so.'), ['Um periodo so.']);
});

test('runsOf: groups a contiguous set of indices into a single run', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(runsOf(env, [2, 3, 4]), [{ a: 2, b: 4 }]);
});

test('runsOf: a gap between indices starts a new run', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(runsOf(env, [1, 3, 5]), [{ a: 1, b: 1 }, { a: 3, b: 3 }, { a: 5, b: 5 }]);
});

test('runsOf: sorts unordered indices before grouping', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(runsOf(env, [5, 1, 2]), [{ a: 1, b: 2 }, { a: 5, b: 5 }]);
});

test('runsOf: an empty set yields no runs', () => {
    const env = loadSnippets();

    assert.deepStrictEqual(runsOf(env, []), []);
});

test('snipHash: matches the pinned FNV-1a golden vector', () => {
    const env = loadSnippets();

    assert.strictEqual(env.window._snipHash('Ela disse que sim.'), SNIP_HASH_GOLDEN);
});

test('snipHash: is always 8 lowercase hex characters', () => {
    const env = loadSnippets();

    assert.match(env.window._snipHash('x'), /^[0-9a-f]{8}$/);
});

test('snipHash: different text yields a different hash', () => {
    const env = loadSnippets();

    assert.notStrictEqual(
        env.window._snipHash('Ela disse que sim.'), env.window._snipHash('Ele disse que nao.'));
});

// Iter 6 (D-A): mirrors TranslationManager.IsSnippetTranslationTooLong so restoreSnippets can purge
// a row a small model poisoned before this guard existed, without an async digest/RPC round trip.
test('isSnippetTranslationTooLong: flags a response far longer than the original excerpt', () => {
    const env = loadSnippets();

    assert.strictEqual(
        env.window._isSnippetTranslationTooLong('Ela disse que sim.', 'x'.repeat(200)), true);
});

test('isSnippetTranslationTooLong: a plausible translation is not flagged', () => {
    const env = loadSnippets();

    assert.strictEqual(
        env.window._isSnippetTranslationTooLong('Ela disse que sim.', 'She said yes.'), false);
});

test('blob geometry: a single line yields one rounded band', () => {
    const env = loadSnippets();

    const result = env.window._blobPath([{ x1: 0, y1: 0, x2: 100, y2: 30 }], 10);

    assert.strictEqual(
        result,
        'M 10.0 0.0 L 90.0 0.0 Q 100.0 0.0 100.0 10.0 L 100.0 20.0 Q 100.0 30.0 90.0 30.0 L 10.0 30.0 Q 0.0 30.0 0.0 20.0 L 0.0 10.0 Q 0.0 0.0 10.0 0.0 Z');
});

test('blob geometry: two lines join at the midpoint between them', () => {
    const env = loadSnippets();

    const result = env.window._blobPath(
        [{ x1: 0, y1: 0, x2: 100, y2: 30 }, { x1: 0, y1: 34, x2: 80, y2: 64 }], 10);

    assert.ok(result.includes('32.0'));
    assert.ok(!result.includes('30.0'));
    assert.ok(!result.includes('34.0'));
});

test('blob geometry: the radius never exceeds half the band', () => {
    const env = loadSnippets();

    const result = env.window._blobPath([{ x1: 0, y1: 0, x2: 12, y2: 8 }], 10);

    assert.strictEqual(
        result,
        'M 4.0 0.0 L 8.0 0.0 Q 12.0 0.0 12.0 4.0 L 12.0 4.0 Q 12.0 8.0 8.0 8.0 L 4.0 8.0 Q 0.0 8.0 0.0 4.0 L 0.0 4.0 Q 0.0 0.0 4.0 0.0 Z');
});

test('blob geometry: rects thinner than one pixel are ignored', () => {
    const env = loadSnippets();
    const paragraph = makeParagraph(env, { top: 0, left: 0, right: 120, bottom: 50, width: 120, height: 50 });
    const validSpan = makeSpan(
        env, paragraph, { top: 8, left: 8, right: 108, bottom: 38, width: 100, height: 30 });
    const thinSpan = makeSpan(
        env, paragraph, { top: 8, left: 108, right: 108.5, bottom: 38, width: 0.5, height: 30 });

    const withThin = env.window._blobFromEls([validSpan, thinSpan]);
    const withoutThin = env.window._blobFromEls([validSpan]);

    assert.strictEqual(withThin.d, withoutThin.d);
});

test('blob geometry: a multi-band path is a single continuous contour, not stacked sub-paths', () => {
    const env = loadSnippets();

    const result = env.window._blobPath(
        [{ x1: 0, y1: 0, x2: 100, y2: 30 }, { x1: 0, y1: 34, x2: 80, y2: 64 }], 10);

    const tokens = result.split(' ');
    assert.strictEqual(tokens.filter((token) => token === 'M').length, 1);
    assert.strictEqual(tokens.filter((token) => token === 'Z').length, 1);
});

test('blob geometry: no bands yields an empty path instead of throwing', () => {
    const env = loadSnippets();

    assert.strictEqual(env.window._blobPath([], 10), '');
});

test('root: paginated mode resolves the pager as the single root', () => {
    const env = loadSnippets();
    const pager = env.document.createElement('div');
    pager.id = '_pager';
    env.document.body.appendChild(pager);

    const roots = env.window._snippetRoots();

    assert.strictEqual(roots.length, 1);
    assert.strictEqual(roots[0].root, pager);
    assert.strictEqual(roots[0].chapterHRef, null);
});

test('root: scroll mode resolves one root per chapter with its own href', () => {
    const env = loadSnippets();
    env.window.setMode('scroll');
    const first = env.document.createElement('div');
    first.className = 'chapter-content';
    first.dataset.chapterHref = 'ch1.xhtml';
    const second = env.document.createElement('div');
    second.className = 'chapter-content';
    second.dataset.chapterHref = 'ch2.xhtml';
    env.document.body.appendChild(first);
    env.document.body.appendChild(second);

    const roots = env.window._snippetRoots();

    assert.deepStrictEqual(
        Array.from(roots, (item) => item.chapterHRef), ['ch1.xhtml', 'ch2.xhtml']);
    assert.strictEqual(roots[0].root, first);
    assert.strictEqual(roots[1].root, second);
});

// snippets.js reads `_translatableCandidates` (translation.js) and `_currentMode` (bridge.js), so
// the visual/interaction layer needs all three files sharing one context, exactly as index.html
// loads them in the WebView.
function loadFull(options) {
    const env = createEnv(options);
    env.document.documentElement.dataset.idiom = 'desktop';
    env.load('bridge.js');
    env.load('translation.js');
    env.load('snippets.js');
    return env;
}

function mountWithParagraphs(env, texts) {
    const pager = env.document.createElement('div');
    pager.id = '_pager';
    env.document.body.appendChild(pager);
    const paragraphs = texts.map((text) => {
        const paragraph = env.document.createElement('p');
        paragraph.textContent = text;
        pager.appendChild(paragraph);
        return paragraph;
    });
    env.window.mountSnippetLayer();
    return paragraphs;
}

// The harness has no generic event dispatcher, so tests reach into the element's/document's own
// `listeners` map (populated by addEventListener) and invoke the registered handler directly with
// a hand-built event object — the same technique translation.test.js and scroll.test.js use for
// window/document listeners, extended here to per-element ones.
function fire(target, type, event) {
    for (const handler of target.listeners.get(type) ?? []) {
        handler(event ?? {});
    }
}

function tap(env, span) {
    fire(span, 'pointerdown', { target: span });
    fire(env.document, 'pointerup');
}

test('mount: wrapping the same paragraphs twice does not double-wrap them', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);

    env.window.mountSnippetLayer();

    assert.strictEqual(paragraphs[0].querySelectorAll('[data-si]').length, 2);
});

test('unmount: restores the original text and is safe to call twice', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);

    env.window.unmountSnippetLayer();
    env.window.unmountSnippetLayer();

    assert.strictEqual(paragraphs[0].textContent, 'Ela chegou. Ele saiu.');
    assert.strictEqual(paragraphs[0].dataset.pi, undefined);
});

test('mount: a paragraph with element children becomes a single period preserving its markup', () => {
    const env = loadFull();
    const pager = env.document.createElement('div');
    pager.id = '_pager';
    env.document.body.appendChild(pager);
    const paragraph = env.document.createElement('p');
    paragraph.innerHTML = 'Hello <em>world</em>!';
    pager.appendChild(paragraph);
    const em = paragraph.querySelectorAll('em')[0];

    env.window.mountSnippetLayer();

    const spans = paragraph.querySelectorAll('[data-si]');
    assert.strictEqual(spans.length, 1);
    assert.strictEqual(spans[0].dataset.si, '0');
    assert.ok(
        Array.from(spans[0].childNodes).includes(em),
        'the original <em> node should be moved in, not recreated');
});

test('tap: tapping a period shows the selection blob and pill', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    const spans = paragraphs[0].querySelectorAll('[data-si]');

    tap(env, spans[0]);

    assert.strictEqual(env.document.querySelectorAll('.tr-pill').length, 1);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob').length, 1);
    assert.strictEqual(spans[0].className, 'tr-sent tr-on');
});

test('z-order: the blob mask and svg become the first children of the paragraph, before any sentence span', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);

    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);

    const children = paragraphs[0].childNodes;
    const maskIndex = children.findIndex((node) => node.className === 'tr-blob');
    // The blob outline is a real SVG element: its className is not a plain string (see
    // FakeSvgElement in harness.js), so this reads the reflected class attribute instead — the
    // same string-safe pattern production code uses via _hasClass (B-2).
    const svgIndex = children.findIndex((node) => node.getAttribute && node.getAttribute('class') === 'tr-blob-svg');
    const firstSentIndex = children.findIndex((node) => node.dataset && node.dataset.si !== undefined);

    assert.notStrictEqual(maskIndex, -1);
    assert.notStrictEqual(svgIndex, -1);
    assert.notStrictEqual(firstSentIndex, -1);
    assert.ok(maskIndex < firstSentIndex, 'the blob mask must paint under the text, not over it');
    assert.ok(svgIndex < firstSentIndex, 'the blob outline must paint under the text, not over it');
});

test('unmount: a stray glass blob is skipped without throwing even though its outline is an SVG element (B-2 belt and suspenders)', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob-svg').length, 1);

    // Calls the paragraph-restore step directly, with its blob mask+svg still attached, instead of
    // going through unmountSnippetLayer (which now removes blobs first): this is what proves the
    // class check itself is string-safe, independent of the call-order fix around it.
    assert.doesNotThrow(() => env.window._unwrapParagraph(paragraphs[0]));

    assert.strictEqual(paragraphs[0].textContent, 'Ela chegou. Ele saiu.');
    assert.strictEqual(env.document.querySelectorAll('.tr-blob').length, 0);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob-svg').length, 0);
    assert.strictEqual(paragraphs[0].dataset.pi, undefined);
});

test('sweep: clearing the selection removes its blob from the registry and the DOM', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    assert.strictEqual(env.window._blobs.size, 1);

    env.window.clearSnippetSelection();

    assert.strictEqual(env.window._blobs.size, 0);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob').length, 0);
});

test('selection: a non-contiguous selection gets one blob per contiguous run, never spanning the gap', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Um. Dois. Tres.']);
    const spans = paragraphs[0].querySelectorAll('[data-si]');

    tap(env, spans[0]);
    tap(env, spans[2]);

    assert.strictEqual(env.window._blobs.size, 2);
    assert.ok(env.window._blobs.has('sel:0:0'));
    assert.ok(env.window._blobs.has('sel:0:2'));
});

test('tap: tapping a selected period again clears the selection', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Um periodo so.']);
    const spans = paragraphs[0].querySelectorAll('[data-si]');

    tap(env, spans[0]);
    tap(env, spans[0]);

    assert.strictEqual(env.document.querySelectorAll('.tr-pill').length, 0);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob').length, 0);
});

test('tap: tapping a period in another paragraph restarts the selection there', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Primeiro periodo.', 'Segundo periodo.']);
    const firstSpan = paragraphs[0].querySelectorAll('[data-si]')[0];
    const secondSpan = paragraphs[1].querySelectorAll('[data-si]')[0];

    tap(env, firstSpan);
    tap(env, secondSpan);

    assert.strictEqual(firstSpan.className, 'tr-sent');
    assert.strictEqual(secondSpan.className, 'tr-sent tr-on');
});

test('drag: dragging from one period to another selects the contiguous range', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Um. Dois. Tres.']);
    const spans = paragraphs[0].querySelectorAll('[data-si]');
    spans[0].rect = { top: 0, left: 0, right: 20, bottom: 20, width: 20, height: 20 };
    spans[1].rect = { top: 0, left: 20, right: 40, bottom: 20, width: 20, height: 20 };
    spans[2].rect = { top: 0, left: 40, right: 60, bottom: 20, width: 20, height: 20 };

    fire(spans[0], 'pointerdown', { target: spans[0] });
    fire(env.document, 'pointermove', { clientX: 50, clientY: 10 });
    fire(env.document, 'pointerup');

    assert.strictEqual(env.document.querySelectorAll('.tr-pill').length, 1);
    assert.strictEqual(env.document.querySelectorAll('.tr-on').length, 3);
});

test('extend and shrink buttons grow and shrink the contiguous selection', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Um. Dois. Tres.']);
    const spans = paragraphs[0].querySelectorAll('[data-si]');
    tap(env, spans[0]);

    fire(env.document.querySelectorAll('.ph-plus')[0], 'click');

    assert.strictEqual(env.document.querySelectorAll('.tr-on').length, 2);

    fire(env.document.querySelectorAll('.ph-minus')[0], 'click');

    assert.strictEqual(env.document.querySelectorAll('.tr-on').length, 1);
});

test('Escape clears the selection on desktop', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);

    fire(env.document, 'keydown', { key: 'Escape' });

    assert.strictEqual(env.document.querySelectorAll('.tr-pill').length, 0);
});

test('Escape does nothing on phone', () => {
    const env = loadFull();
    env.document.documentElement.dataset.idiom = 'phone';
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);

    fire(env.document, 'keydown', { key: 'Escape' });

    assert.strictEqual(env.document.querySelectorAll('.tr-pill').length, 1);
});

test('click outside the paragraph text clears the selection', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    const outside = env.appendToBody('div');

    fire(env.document, 'click', { target: outside });

    assert.strictEqual(env.document.querySelectorAll('.tr-pill').length, 0);
});

test('css: the pill tip, onlySentence note, primary button and hint never wrap onto a new line', () => {
    const env = loadSnippets();
    const css = env.window._SNIPPET_CSS;
    const ruleFor = (selector) => css.split('\n').find((line) => line.startsWith(selector + ' '));

    for (const selector of ['.tr-pill-tip', '.tr-pill-only', '.tr-pill-primary', '.tr-hint']) {
        const rule = ruleFor(selector);
        assert.ok(rule, selector + ' rule not found in _SNIPPET_CSS');
        assert.ok(rule.includes('white-space: nowrap'), selector + ' must never wrap internally');
    }
});

test('css: the pill, hint and chip font stack no longer falls back to the undefined --font-body variable', () => {
    const env = loadSnippets();

    assert.ok(!env.window._SNIPPET_CSS.includes('var(--font-body)'));
    assert.ok(env.window._SNIPPET_CSS.includes("'Inter', sans-serif"));
});

test('pillBottom: paginated desktop sits 24px above the footer', () => {
    const env = loadFull();

    assert.strictEqual(env.window._pillBottom(), 24);
});

test('pillBottom: paginated phone sits 10px above the footer', () => {
    const env = loadFull();
    env.document.documentElement.dataset.idiom = 'phone';

    assert.strictEqual(env.window._pillBottom(), 10);
});

test('pillBottom: scroll desktop sits 32px above the WebView bottom edge', () => {
    const env = loadFull();
    env.window.setMode('scroll');

    assert.strictEqual(env.window._pillBottom(), 32);
});

test('pillBottom: scroll phone also sits 32px above the WebView bottom edge', () => {
    const env = loadFull();
    env.document.documentElement.dataset.idiom = 'phone';
    env.window.setMode('scroll');

    assert.strictEqual(env.window._pillBottom(), 32);
});

test('hint: shows before the first selection and never returns after it', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);

    assert.strictEqual(env.document.querySelectorAll('.tr-hint').length, 1);

    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);

    assert.strictEqual(env.document.querySelectorAll('.tr-hint').length, 0);

    env.window.clearSnippetSelection();

    assert.strictEqual(env.document.querySelectorAll('.tr-hint').length, 0);
});

const LABELS = {
    selectHint: '', extendTip: '', sentenceOne: '', sentenceMany: '', translateSnip: '',
    extendSel: '', shrinkSel: '', onlySentence: '', toggleSnip: '', removeSnip: '',
    langMap: { English: 'EN', 'Brazilian Portuguese (PT-BR)': 'PT-BR' },
    theme: { bg: '#1A1A2E', accent: '#60A5FA' },
    sourceLanguage: 'English', targetLanguage: 'Brazilian Portuguese (PT-BR)',
};

function loadWithLabels() {
    const env = loadFull();
    env.window.setSnippetLabels(LABELS);
    return env;
}

test('restore: a snippet whose hash matches renders the translated text', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);

    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);

    const snip = env.document.querySelectorAll('[data-snip]');
    assert.strictEqual(snip.length, 1);
    assert.strictEqual(snip[0].childNodes[0].textContent, 'She said yes.');
});

test('restore: a restored snippet keeps a permanent glass blob around it, not just while selected', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);
    const snip = env.document.querySelectorAll('[data-snip]')[0];
    snip.rect = { top: 0, left: 0, right: 80, bottom: 20, width: 80, height: 20 };
    env.window._renderAllBlobs();

    const blob = env.window._blobs.get('snip:' + snip.dataset.snip);
    assert.ok(blob, 'a finished snip must register a blob keyed by its own snip key');
    assert.notStrictEqual(blob.mask.style.clipPath, "path('')");
});

test('restore: a snippet whose hash diverges is dropped and the paragraph is untouched', () => {
    const env = loadWithLabels();
    const paragraphs = mountWithParagraphs(env, ['Ela disse que sim.']);

    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: 'deadbeef', translatedText: 'She said yes.', showingOriginal: false,
    }]);

    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 0);
    assert.strictEqual(paragraphs[0].querySelectorAll('[data-si]').length, 1);
    assert.strictEqual(paragraphs[0].textContent, 'Ela disse que sim.');
});

// Iter 6 (D-A): a row poisoned before the length guard existed (or by a stale, pre-hardening cache
// entry) must be purged the first time the book reopens, not rendered as a "duplicated" paragraph.
test('restore: a translation implausibly longer than the original is dropped and its dead row is purged via snip-remove', () => {
    const env = loadWithLabels();
    const paragraphs = mountWithParagraphs(env, ['Ela disse que sim.']);
    const sent = [];
    env.window.sendRawMessage = (message) => { sent.push(message); return true; };
    const poisoned = 'Ela disse que sim. ' + 'x'.repeat(200);

    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: poisoned, showingOriginal: false,
    }]);

    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 0);
    assert.strictEqual(paragraphs[0].querySelectorAll('[data-si]').length, 1);
    assert.strictEqual(sent.length, 1);
    assert.ok(sent[0].startsWith('snip-remove|'));
    assert.deepStrictEqual(JSON.parse(sent[0].slice('snip-remove|'.length)), {
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
    });
});

test('restore: a plausible translation is applied and never triggers a purge', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    const sent = [];
    env.window.sendRawMessage = (message) => { sent.push(message); return true; };

    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);

    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 1);
    assert.strictEqual(sent.length, 0);
});

test('restore: a snippet whose hash diverges is discarded silently, without purging anything', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    const sent = [];
    env.window.sendRawMessage = (message) => { sent.push(message); return true; };

    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: 'deadbeef', translatedText: 'She said yes.', showingOriginal: false,
    }]);

    assert.strictEqual(sent.length, 0);
});

test('restore: a snippet saved showing the original comes back showing the original', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);

    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: true,
    }]);

    const snip = env.document.querySelectorAll('[data-snip]')[0];
    assert.strictEqual(snip.childNodes[0].textContent, 'Ela disse que sim.');
});

test('toggle: switching a snippet swaps the text and flips the chip label', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);
    const snip = env.document.querySelectorAll('[data-snip]')[0];
    const chipLabel = snip.querySelector('.tr-snip-chip').querySelectorAll('span')[0];
    assert.strictEqual(chipLabel.textContent, 'PT-BR');

    fire(snip, 'click', { target: snip });

    assert.strictEqual(snip.childNodes[0].textContent, 'Ela disse que sim.');
    assert.strictEqual(
        snip.querySelector('.tr-snip-chip').querySelectorAll('span')[0].textContent, 'EN');
});

test('toggle: switching a snippet re-measures its blob when the geometry changed', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);
    const snip = env.document.querySelectorAll('[data-snip]')[0];
    snip.rect = { top: 0, left: 0, right: 60, bottom: 20, width: 60, height: 20 };
    env.window._renderAllBlobs();
    const before = env.window._blobs.get('snip:' + snip.dataset.snip).mask.style.clipPath;

    snip.rect = { top: 0, left: 0, right: 140, bottom: 20, width: 140, height: 20 };
    fire(snip, 'click', { target: snip });

    const after = env.window._blobs.get('snip:' + snip.dataset.snip).mask.style.clipPath;
    assert.notStrictEqual(after, before);
});

test('the remove icon on the chip restores the periods and does not toggle the snip', () => {
    const env = loadWithLabels();
    const paragraphs = mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);
    const closeIcon = env.document.querySelectorAll('[data-snip]')[0]
        .querySelector('.tr-snip-chip').querySelectorAll('.ph-x')[0];

    fire(closeIcon, 'click', { target: closeIcon });

    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 0);
    assert.strictEqual(paragraphs[0].querySelectorAll('[data-si]').length, 1);
    assert.strictEqual(paragraphs[0].textContent, 'Ela disse que sim.');
});

test('sweep: removing a snip via the chip clears its blob from the registry and the DOM', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);
    assert.strictEqual(env.window._blobs.size, 1);
    const closeIcon = env.document.querySelectorAll('[data-snip]')[0]
        .querySelector('.tr-snip-chip').querySelectorAll('.ph-x')[0];

    fire(closeIcon, 'click', { target: closeIcon });

    assert.strictEqual(env.window._blobs.size, 0);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob').length, 0);
});

test('unmount: completes without throwing and remains re-mountable with a snip blob and an active selection present (B-2 regression)', () => {
    const env = loadWithLabels();
    const paragraphs = mountWithParagraphs(env, ['Ela disse que sim. Ele saiu.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    assert.strictEqual(env.window._blobs.size, 2);

    assert.doesNotThrow(() => env.window.unmountSnippetLayer());

    assert.strictEqual(env.document.querySelectorAll('[data-pi]').length, 0);
    assert.strictEqual(env.document.querySelectorAll('[data-si]').length, 0);
    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 0);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob').length, 0);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob-svg').length, 0);
    assert.strictEqual(env.window._blobs.size, 0);
    assert.strictEqual(paragraphs[0].textContent, 'Ela disse que sim. Ele saiu.');

    env.window.mountSnippetLayer();

    assert.strictEqual(paragraphs[0].querySelectorAll('[data-si]').length, 2);
});

test('applySnippetTranslation replaces a loading placeholder with the finished snip', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.setSnippetLoading(['ch1.xhtml:0:0:0']);
    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 1);

    env.window.applySnippetTranslation([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        translatedText: 'She said yes.', showingOriginal: false,
    }]);

    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 0);
    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 1);
});

test('applySnippetTranslation gives the finished snip a permanent glass blob, replacing the loading one', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.setSnippetLoading(['ch1.xhtml:0:0:0']);
    assert.strictEqual(env.window._blobs.has('load:ch1.xhtml:0:0:0'), true);

    env.window.applySnippetTranslation([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        translatedText: 'She said yes.', showingOriginal: false,
    }]);

    assert.strictEqual(env.window._blobs.has('load:ch1.xhtml:0:0:0'), false);
    const snip = env.document.querySelectorAll('[data-snip]')[0];
    assert.ok(env.window._blobs.has('snip:' + snip.dataset.snip));
});

test('loading: the placeholder blob pulses while its translation is in flight', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Um. Dois. Tres.']);

    env.window.setSnippetLoading(['ch1.xhtml:0:0:1']);

    const blob = env.window._blobs.get('load:ch1.xhtml:0:0:1');
    assert.ok(blob, 'a loading placeholder must register a blob keyed by load:<snipKey>');
    assert.ok(blob.mask.className.includes('tr-blob-pulse'));
});

test('clearSnippetLoading: restores a pulsing placeholder back to individual periods', () => {
    const env = loadWithLabels();
    const paragraphs = mountWithParagraphs(env, ['Um. Dois. Tres.']);
    env.window.setSnippetLoading(['ch1.xhtml:0:0:1']);
    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 1);

    env.window.clearSnippetLoading(['ch1.xhtml:0:0:1']);

    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 0);
    assert.strictEqual(paragraphs[0].querySelectorAll('[data-si]').length, 3);
    assert.strictEqual(paragraphs[0].textContent, 'Um. Dois. Tres.');
});

test('clearSnippetLoading: removes the pulsing blob from the registry and the DOM', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Um. Dois. Tres.']);
    env.window.setSnippetLoading(['ch1.xhtml:0:0:1']);
    assert.strictEqual(env.window._blobs.size, 1);

    env.window.clearSnippetLoading(['ch1.xhtml:0:0:1']);

    assert.strictEqual(env.window._blobs.size, 0);
    assert.strictEqual(env.document.querySelectorAll('.tr-blob').length, 0);
});

test('resize: re-measures blobs even when there is no active selection', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Um. Dois. Tres.']);
    env.window.setSnippetLoading(['ch1.xhtml:0:0:1']);
    const loadingSpan = env.document.querySelectorAll('.tr-loading')[0];
    loadingSpan.rect = { top: 0, left: 0, right: 60, bottom: 20, width: 60, height: 20 };
    env.window._renderAllBlobs();
    const before = env.window._blobs.get('load:ch1.xhtml:0:0:1').mask.style.clipPath;

    loadingSpan.rect = { top: 0, left: 0, right: 140, bottom: 20, width: 140, height: 20 };
    env.fireWindow('resize');

    const after = env.window._blobs.get('load:ch1.xhtml:0:0:1').mask.style.clipPath;
    assert.notStrictEqual(after, before);
});

test('clearSnippetLoading: never touches a snip whose translation already arrived', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: SNIP_HASH_GOLDEN, translatedText: 'She said yes.', showingOriginal: false,
    }]);

    env.window.clearSnippetLoading(['ch1.xhtml:0:0:0']);

    const snip = env.document.querySelectorAll('[data-snip]');
    assert.strictEqual(snip.length, 1);
    assert.strictEqual(snip[0].childNodes[0].textContent, 'She said yes.');
});

test('clearSnippetLoading: does nothing when the paragraph no longer exists', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);

    assert.doesNotThrow(() => env.window.clearSnippetLoading(['ch1.xhtml:9:0:0']));
});

test('applySnippetTranslation destructively replaces an overlapping existing snip', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Um. Dois. Tres.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 1,
        originalHash: env.window._snipHash('Um. Dois.'), translatedText: 'One. Two.',
        showingOriginal: false,
    }]);
    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 1);

    env.window.applySnippetTranslation([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 1, sentenceEnd: 2,
        translatedText: 'Two. Three.', showingOriginal: false,
    }]);

    const snips = env.document.querySelectorAll('[data-snip]');
    assert.strictEqual(snips.length, 1);
    assert.strictEqual(snips[0].dataset.snip, 'ch1.xhtml:0:1:2');
});

test('translate: clicking the primary button sends a snip| message with the selected run', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    const sent = [];
    env.window.sendRawMessage = (message) => { sent.push(message); return true; };
    tap(env, env.document.querySelectorAll('[data-si]')[0]);

    fire(env.document.querySelectorAll('.tr-pill-primary')[0], 'click');

    assert.strictEqual(sent.length, 1);
    assert.ok(sent[0].startsWith('snip|'));
    const runs = JSON.parse(sent[0].slice('snip|'.length));
    assert.deepStrictEqual(runs, [{
        chapterHRef: null, paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        text: 'Ela chegou.', paragraph: 'Ela chegou. Ele saiu.',
    }]);
    assert.strictEqual(env.document.querySelectorAll('.tr-pill').length, 0);
});

// Iteration 5 parte 2: a paragraph that already has translated snips leaked their chip labels and
// currently-shown text into the "paragraph" context field, which made a small model translate the
// whole paragraph (labels and all) instead of just the newly selected excerpt.

test('_originalParagraphText: reconstructs the untouched original text through a snip showing its translation, a snip showing its original, and a plain period', () => {
    const env = loadWithLabels();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu. Maria ficou.']);
    env.window.restoreSnippets([
        {
            chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
            originalHash: env.window._snipHash('Ela chegou.'), translatedText: 'She arrived.',
            showingOriginal: false,
        },
        {
            chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 1, sentenceEnd: 1,
            originalHash: env.window._snipHash('Ele saiu.'), translatedText: 'He left.',
            showingOriginal: true,
        },
    ]);

    const text = env.window._originalParagraphText(paragraphs[0]);

    assert.strictEqual(text, 'Ela chegou. Ele saiu. Maria ficou.');
    assert.ok(!text.includes('She arrived'));
    assert.ok(!text.includes('He left'));
    assert.ok(!text.includes('PT-BR'));
    assert.ok(!text.includes('EN'));
});

test('translate: the snip| payload carries the clean original paragraph even with a translated snip already showing (context pollution regression)', () => {
    const env = loadWithLabels();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu. Maria ficou.']);
    env.window.restoreSnippets([{
        chapterHRef: 'ch1.xhtml', paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: env.window._snipHash('Ela chegou.'), translatedText: 'She arrived.',
        showingOriginal: false,
    }]);
    const sent = [];
    env.window.sendRawMessage = (message) => { sent.push(message); return true; };
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);

    fire(env.document.querySelectorAll('.tr-pill-primary')[0], 'click');

    const runs = JSON.parse(sent[0].slice('snip|'.length));
    assert.strictEqual(runs[0].text, 'Ele saiu.');
    assert.strictEqual(runs[0].paragraph, 'Ela chegou. Ele saiu. Maria ficou.');
    assert.ok(!runs[0].paragraph.includes('She arrived'));
    assert.ok(!runs[0].paragraph.includes('PT-BR'));
});

// Iteration 5 fix: a real app window is not the mockup's 1280px capture frame — `data-idiom` names
// a device class, not a window width, so a resized Windows desktop can be narrower than the pill's
// full desktop content. These pin the measure-then-degrade behavior _fitPill/_renderHint add.

test('pill: nothing is removed when the built pill already fits the viewport', () => {
    const env = loadSnippets();
    env.document.documentElement.clientWidth = 800;
    const pill = makeFakePill(env);
    pill.scrollWidth = 300;

    env.window._fitPill(pill);

    assert.strictEqual(pill.querySelectorAll('.tr-pill-tip').length, 1);
    assert.strictEqual(pill.querySelector('.tr-pill-primary').querySelectorAll('span').length, 1);
});

test('pill: dropping the tip is enough to fit, so the button keeps its label', () => {
    const env = loadSnippets();
    env.document.documentElement.clientWidth = 320;
    const pill = makeFakePill(env);
    Object.defineProperty(pill, 'scrollWidth', {
        get: () => (pill.querySelector('.tr-pill-tip') ? 400 : 200),
    });

    env.window._fitPill(pill);

    assert.strictEqual(pill.querySelectorAll('.tr-pill-tip').length, 0);
    assert.strictEqual(pill.querySelector('.tr-pill-primary').querySelectorAll('span').length, 1);
});

test('pill: still overflowing once the tip is gone also drops the button label, keeping it reachable via aria-label', () => {
    const env = loadSnippets();
    env.window.setSnippetLabels(Object.assign({}, LABELS, { translateSnip: 'Traduzir trecho' }));
    env.document.documentElement.clientWidth = 260;
    const pill = makeFakePill(env);
    const primary = pill.querySelector('.tr-pill-primary');
    Object.defineProperty(pill, 'scrollWidth', {
        get: () => {
            if (pill.querySelector('.tr-pill-tip')) return 500;
            if (primary.querySelector('span')) return 400;
            return 200;
        },
    });

    env.window._fitPill(pill);

    assert.strictEqual(pill.querySelectorAll('.tr-pill-tip').length, 0);
    assert.strictEqual(primary.querySelectorAll('span').length, 0);
    assert.strictEqual(primary.getAttribute('aria-label'), 'Traduzir trecho');
    assert.strictEqual(primary.getAttribute('title'), 'Traduzir trecho');
});

test('hint: never renders when it cannot fit the viewport, since it is disposable', () => {
    const env = loadSnippets();
    env.document.documentElement.clientWidth = 0;

    env.window._renderHint();

    assert.strictEqual(env.document.querySelectorAll('.tr-hint').length, 0);
});

test('resize: rebuilds the pill when a selection is active, so it can re-fit a new viewport width', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    const before = env.document.querySelectorAll('.tr-pill')[0];

    env.fireWindow('resize');

    const after = env.document.querySelectorAll('.tr-pill');
    assert.strictEqual(after.length, 1);
    assert.notStrictEqual(after[0], before, 'resize must rebuild the pill element, not reuse the old one');
});
