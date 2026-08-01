'use strict';

const test = require('node:test');
const assert = require('node:assert');
const { createEnv } = require('./harness');

const WINDOW_WIDTH = 500;
const VIEWPORT_WIDTH = 400;

// translation.js reads `_stepW` and `_currentPage`, which are declared by
// paginated.js: both files must share one context, exactly as they do in the WebView.
function loadTranslation(options) {
    const env = createEnv({ innerWidth: WINDOW_WIDTH, ...options });
    env.load('paginated.js');
    env.load('translation.js');
    return env;
}

function addPager(env, paragraphs) {
    const pager = env.document.createElement('div');
    pager.id = '_pager';
    env.document.body.appendChild(pager);
    for (const paragraph of paragraphs) {
        const element = env.document.createElement('p');
        element.textContent = paragraph.text;
        element.offsetLeft = paragraph.offsetLeft ?? 0;
        if (paragraph.original !== undefined) {
            element.dataset.original = paragraph.original;
        }
        pager.appendChild(element);
    }
    return pager;
}

function paginate(env, html) {
    const container = env.document.createElement('div');
    container.id = 'chapter-container';
    container.innerHTML = html;
    env.document.body.appendChild(container);
    env.window.initPagination();
    env.document.getElementById('_viewport').offsetWidth = VIEWPORT_WIDTH;
    return env.document.getElementById('_pager');
}

test('getVisibleParagraphs returns nothing before a chapter is paginated', () => {
    const env = loadTranslation();

    assert.strictEqual(env.window.getVisibleParagraphs().length, 0);
});

test('getVisibleParagraphs falls back to the window width while no viewport exists', () => {
    const env = loadTranslation();
    addPager(env, [
        { text: 'primeiro', offsetLeft: 0 },
        { text: 'ainda visivel', offsetLeft: WINDOW_WIDTH - 1 },
        { text: 'proxima pagina', offsetLeft: WINDOW_WIDTH },
    ]);

    const visible = env.window.getVisibleParagraphs();

    assert.strictEqual(visible.length, 2);
    assert.strictEqual(visible[0].index, 0);
    assert.strictEqual(visible[1].index, 1);
});

test('getVisibleParagraphs skips paragraphs without text', () => {
    const env = loadTranslation();
    addPager(env, [{ text: '   ' }, { text: 'com texto' }]);

    const visible = env.window.getVisibleParagraphs();

    assert.strictEqual(visible.length, 1);
    assert.strictEqual(visible[0].text, 'com texto');
});

test('getVisibleParagraphs reports the original text of an already translated paragraph', () => {
    const env = loadTranslation();
    addPager(env, [{ text: 'traduzido', original: 'original text' }]);

    assert.strictEqual(env.window.getVisibleParagraphs()[0].text, 'original text');
});

test('getVisibleParagraphs uses the viewport width of the current page', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<p>um</p><p>dois</p>');
    pager.scrollWidth = VIEWPORT_WIDTH * 2;
    pager.childNodes[0].offsetLeft = 0;
    pager.childNodes[1].offsetLeft = VIEWPORT_WIDTH;
    env.window.goToPage(1);

    const visible = env.window.getVisibleParagraphs();

    assert.strictEqual(visible.length, 1);
    assert.strictEqual(visible[0].index, 1);
    assert.strictEqual(visible[0].text, 'dois');
});

test('applyTranslations keeps the original text and repaints the current page', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<p>one</p><p>two</p>');
    pager.scrollWidth = VIEWPORT_WIDTH;

    env.window.applyTranslations([{ index: 1, translated: 'dois' }]);

    assert.strictEqual(pager.childNodes[1].textContent, 'dois');
    assert.strictEqual(pager.childNodes[1].dataset.original, 'two');
    assert.strictEqual(pager.style.transform, 'translateX(-0px)');
});

test('applyTranslations does not overwrite an original captured earlier', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<p>one</p>');

    env.window.applyTranslations([{ index: 0, translated: 'um' }]);
    env.window.applyTranslations([{ index: 0, translated: 'unico' }]);

    assert.strictEqual(pager.childNodes[0].textContent, 'unico');
    assert.strictEqual(pager.childNodes[0].dataset.original, 'one');
});

test('applyTranslations ignores indexes outside the paragraph list', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<p>one</p>');

    env.window.applyTranslations([
        { index: -1, translated: 'ignorado' },
        { index: 9, translated: 'ignorado' },
    ]);

    assert.strictEqual(pager.childNodes[0].textContent, 'one');
});

test('applyTranslations without items does nothing', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<p>one</p>');

    env.window.applyTranslations(null);

    assert.strictEqual(pager.childNodes[0].textContent, 'one');
});

test('applyTranslations does nothing before a chapter is paginated', () => {
    const env = loadTranslation();

    env.window.applyTranslations([{ index: 0, translated: 'um' }]);

    assert.strictEqual(env.document.getElementById('_pager'), null);
});

test('applyTranslations logs instead of throwing when the payload is not iterable', () => {
    const env = loadTranslation();
    paginate(env, '<p>one</p>');

    env.window.applyTranslations(42);

    assert.ok(env.logged('error', 'applyTranslations error:'));
});

test('clearTranslations restores every original and drops the marker', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<p>one</p><p>two</p>');
    env.window.applyTranslations([{ index: 0, translated: 'um' }]);

    env.window.clearTranslations();

    assert.strictEqual(pager.childNodes[0].textContent, 'one');
    assert.strictEqual(pager.childNodes[0].dataset.original, undefined);
    assert.strictEqual(pager.childNodes[1].textContent, 'two');
});

test('clearTranslations does nothing before a chapter is paginated', () => {
    const env = loadTranslation();

    env.window.clearTranslations();

    assert.strictEqual(env.document.getElementById('_pager'), null);
});

// Same markup as CalibreFixtures.PartiallyCoveredBody on the C# side: calibre wraps every
// paragraph in a leaf `div class="calibreN"` and never emits a `p`. The wrapper div, the image
// only div and the bullet only div are the three shapes that must stay out of the paragraph list.
const CALIBRE_BODY = [
    '<div class="calibre1">',
    '<div class="calibre2">First calibre paragraph with real text.</div>',
    '<div class="calibre2">Second calibre paragraph with more text.</div>',
    '<div class="calibre3"><img src="fig1.png"/></div>',
    '<div class="calibre2">&#8226;</div>',
    '<div class="calibre2">Third paragraph, letters only matter here.</div>',
    '</div>',
].join('\n');

const NO_PARAGRAPH_WARNING = 'no translatable paragraph';

test('getVisibleParagraphs returns every calibre leaf div that holds letters', () => {
    const env = loadTranslation();
    paginate(env, CALIBRE_BODY);

    const visible = env.window.getVisibleParagraphs();

    assert.deepStrictEqual(visible.map((paragraph) => paragraph.text), [
        'First calibre paragraph with real text.',
        'Second calibre paragraph with more text.',
        'Third paragraph, letters only matter here.',
    ]);
});

test('getVisibleParagraphs leaves the calibre wrapper, the image div and the bullet div out', () => {
    const env = loadTranslation();
    paginate(env, CALIBRE_BODY);

    const texts = env.window.getVisibleParagraphs().map((paragraph) => paragraph.text);

    assert.strictEqual(texts.length, 3);
    assert.ok(!texts.some((text) => text.includes('First calibre') && text.includes('Second calibre')),
        'the non-leaf wrapper collapsed the whole chapter into one block');
    assert.ok(!texts.some((text) => text.includes('&#8226;')), 'the bullet only div became a paragraph');
    assert.ok(!texts.some((text) => text.trim() === ''), 'the image only div became a paragraph');
});

test('getVisibleParagraphs indexes p and calibre div elements in document order', () => {
    const env = loadTranslation();
    paginate(env, '<p>one</p><div>two</div><p>three</p>');

    const visible = env.window.getVisibleParagraphs();

    assert.deepStrictEqual(visible, [
        { index: 0, text: 'one' },
        { index: 1, text: 'two' },
        { index: 2, text: 'three' },
    ]);
});

test('applyTranslations writes into the calibre div the reported index points at', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<p>one</p><div>two</div><p>three</p>');
    const reported = env.window.getVisibleParagraphs()[1];

    env.window.applyTranslations([{ index: reported.index, translated: 'dois' }]);

    assert.strictEqual(pager.childNodes[1].tagName, 'DIV');
    assert.strictEqual(pager.childNodes[1].textContent, 'dois');
    assert.strictEqual(pager.childNodes[1].dataset.original, 'two');
    assert.strictEqual(pager.childNodes[0].textContent, 'one');
    assert.strictEqual(pager.childNodes[2].textContent, 'three');
});

test('clearTranslations restores a translated calibre div and drops the marker', () => {
    const env = loadTranslation();
    const pager = paginate(env, '<div class="calibre2">only paragraph</div>');
    env.window.applyTranslations([{ index: 0, translated: 'unico paragrafo' }]);
    assert.strictEqual(pager.childNodes[0].textContent, 'unico paragrafo');

    env.window.clearTranslations();

    assert.strictEqual(pager.childNodes[0].textContent, 'only paragraph');
    assert.strictEqual(pager.childNodes[0].dataset.original, undefined);
});

test('getVisibleParagraphs warns when the page has text but no translatable paragraph', () => {
    const env = loadTranslation();
    paginate(env, '<span>so span</span>');

    const visible = env.window.getVisibleParagraphs();

    assert.strictEqual(visible.length, 0);
    assert.ok(env.logged('warn', NO_PARAGRAPH_WARNING));
});

test('getVisibleParagraphs stays quiet when a calibre div is translatable', () => {
    const env = loadTranslation();
    paginate(env, CALIBRE_BODY);

    const visible = env.window.getVisibleParagraphs();

    assert.strictEqual(visible.length, 3);
    assert.strictEqual(env.logged('warn', NO_PARAGRAPH_WARNING), false);
});
