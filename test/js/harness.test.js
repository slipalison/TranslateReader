'use strict';

const test = require('node:test');
const assert = require('node:assert');
const { createEnv } = require('./harness');

// The harness itself is test infrastructure shared by every production script, so a regression
// here is invisible in the suites that consume it. These tests pin the one CSS feature the
// production code needs beyond a single simple selector: selector groups separated by commas.

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
