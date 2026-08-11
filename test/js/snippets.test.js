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

// Blob geometry (iter 7) is relative to the snippet ROOT, not the paragraph, so every paragraph a
// test builds needs a root ancestor for _rootFor to resolve - the root's own rect defaults to zero
// (harness default), same origin every one of these tests already assumed of the paragraph before.
function ensureRoot(env) {
    let root = env.document.getElementById('_pager');
    if (!root) {
        root = env.document.createElement('div');
        root.id = '_pager';
        env.document.body.appendChild(root);
    }
    return root;
}

function makeParagraph(env, rect) {
    const paragraph = env.document.createElement('p');
    paragraph.dataset.pi = '0';
    paragraph.rect = rect;
    ensureRoot(env).appendChild(paragraph);
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

// Iter 6 (D-B): CSS multi-column pagination can fragment a paragraph across two columns/pages. The
// tail of one column and the head of the next sit at similar heights but are far apart horizontally
// — before this fix the height-only line grouping merged them into one band spanning the gap.
test('blob geometry: a paragraph fragmented across two columns traces one contour per column, never bridging the gap', () => {
    const env = loadSnippets();
    const paragraph = makeParagraph(env, { top: 0, left: 0, right: 600, bottom: 600, width: 600, height: 600 });
    const tail = makeSpan(env, paragraph, { top: 560, left: 8, right: 108, bottom: 590, width: 100, height: 30 });
    const head = makeSpan(env, paragraph, { top: 16, left: 408, right: 488, bottom: 46, width: 80, height: 30 });

    const result = env.window._blobFromEls([tail, head]);

    const tokens = result.d.split(' ');
    assert.strictEqual(tokens.filter((token) => token === 'M').length, 2);
    assert.strictEqual(tokens.filter((token) => token === 'Z').length, 2);
});

test('blob geometry: two lines in the same column still trace a single contour', () => {
    const env = loadSnippets();
    const paragraph = makeParagraph(env, { top: 0, left: 0, right: 200, bottom: 100, width: 200, height: 100 });
    const first = makeSpan(env, paragraph, { top: 8, left: 8, right: 108, bottom: 38, width: 100, height: 30 });
    const second = makeSpan(env, paragraph, { top: 42, left: 8, right: 88, bottom: 72, width: 80, height: 30 });

    const result = env.window._blobFromEls([first, second]);

    const tokens = result.d.split(' ');
    assert.strictEqual(tokens.filter((token) => token === 'M').length, 1);
    assert.strictEqual(tokens.filter((token) => token === 'Z').length, 1);
});

// Iter 7 (D-B round 2): a period fragmented across two pager columns used to be measured relative to
// the PARAGRAPH's own (fragmented) box, whose reported rect can disagree with where an absolutely
// positioned descendant actually anchors — the exact anchor/origin mismatch that clipped the glass
// (negative-looking geometry) or floated it as a phantom bubble. This root has its own non-zero rect
// and the paragraph deliberately has NONE (default zero), so a wrong left/top/w/h below would prove
// the paragraph is still being read somewhere.
test('blob geometry: a paragraph fragmented across two columns is measured relative to the ROOT, never the paragraph', () => {
    const env = loadSnippets();
    const pager = env.document.createElement('div');
    pager.id = '_pager';
    pager.rect = { top: 10, left: 5, right: 1205, bottom: 610, width: 1200, height: 600 };
    env.document.body.appendChild(pager);
    const paragraph = env.document.createElement('p');
    pager.appendChild(paragraph);
    // Column 1's tail sits near the root's bottom; column 2's head is further right AND higher up
    // (a smaller y1 than the tail it continues from) — the exact shape a pager's CSS columns
    // produce, and the one _columnGroupsOf relies on to tell the two columns apart.
    const tail = makeSpan(env, paragraph, { top: 560, left: 13, right: 113, bottom: 590, width: 100, height: 30 });
    const head = makeSpan(env, paragraph, { top: 26, left: 413, right: 493, bottom: 56, width: 80, height: 30 });

    const result = env.window._blobFromEls([tail, head]);

    // Hand-derived from the root rect above (OFF=8, padX=5, padY=1.5).
    assert.strictEqual(result.left, -5);
    assert.strictEqual(result.top, 6.5);
    assert.strictEqual(result.w, 506);
    assert.strictEqual(result.h, 583);
    const tailBandLocal = { x1: 8, y1: 542, x2: 118, y2: 575 };
    const headBandLocal = { x1: 408, y1: 8, x2: 498, y2: 41 };
    assert.ok(
        [tailBandLocal, headBandLocal].every((band) => band.y1 >= 0 && band.y2 >= 0),
        'a band drawn above the mask box would be clipped/invisible — the exact original defect');
    assert.strictEqual(
        result.d,
        env.window._blobPath([tailBandLocal], 10) + ' ' + env.window._blobPath([headBandLocal], 10));
    const tokens = result.d.split(' ');
    assert.strictEqual(tokens.filter((token) => token === 'M').length, 2);
    assert.strictEqual(tokens.filter((token) => token === 'Z').length, 2);
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

test('root: an element outside every snippet root resolves to no root', () => {
    const env = loadSnippets();
    const outside = env.document.createElement('span');
    env.document.body.appendChild(outside);

    assert.strictEqual(env.window._rootFor(outside), null);
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

// Iter 7 (D-B round 2): the glass blob is anchored to a layer owned by the snippet ROOT, not the
// paragraph — this is what makes a period fragmented across pager columns keep its glass visible on
// every page it spans, instead of a mismatched anchor clipping it or floating a phantom bubble.

test('layer: mounting creates a blob layer as the first child of the root, ignoring pointer events', () => {
    const env = loadFull();
    mountWithParagraphs(env, ['Ela chegou.']);
    const pager = env.document.getElementById('_pager');

    assert.strictEqual(pager.childNodes[0].className, 'tr-blob-layer');
});

test('css: the blob layer never intercepts pointer events', () => {
    const env = loadSnippets();
    const css = env.window._SNIPPET_CSS;
    const rule = css.split('\n').find((line) => line.startsWith('.tr-blob-layer '));

    assert.ok(rule, '.tr-blob-layer rule not found in _SNIPPET_CSS');
    assert.ok(rule.includes('pointer-events: none'));
});

test('layer: claims position:relative on a static root and restores it on unmount', () => {
    const env = loadFull();
    mountWithParagraphs(env, ['Ela chegou.']);
    const pager = env.document.getElementById('_pager');
    assert.strictEqual(pager.style.position, 'relative');

    env.window.unmountSnippetLayer();

    assert.strictEqual(pager.style.position, '');
});

test('layer: never touches a root that already had its own position', () => {
    const env = loadFull();
    const pager = env.document.createElement('div');
    pager.id = '_pager';
    pager.style.position = 'absolute';
    env.document.body.appendChild(pager);
    const paragraph = env.document.createElement('p');
    paragraph.textContent = 'Ela chegou.';
    pager.appendChild(paragraph);

    env.window.mountSnippetLayer();
    env.window.unmountSnippetLayer();

    assert.strictEqual(pager.style.position, 'absolute');
});

test('layer: scroll mode gives each chapter root its own blob layer', () => {
    const env = loadFull();
    env.window.setMode('scroll');
    const first = env.document.createElement('div');
    first.className = 'chapter-content';
    first.dataset.chapterHref = 'ch1.xhtml';
    const second = env.document.createElement('div');
    second.className = 'chapter-content';
    second.dataset.chapterHref = 'ch2.xhtml';
    env.document.body.appendChild(first);
    env.document.body.appendChild(second);
    const firstParagraph = env.document.createElement('p');
    firstParagraph.textContent = 'Um.';
    first.appendChild(firstParagraph);
    const secondParagraph = env.document.createElement('p');
    secondParagraph.textContent = 'Dois.';
    second.appendChild(secondParagraph);

    env.window.mountSnippetLayer();

    assert.strictEqual(first.childNodes[0].className, 'tr-blob-layer');
    assert.strictEqual(second.childNodes[0].className, 'tr-blob-layer');
    assert.notStrictEqual(first.childNodes[0], second.childNodes[0]);
});

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

// Iter 8 (derivation D delivered): a paragraph with inline markup no longer collapses into a single
// period whenever a REAL sentence boundary exists outside the markup — the mockups' `onlySentence`
// state stays reachable (see the test above) only when the paragraph genuinely has one sentence.

function pagerWithMarkup(env, html) {
    const pager = env.document.createElement('div');
    pager.id = '_pager';
    env.document.body.appendChild(pager);
    const paragraph = env.document.createElement('p');
    paragraph.innerHTML = html;
    pager.appendChild(paragraph);
    return paragraph;
}

test('mount: inline markup between two real sentence boundaries stays inside its own period, and the other periods split normally', () => {
    const env = loadFull();
    const paragraph = pagerWithMarkup(env, 'A <em>bold</em> claim here. Second sentence follows. Third one ends.');
    const em = paragraph.querySelectorAll('em')[0];

    env.window.mountSnippetLayer();

    const spans = paragraph.querySelectorAll('[data-si]');
    assert.strictEqual(spans.length, 3);
    assert.strictEqual(spans.map((s) => s.dataset.si).join(','), '0,1,2');
    assert.ok(Array.from(spans[0].childNodes).includes(em), 'the <em> lives inside period 0');
    assert.strictEqual(spans[0].textContent, 'A bold claim here.');
    assert.strictEqual(spans[1].textContent, 'Second sentence follows.');
    assert.strictEqual(spans[2].textContent, 'Third one ends.');
    assert.strictEqual(
        paragraph.textContent, 'A bold claim here. Second sentence follows. Third one ends.');

    tap(env, spans[1]);

    assert.strictEqual(spans[0].className, 'tr-sent');
    assert.strictEqual(spans[1].className, 'tr-sent tr-on');
    assert.strictEqual(spans[2].className, 'tr-sent', 'selecting period 1 must not select period 2');
});

test('mount: a sentence boundary that would fall inside an inline element is deferred to after it, never cutting the element', () => {
    const env = loadFull();
    const paragraph = pagerWithMarkup(
        env, 'Intro text <em>ends here. And continues</em> after. Final sentence.');
    const em = paragraph.querySelectorAll('em')[0];

    env.window.mountSnippetLayer();

    const spans = paragraph.querySelectorAll('[data-si]');
    assert.strictEqual(spans.length, 2);
    assert.ok(
        Array.from(spans[0].childNodes).includes(em),
        'the whole <em> stays inside period 0, including the boundary that would have cut through it');
    assert.strictEqual(spans[0].textContent, 'Intro text ends here. And continues after.');
    assert.strictEqual(spans[1].textContent, 'Final sentence.');
});

// Reviewer BLOCKED B-1: a boundary's whitespace run can START in free text and continue PAST an
// inline element's own opening tag when that element's own content starts with a space (common EPUB
// markup, e.g. `<em> continues</em>`). Filtering only on the boundary's START missed this, so
// _consumeTextNode later called Text.splitText with an offset measured past the SHRUNK node it was
// actually holding — a real WebView (and a spec-faithful harness) throws IndexSizeError there,
// aborting the mount mid-paragraph with whatever was already moved detached from the screen.
test('mount: a sentence boundary whose whitespace starts in free text and continues into a leading space inside the next element never crashes the mount (B-1)', () => {
    const env = loadFull();
    const paragraph = pagerWithMarkup(env, 'One. <em> Two words</em>');
    const em = paragraph.querySelectorAll('em')[0];

    assert.doesNotThrow(() => env.window.mountSnippetLayer());

    const spans = paragraph.querySelectorAll('[data-si]');
    assert.strictEqual(spans.length, 1, 'the only boundary found overlaps the element, so it is deferred entirely — one period');
    assert.ok(
        Array.from(spans[0].childNodes).includes(em),
        'the <em> must still be moved in whole, not lost/detached by an aborted mount');
    assert.strictEqual(paragraph.textContent, 'One.  Two words', 'no character is lost');
});

// Reviewer BLOCKED B-3: the walk treated any non-element child as a genuine, splittable Text node.
// A Comment has its own `.data` (so it looked like text) but contributes NOTHING to el.textContent
// (DOM spec) and has no splitText at all — a book's HTML comments survive into the reader's real DOM
// (ExtractBodyContent/loadChapter never strip them), so this is reachable with real EPUB content.
// Built via direct DOM calls (appendChild/createComment), not innerHTML: the harness's HTML parser
// never claimed to understand comment syntax, so a `<!-- -->` string set through innerHTML would
// just become literal text here, never exercising the code path this fix touches.

test('mount: a comment between two text runs never crashes the mount and does not lose the text before it (B-3)', () => {
    const env = loadFull();
    const pager = ensureRoot(env);
    const paragraph = env.document.createElement('p');
    pager.appendChild(paragraph);
    paragraph.appendChild(env.document.createTextNode('End. '));
    paragraph.appendChild(env.document.createComment(' note '));
    const em = env.document.createElement('em');
    em.appendChild(env.document.createTextNode('y'));
    paragraph.appendChild(env.document.createTextNode(' Next sentence '));
    paragraph.appendChild(em);
    paragraph.appendChild(env.document.createTextNode('.'));

    assert.doesNotThrow(() => env.window.mountSnippetLayer());

    const spans = paragraph.querySelectorAll('[data-si]');
    assert.strictEqual(spans.length, 2);
    assert.strictEqual(spans[0].textContent, 'End.', '"End." must not be lost, unlike the pre-fix abort');
    assert.ok(
        Array.from(spans[1].childNodes).includes(em), 'the <em> after the comment still lands in period 1');
    assert.strictEqual(paragraph.textContent, 'End.  Next sentence y.', 'no character is lost');
});

test('mount: a comment right after an element never shifts a later boundary out of place (B-3)', () => {
    const env = loadFull();
    const pager = ensureRoot(env);
    const paragraph = env.document.createElement('p');
    pager.appendChild(paragraph);
    const em = env.document.createElement('em');
    em.appendChild(env.document.createTextNode('Intro'));
    paragraph.appendChild(em);
    const comment = env.document.createComment('0123456789');
    paragraph.appendChild(comment);
    paragraph.appendChild(env.document.createTextNode(' word one. Second half here.'));

    env.window.mountSnippetLayer();

    const spans = paragraph.querySelectorAll('[data-si]');
    assert.strictEqual(spans.length, 2);
    assert.strictEqual(
        spans[0].textContent, 'Intro word one.',
        'the comment must contribute ZERO to the offset — counting its own data length shifted the boundary 10 chars too early');
    assert.ok(Array.from(spans[0].childNodes).includes(em), 'the <em> stays in period 0');
    assert.ok(
        Array.from(spans[0].childNodes).includes(comment),
        'the comment lives inside whichever period it landed in, moved whole rather than split');
    assert.strictEqual(spans[1].textContent, 'Second half here.');
});

test('unmount: a paragraph with inline markup between real sentence boundaries restores the exact original DOM structure', () => {
    const env = loadFull();
    const paragraph = pagerWithMarkup(
        env, 'A <em>bold</em> claim here. Second sentence follows. Third one ends.');
    const originalHtml = paragraph.innerHTML;

    env.window.mountSnippetLayer();
    env.window.unmountSnippetLayer();

    assert.strictEqual(paragraph.innerHTML, originalHtml);
    assert.strictEqual(paragraph.dataset.pi, undefined);
});

test('_originalParagraphText: a period carrying inline markup contributes its FULL text, not just its first child (W-13)', () => {
    const env = loadWithLabels();
    const paragraph = pagerWithMarkup(
        env, 'A <em>bold</em> claim here. Second sentence follows. Third one ends.');
    env.window.mountSnippetLayer();
    env.window.restoreSnippets([{
        chapterHRef: null, paragraphIndex: 0, sentenceStart: 1, sentenceEnd: 1,
        originalHash: env.window._snipHash('Second sentence follows.'),
        translatedText: 'Segunda frase segue.', showingOriginal: false,
    }]);

    const text = env.window._originalParagraphText(paragraph);

    assert.strictEqual(text, 'A bold claim here. Second sentence follows. Third one ends.');
});

test('snip: translating a period that carries inline markup works normally, and removing it restores the exact original <em> node', () => {
    const env = loadWithLabels();
    const paragraph = pagerWithMarkup(env, 'A <em>bold</em> claim here. Second sentence follows.');
    env.window.mountSnippetLayer();
    const em = paragraph.querySelectorAll('em')[0];

    env.window.setSnippetLoading(['null:0:0:0']);
    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 1);

    env.window.applySnippetTranslation([{
        chapterHRef: null, paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        translatedText: 'A bold claim here, translated.', showingOriginal: false,
    }]);

    const snip = env.document.querySelectorAll('[data-snip]')[0];
    assert.ok(snip, 'the finished snip must exist');
    assert.strictEqual(snip.childNodes[0].textContent, 'A bold claim here, translated.');

    const closeIcon = snip.querySelector('.tr-snip-chip').querySelectorAll('.ph-x')[0];
    fire(closeIcon, 'click', { target: closeIcon });

    const restored = paragraph.querySelectorAll('[data-si]')[0];
    assert.ok(
        Array.from(restored.childNodes).includes(em),
        'removing the snip must bring back the ORIGINAL <em> node, not a re-serialized copy');
    assert.strictEqual(env.window._snipOriginalNodes.size, 0, 'consumed on restore, no leak');
});

// Reviewer BLOCKED B-2: a snip restored straight from a PERSISTED session (restoreSnippets never
// populates _snipOriginalNodes, since setSnippetLoading — the only thing that stashes nodes — was
// never called this session) only ever carries plain text in dataset.orig. Removing it falls back to
// _plainPeriodSpans, which used to re-split that flat text with the raw regex — rediscovering the
// very boundary _wrapMarkupParagraph had deferred past the <em> at wrap time (no element left to
// protect it once flattened) and manufacturing an extra period. That extra period reused whatever
// data-si the FOLLOWING, untouched period already had (0, 1, 1 instead of 0, 1), corrupting every
// range lookup in the paragraph from then on.
test('remove-snip: a snip restored from a persisted session never over-splits into a colliding data-si when its deferred boundary reappears in the flattened text (B-2)', () => {
    const env = loadWithLabels();
    const paragraph = pagerWithMarkup(
        env, 'Intro text <em>ends here. And continues</em> after. Final sentence.');
    env.window.mountSnippetLayer();
    // Wrap-time periods: 0 = "Intro text <em>...</em> after." (the deferred boundary lives here),
    // 1 = "Final sentence.".
    const originalText = env.window._rangeText(paragraph, 0, 0);

    env.window.restoreSnippets([{
        chapterHRef: null, paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        originalHash: env.window._snipHash(originalText), translatedText: 'Traduzido.',
        showingOriginal: false,
    }]);
    const snip = env.document.querySelectorAll('[data-snip]')[0];
    assert.ok(snip, 'the restored snip must exist');

    const closeIcon = snip.querySelector('.tr-snip-chip').querySelectorAll('.ph-x')[0];
    fire(closeIcon, 'click', { target: closeIcon });

    const spans = paragraph.querySelectorAll('[data-si]');
    assert.deepStrictEqual(
        spans.map((s) => s.dataset.si), ['0', '1'],
        'data-si must stay unique and sequential, never 0, 1, 1');
    assert.strictEqual(spans[0].textContent, originalText);
    assert.strictEqual(env.window._rangeText(paragraph, 1, 1), 'Final sentence.');
});

test('clearSnippetLoading: restores the ORIGINAL <em> node (not a re-serialized copy) when a markup period was loading and its translation never arrived', () => {
    const env = loadWithLabels();
    const paragraph = pagerWithMarkup(env, 'A <em>bold</em> claim here.');
    env.window.mountSnippetLayer();
    const em = paragraph.querySelectorAll('em')[0];

    env.window.setSnippetLoading(['null:0:0:0']);
    assert.strictEqual(env.window._snipOriginalNodes.size, 1);

    env.window.clearSnippetLoading(['null:0:0:0']);

    const restored = paragraph.querySelectorAll('[data-si]')[0];
    assert.ok(Array.from(restored.childNodes).includes(em));
    assert.strictEqual(env.window._snipOriginalNodes.size, 0, 'consumed on restore, no leak');
});

test('unmount: clears every stashed original-node entry, never leaking a detached subtree across chapters', () => {
    const env = loadWithLabels();
    pagerWithMarkup(env, 'A <em>bold</em> claim here.');
    env.window.mountSnippetLayer();
    env.window.setSnippetLoading(['null:0:0:0']);
    assert.strictEqual(env.window._snipOriginalNodes.size, 1);

    env.window.unmountSnippetLayer();

    assert.strictEqual(env.window._snipOriginalNodes.size, 0);
});

// Iter 8 (D-B): applySnippetTranslation must never leave a `.tr-loading` placeholder stuck pulsing
// forever for an item whose paragraph/range can no longer be resolved by the time the translation
// comes back (a result that IS present but inapplicable).

test('applySnippetTranslation: an item whose paragraph can no longer be resolved still clears its own loading placeholder instead of leaving it stuck pulsing forever (D-B)', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.setSnippetLoading(['null:0:0:0']);
    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 1);

    // Simulates the exact race reproduced against the real app: by the time the translation comes
    // back, _snippetRoots can no longer resolve the pager that owned the request (torn down by a
    // navigation), so _findParagraph resolves nothing for the key still pulsing in the DOM.
    env.document.getElementById('_pager').id = '';

    env.window.applySnippetTranslation([{
        chapterHRef: null, paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0,
        translatedText: 'She said yes.', showingOriginal: false,
    }]);

    assert.strictEqual(
        env.document.querySelectorAll('.tr-loading').length, 0,
        'an item that could not be applied must not leave its own loading placeholder stuck forever');
    assert.strictEqual(env.document.querySelectorAll('[data-snip]').length, 0);
    assert.strictEqual(env.window._blobs.size, 0);
});

// Iter 8 follow-up: exact-string key matching missed the real-world case where a paginated-mode
// placeholder is keyed with chapterHRef=null (setSnippetLoading always does this) but the item that
// comes back off an in-flight translation carries the concrete chapterHRef of whatever chapter was
// current by the time the result landed — the two strings never matched even though they name the
// SAME request. _clearOrphanedLoading must match by parsed anchor with the same tolerant chapterHRef
// semantics _findParagraph uses (null on either side matches anything), while still requiring
// paragraphIndex AND sentenceStart to agree exactly so it never clears an unrelated placeholder.

test('applySnippetTranslation: an inapplicable item whose paragraphIndex does not match any loading placeholder clears nothing (anchors do not match)', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['Ela disse que sim.']);
    env.window.setSnippetLoading(['null:0:0:0']);
    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 1);

    env.window.applySnippetTranslation([{
        chapterHRef: 'OUTRO-CAPITULO', paragraphIndex: 99, sentenceStart: 0, sentenceEnd: 0,
        translatedText: 'She said yes.', showingOriginal: false,
    }]);

    assert.strictEqual(
        env.document.querySelectorAll('.tr-loading').length, 1,
        'paragraphIndex 99 vs 0 must never match — the unrelated placeholder stays put');
    assert.strictEqual(
        env.document.querySelectorAll('.tr-loading')[0].dataset.loadKey, 'null:0:0:0');
});

test('applySnippetTranslation: an inapplicable item whose chapterHRef differs from the placeholder\'s null still clears it (loose chapterHRef match)', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['P0.', 'P1.', 'P2.', 'P3.', 'P4.', 'Five A. Five B.']);
    env.window.setSnippetLoading(['null:5:0:1']);
    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 1);

    // Same race as the test above, but this time the item comes back with the CONCRETE chapterHRef
    // of the chapter that was current when the result landed, instead of the null the placeholder
    // was keyed with — the exact-string match this fix replaces would have missed this.
    env.document.getElementById('_pager').id = '';

    env.window.applySnippetTranslation([{
        chapterHRef: 'cap3.html', paragraphIndex: 5, sentenceStart: 0, sentenceEnd: 1,
        translatedText: 'Five A. Five B, translated.', showingOriginal: false,
    }]);

    assert.strictEqual(
        env.document.querySelectorAll('.tr-loading').length, 0,
        'null on the placeholder side must match the item\'s concrete chapterHRef');
});

test('applySnippetTranslation: an inapplicable item never clears a DIFFERENT in-flight loading in the same paragraph (sentenceStart disambiguates)', () => {
    const env = loadWithLabels();
    mountWithParagraphs(env, ['P0.', 'P1.', 'Alpha. Beta. Gamma.']);
    env.window.setSnippetLoading(['null:2:0:0', 'null:2:2:2']);
    assert.strictEqual(env.document.querySelectorAll('.tr-loading').length, 2);
    env.document.getElementById('_pager').id = '';

    env.window.applySnippetTranslation([{
        chapterHRef: 'cap.html', paragraphIndex: 2, sentenceStart: 0, sentenceEnd: 0,
        translatedText: 'Alpha, translated.', showingOriginal: false,
    }]);

    const remaining = env.document.querySelectorAll('.tr-loading');
    assert.strictEqual(remaining.length, 1, 'only the matching (pi=2, a=0) placeholder is cleared');
    assert.strictEqual(
        remaining[0].dataset.loadKey, 'null:2:2:2',
        'the OTHER in-flight run in the same paragraph (a=2) must survive untouched');
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

// Iter 7: the blob now lives in a layer that is the ROOT's first child, not the paragraph's — a
// paragraph fragmented across pager columns still fragments its own boxes, so anchoring inside it
// was never safe (see _blobFromEls). The layer painting before every paragraph is what keeps the
// glass under the text now; _unwrapParagraph no longer has a blob node to recognize or skip inside
// a paragraph at all (B-2's specific hazard is structurally unreachable, not just avoided by order).
test('z-order: the blob layer is the first child of the root, so the glass paints before every paragraph', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    const pager = env.document.getElementById('_pager');

    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);

    const children = pager.childNodes;
    const layerIndex = children.findIndex((node) => node.className === 'tr-blob-layer');
    const paragraphIndex = children.indexOf(paragraphs[0]);
    assert.strictEqual(layerIndex, 0, 'the layer must be the very first child of the root');
    assert.ok(layerIndex < paragraphIndex, 'the blob layer must come before every paragraph');

    const layer = children[layerIndex];
    const maskIndex = layer.childNodes.findIndex((node) => node.className === 'tr-blob');
    // The blob outline is a real SVG element: its className is not a plain string (see
    // FakeSvgElement in harness.js), so this reads the reflected class attribute instead — the
    // same string-safe pattern production code uses via _hasClass (B-2).
    const svgIndex = layer.childNodes.findIndex((node) => node.getAttribute && node.getAttribute('class') === 'tr-blob-svg');
    assert.notStrictEqual(maskIndex, -1);
    assert.notStrictEqual(svgIndex, -1);
});

test('sweep: the layer holds no orphaned blob after a selection is cleared', () => {
    const env = loadFull();
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    const pager = env.document.getElementById('_pager');
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    const layer = pager.childNodes[0];
    assert.strictEqual(layer.childNodes.length, 2, 'the mask and svg pair for the active selection');

    env.window.clearSnippetSelection();

    assert.strictEqual(layer.childNodes.length, 0);
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

// Iter 6 (D-B): SetupSnippetLayerAsync measures blobs right after mount, before the async book/Inter
// fonts and the pagination reflow that follows settle - these three triggers keep every blob's
// geometry honest afterwards, without touching the frozen translation/paginated/scroll.js files.

test('refreshSnippetBlobs: exposes _renderAllBlobs for the C# side to call after page navigation', () => {
    const env = loadSnippets();

    assert.strictEqual(env.window.refreshSnippetBlobs, env.window._renderAllBlobs);
});

test('mount: observes each wrapped paragraph with a ResizeObserver when the host supports it', () => {
    const env = loadFull({ resizeObserver: true });
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);

    const observer = env.resizeObserverInstances[0];
    assert.ok(observer, 'a ResizeObserver instance should have been created');
    assert.ok(observer.targets.includes(paragraphs[0]));
});

test('unmount: disconnects the ResizeObserver', () => {
    const env = loadFull({ resizeObserver: true });
    mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    const observer = env.resizeObserverInstances[0];

    env.window.unmountSnippetLayer();

    assert.strictEqual(observer.disconnected, true);
});

test('resize observer: a size change on a wrapped paragraph re-measures its blob through the fallback timer (no rAF in this harness)', () => {
    const env = loadFull({ resizeObserver: true });
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    const spans = paragraphs[0].querySelectorAll('[data-si]');
    spans[0].rect = { top: 0, left: 0, right: 40, bottom: 20, width: 40, height: 20 };
    env.window._renderAllBlobs();
    const before = env.window._blobs.get('sel:0:0').mask.style.clipPath;
    const observer = env.resizeObserverInstances[0];
    // bridge.js's own "ready" retry already keeps one timer pending in this harness (no host ever
    // accepts the message here), so this asserts the DELTA the resize callback adds, not an
    // absolute queue length.
    const pendingBefore = env.timers.length;

    spans[0].rect = { top: 0, left: 0, right: 300, bottom: 20, width: 300, height: 20 };
    observer.callback();

    assert.strictEqual(env.timers.length, pendingBefore + 1, 'exactly one coalesced refresh is scheduled');
    env.runTimers();
    const after = env.window._blobs.get('sel:0:0').mask.style.clipPath;
    assert.notStrictEqual(after, before);
});

test('mount: re-measures blobs once the async book/Inter fonts finish loading', async () => {
    let resolveReady;
    const ready = new Promise((resolve) => { resolveReady = resolve; });
    const env = loadFull({ fonts: { ready } });
    const paragraphs = mountWithParagraphs(env, ['Ela chegou. Ele saiu.']);
    tap(env, paragraphs[0].querySelectorAll('[data-si]')[0]);
    const spans = paragraphs[0].querySelectorAll('[data-si]');
    spans[0].rect = { top: 0, left: 0, right: 40, bottom: 20, width: 40, height: 20 };
    env.window._renderAllBlobs();
    const before = env.window._blobs.get('sel:0:0').mask.style.clipPath;

    spans[0].rect = { top: 0, left: 0, right: 300, bottom: 20, width: 300, height: 20 };
    resolveReady();
    await ready;

    const after = env.window._blobs.get('sel:0:0').mask.style.clipPath;
    assert.notStrictEqual(after, before);
});

test('mount: does not throw when neither ResizeObserver nor document.fonts is supported', () => {
    const env = loadFull();

    assert.doesNotThrow(() => mountWithParagraphs(env, ['Ela chegou. Ele saiu.']));
});
