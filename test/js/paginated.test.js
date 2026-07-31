'use strict';

const test = require('node:test');
const assert = require('node:assert');
const { createEnv } = require('./harness');

const VIEWPORT_WIDTH = 400;

function loadPaginated(options) {
    const env = createEnv(options);
    env.load('paginated.js');
    return env;
}

function addContainer(env, html) {
    const container = env.document.createElement('div');
    container.id = 'chapter-container';
    container.innerHTML = html;
    env.document.body.appendChild(container);
    return container;
}

function layout(env, { width = VIEWPORT_WIDTH, scrollWidth = 0 } = {}) {
    const viewport = env.document.getElementById('_viewport');
    viewport.offsetWidth = width;
    env.document.getElementById('_pager').scrollWidth = scrollWidth;
    return viewport;
}

test('initPagination moves the chapter content into a pager inside a new viewport', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p><p>two</p>');

    env.window.initPagination();

    const viewport = env.document.getElementById('_viewport');
    const pager = env.document.getElementById('_pager');
    assert.notStrictEqual(viewport, null);
    assert.strictEqual(pager.parentNode, viewport);
    assert.strictEqual(viewport.parentNode, env.document.body);
    assert.strictEqual(pager.childNodes.length, 2);
    assert.strictEqual(env.document.body.style.overflow, 'hidden');
});

test('initPagination hides and empties the original container', () => {
    const env = loadPaginated();
    const container = addContainer(env, '<p>one</p>');

    env.window.initPagination();

    assert.strictEqual(container.style.display, 'none');
    assert.strictEqual(container.childNodes.length, 0);
});

test('initPagination removes a viewport left over from a previous call', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');

    env.window.initPagination();
    const first = env.document.getElementById('_viewport');
    env.window.initPagination();
    const second = env.document.getElementById('_viewport');

    assert.notStrictEqual(first, second);
    assert.strictEqual(first.parentNode, null);
    assert.ok(env.logged('log', 'Removing old viewport'));
    assert.strictEqual(second.parentNode, env.document.body);
});

test('initPagination falls back to the document body when the container is missing', () => {
    const env = loadPaginated();
    env.document.body.innerHTML = '<p>loose</p>';

    env.window.initPagination();

    assert.ok(env.logged('warn', 'chapter-container NOT FOUND'));
    assert.strictEqual(env.document.getElementById('_pager').childNodes.length, 1);
});

test('initPagination applies the column layout to the pager', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');

    env.window.initPagination();
    const viewport = env.document.getElementById('_viewport');
    viewport.offsetWidth = 500;
    env.window.goToPage(0);

    const style = env.document.getElementById('_pager').style;
    assert.strictEqual(style.height, '100vh');
    assert.strictEqual(style.padding, '16px 24px');
    assert.strictEqual(style.columnGap, '48px');
    assert.strictEqual(style.boxSizing, 'border-box');
    assert.strictEqual(style.columnFill, 'auto');
});

test('the layout uses the viewport width minus the horizontal padding', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    const viewport = env.document.getElementById('_viewport');
    viewport.offsetWidth = 500;

    env.fireWindow('resize');

    assert.strictEqual(env.document.getElementById('_pager').style.columnWidth, '452px');
});

test('the resize handler is a no-op before pagination is initialised', () => {
    const env = loadPaginated();

    env.fireWindow('resize');

    assert.strictEqual(env.document.getElementById('_pager'), null);
});

test('the resize handler restores the current page', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 3 });
    env.window.goToPage(2);

    env.fireWindow('resize');

    assert.strictEqual(
        env.document.getElementById('_pager').style.transform,
        `translateX(-${VIEWPORT_WIDTH * 2}px)`);
});

test('getTotalPages returns a single page before pagination is initialised', () => {
    const env = loadPaginated();

    assert.strictEqual(env.window.getTotalPages(), 1);
});

test('getTotalPages divides the pager scroll width by the viewport width', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 3 });

    assert.strictEqual(env.window.getTotalPages(), 3);
});

test('getTotalPages rounds a partial trailing page up', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 2 + 1 });

    assert.strictEqual(env.window.getTotalPages(), 3);
});

test('getTotalPages returns a single page when the viewport has no width yet', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { width: 0, scrollWidth: 900 });

    assert.strictEqual(env.window.getTotalPages(), 1);
});

test('goToPage reports the neutral position before pagination is initialised', () => {
    const env = loadPaginated();

    const info = env.window.goToPage(3);

    assert.strictEqual(info.current, 0);
    assert.strictEqual(info.total, 1);
});

test('goToPage translates the pager by whole viewport steps', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 4 });

    const info = env.window.goToPage(2);

    assert.strictEqual(info.current, 2);
    assert.strictEqual(info.total, 4);
    assert.strictEqual(
        env.document.getElementById('_pager').style.transform,
        `translateX(-${VIEWPORT_WIDTH * 2}px)`);
});

test('goToPage clamps a negative index to the first page', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 4 });

    assert.strictEqual(env.window.goToPage(-5).current, 0);
});

test('goToPage clamps an out-of-range index to the last page', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 4 });

    assert.strictEqual(env.window.goToPage(99).current, 3);
});

test('nextPage and prevPage walk one page at a time', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 3 });

    assert.strictEqual(env.window.nextPage().current, 1);
    assert.strictEqual(env.window.nextPage().current, 2);
    assert.strictEqual(env.window.prevPage().current, 1);
});

test('getPageInfo reports the current position without moving the pager', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 3 });
    env.window.goToPage(1);

    const info = env.window.getPageInfo();

    assert.strictEqual(info.current, 1);
    assert.strictEqual(info.total, 3);
});

test('goToLastPage jumps to the final page', () => {
    const env = loadPaginated();
    addContainer(env, '<p>one</p>');
    env.window.initPagination();
    layout(env, { scrollWidth: VIEWPORT_WIDTH * 5 });

    const info = env.window.goToLastPage();

    assert.strictEqual(info.current, 4);
    assert.strictEqual(info.total, 5);
});
