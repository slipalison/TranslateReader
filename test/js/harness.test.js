'use strict';

const test = require('node:test');
const assert = require('node:assert');
const { createEnv } = require('./harness');

// The harness itself is test infrastructure shared by every production script, so a regression
// here is invisible in the suites that consume it. These tests pin the CSS features the production
// code needs beyond a single simple selector: selector groups separated by commas, and failing
// CLOSED on a selector the harness cannot parse the way a real DOM does.

function withBody(html) {
    const env = createEnv();
    env.document.body.innerHTML = html;
    return env;
}

function tagNames(elements) {
    return elements.map((element) => element.tagName);
}

test('querySelectorAll with a selector group returns document order, not selector order', () => {
    const env = withBody('<p>um</p><div>dois</div><p>tres</p>');

    const found = env.document.querySelectorAll('div, p');

    assert.deepStrictEqual(tagNames(found), ['P', 'DIV', 'P']);
    assert.deepStrictEqual(found.map((element) => element.textContent), ['um', 'dois', 'tres']);
});

test('querySelectorAll with a selector group yields an element matching two parts only once', () => {
    const env = withBody('<div class="calibre2">unico</div>');

    const found = env.document.querySelectorAll('div, .calibre2');

    assert.strictEqual(found.length, 1);
    assert.strictEqual(found[0].textContent, 'unico');
});

test('querySelectorAll does not split a comma inside an attribute value', () => {
    const env = withBody(
        '<div data-chapter-href="a,b.xhtml">alvo</div><div data-chapter-href="c.xhtml">outro</div>');

    const found = env.document.querySelectorAll('[data-chapter-href="a,b.xhtml"]');

    assert.strictEqual(found.length, 1);
    assert.strictEqual(found[0].textContent, 'alvo');
});

test('querySelectorAll keeps an attribute value holding a comma and a bracket in one selector', () => {
    const env = withBody(
        '<div data-chapter-href="a,b].xhtml">alvo</div><div data-chapter-href="c.xhtml">outro</div>');

    const found = env.document.querySelectorAll('[data-chapter-href="a,b].xhtml"]');

    assert.strictEqual(found.length, 1);
    assert.strictEqual(found[0].textContent, 'alvo');
});

// scroll.js:32 concatenates an EPUB href — untrusted input — into `[data-chapter-href="<href>"]`.
// A quote in that href builds a selector a real WebView rejects outright; the harness used to skip
// the text it could not read and end up with a matcher that accepted EVERY element, so a test could
// go green over code that selects the wrong chapter (or the whole document) in production.
test('querySelectorAll refuses an unparseable selector instead of matching every element', () => {
    const env = withBody('<div data-chapter-href="ok.xhtml"><span>x</span></div>');
    const href = 'ch"1';

    assert.throws(
        () => env.document.querySelectorAll('[data-chapter-href="' + href + '"]'),
        SyntaxError);
});

test('querySelector refuses an unparseable selector instead of reporting not-found', () => {
    const env = withBody('<div data-chapter-href="ok.xhtml"><span>x</span></div>');
    const href = 'ch"1';

    assert.throws(
        () => env.document.querySelector('[data-chapter-href="' + href + '"]'),
        SyntaxError);
    assert.throws(() => env.document.body.querySelector(']]garbage(('), SyntaxError);
});

test('querySelectorAll refuses an empty part of a selector group', () => {
    const env = withBody('<p>um</p><div>dois</div>');

    assert.throws(() => env.document.querySelectorAll('p,'), SyntaxError);
});

test('querySelectorAll keeps a comma-free selector matching exactly what it matched before', () => {
    const env = withBody('<div><p class="a">um</p><span>x</span><p class="b">dois</p></div>');

    assert.deepStrictEqual(tagNames(env.document.querySelectorAll('p')), ['P', 'P']);
    assert.deepStrictEqual(
        env.document.querySelectorAll('p.b').map((element) => element.textContent), ['dois']);
    assert.strictEqual(env.document.querySelectorAll('article').length, 0);
});

test('querySelectorAll tolerates whitespace around every part of a selector group', () => {
    const env = withBody('<h1>titulo</h1><p>texto</p><li>item</li>');

    const found = env.document.querySelectorAll('  p ,\th1 ,  li  ');

    assert.deepStrictEqual(tagNames(found), ['H1', 'P', 'LI']);
});

test('querySelector with a selector group returns the first element in document order', () => {
    const env = withBody('<p>um</p><div>dois</div>');

    assert.strictEqual(env.document.querySelector('div, p').textContent, 'um');
    assert.strictEqual(env.document.body.querySelector('div, p').tagName, 'P');
});

// The snippet blob geometry (D-2026-08-09-snippet-translation-4) needs multi-line rects, ancestor
// lookup and hit-testing that querySelectorAll alone cannot provide — these three capabilities are
// what snippets.js relies on to group periods into lines and to resolve drag targets.

test('getClientRects returns the set rect array, or falls back to the single element rect', () => {
    const env = withBody('<p>texto</p>');
    const paragraph = env.document.body.querySelector('p');

    assert.deepStrictEqual(paragraph.getClientRects(), [paragraph.rect]);

    paragraph.rects = [
        { top: 0, left: 0, right: 10, bottom: 5, width: 10, height: 5 },
        { top: 5, left: 0, right: 8, bottom: 10, width: 8, height: 5 },
    ];

    assert.deepStrictEqual(paragraph.getClientRects(), paragraph.rects);
});

test('closest climbs the ancestor chain, including the element itself', () => {
    const env = withBody('<div><span data-pi="0"><em>x</em></span></div>');
    const em = env.document.body.querySelectorAll('em')[0];

    assert.strictEqual(em.closest('[data-pi]').tagName, 'SPAN');
    assert.strictEqual(em.closest('em').tagName, 'EM');
});

test('closest returns null when no ancestor matches', () => {
    const env = withBody('<div><span>x</span></div>');
    const span = env.document.body.querySelectorAll('span')[0];

    assert.strictEqual(span.closest('[data-pi]'), null);
});

test('elementFromPoint returns the topmost element containing the point, or null outside every rect', () => {
    const env = withBody('');
    const back = env.appendToBody(
        'div', { rect: { top: 0, left: 0, right: 100, bottom: 100, width: 100, height: 100 } });
    const front = env.appendToBody(
        'div', { rect: { top: 10, left: 10, right: 50, bottom: 50, width: 40, height: 40 } });

    assert.strictEqual(env.document.elementFromPoint(20, 20), front);
    assert.strictEqual(env.document.elementFromPoint(70, 70), back);
    assert.strictEqual(env.document.elementFromPoint(200, 200), null);
});

// snippets.js builds a glass blob's outline through createElementNS (B-2): a real SVGElement's
// className is a read-only SVGAnimatedString, not a string, and code that forgot that threw a
// TypeError invisible to every test here — because this harness used to have no createElementNS at
// all, so the fallback path always produced a harmless plain-string element instead.
const SVG_NS = 'http://www.w3.org/2000/svg';

test('createElementNS on the SVG namespace returns an element whose className is not a plain string', () => {
    const env = createEnv();

    const svg = env.document.createElementNS(SVG_NS, 'svg');

    assert.strictEqual(svg.tagName, 'SVG');
    assert.notStrictEqual(typeof svg.className, 'string');
});

test('createElementNS keeps getAttribute("class") working on an SVG element after setAttribute', () => {
    const env = createEnv();
    const svg = env.document.createElementNS(SVG_NS, 'svg');

    svg.setAttribute('class', 'tr-blob-svg');

    assert.strictEqual(svg.getAttribute('class'), 'tr-blob-svg');
    assert.notStrictEqual(typeof svg.className, 'string');
});

test('createElementNS outside the SVG namespace behaves like createElement', () => {
    const env = createEnv();

    const element = env.document.createElementNS('http://www.w3.org/1999/xhtml', 'div');

    assert.strictEqual(element.tagName, 'DIV');
    assert.strictEqual(typeof element.className, 'string');
});

test('querySelectorAll matches an SVG element by class even though its className is not a string', () => {
    const env = createEnv();
    const svg = env.document.createElementNS(SVG_NS, 'svg');
    svg.setAttribute('class', 'tr-blob-svg');
    env.document.body.appendChild(svg);

    const found = env.document.querySelectorAll('.tr-blob-svg');

    assert.strictEqual(found.length, 1);
    assert.strictEqual(found[0], svg);
});
