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
