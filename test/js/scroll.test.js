'use strict';

const test = require('node:test');
const assert = require('node:assert');
const { createEnv } = require('./harness');

const VIEW_HEIGHT = 600;
const MIDPOINT = VIEW_HEIGHT / 2;

function loadScroll() {
    const env = createEnv({ innerHeight: VIEW_HEIGHT });
    env.load('scroll.js');
    return env;
}

function addChapter(env, { href, index, top, bottom, offsetTop = 0, offsetHeight = 0 }) {
    const chapter = env.document.createElement('div');
    chapter.className = 'chapter-content';
    chapter.dataset.chapterHref = href;
    chapter.dataset.chapterIndex = String(index);
    chapter.rect = { top, bottom, left: 0, right: 0, width: 0, height: bottom - top };
    chapter.offsetTop = offsetTop;
    chapter.offsetHeight = offsetHeight;
    env.document.body.appendChild(chapter);
    return chapter;
}

test('getScrollInfo returns the neutral position when the document has no chapters', () => {
    const env = loadScroll();

    const info = env.window.getScrollInfo();

    assert.strictEqual(info.chapterHRef, '');
    assert.strictEqual(info.chapterIndex, 0);
    assert.strictEqual(info.relativeScroll, 0);
});

test('getScrollInfo picks the chapter crossing the vertical midpoint', () => {
    const env = loadScroll();
    addChapter(env, { href: 'c1.xhtml', index: 0, top: -900, bottom: MIDPOINT - 1 });
    addChapter(env, {
        href: 'c2.xhtml', index: 1, top: MIDPOINT - 1, bottom: MIDPOINT + 500,
        offsetTop: 1000, offsetHeight: 500,
    });
    env.window.scrollY = 1250;

    const info = env.window.getScrollInfo();

    assert.strictEqual(info.chapterHRef, 'c2.xhtml');
    assert.strictEqual(info.chapterIndex, 1);
    assert.strictEqual(info.relativeScroll, 0.5);
});

test('getScrollInfo reports no relative scroll for a chapter with no measured height', () => {
    const env = loadScroll();
    addChapter(env, {
        href: 'c1.xhtml', index: 0, top: 0, bottom: VIEW_HEIGHT,
        offsetTop: 0, offsetHeight: 0,
    });
    env.window.scrollY = 40;

    assert.strictEqual(env.window.getScrollInfo().relativeScroll, 0);
});

test('getScrollInfo falls back to the last chapter when none crosses the midpoint', () => {
    const env = loadScroll();
    addChapter(env, { href: 'c1.xhtml', index: 0, top: -2000, bottom: -1000 });
    addChapter(env, { href: 'c2.xhtml', index: 1, top: -1000, bottom: -10 });

    const info = env.window.getScrollInfo();

    assert.strictEqual(info.chapterHRef, 'c2.xhtml');
    assert.strictEqual(info.chapterIndex, 1);
    assert.strictEqual(info.relativeScroll, 1);
});

test('getScrollInfo falls back to pageYOffset when scrollY is zero', () => {
    const env = loadScroll();
    addChapter(env, {
        href: 'c1.xhtml', index: 0, top: 0, bottom: VIEW_HEIGHT,
        offsetTop: 0, offsetHeight: 400,
    });
    env.window.scrollY = 0;
    env.window.pageYOffset = 200;

    assert.strictEqual(env.window.getScrollInfo().relativeScroll, 0.5);
});

test('scrollToChapter is a no-op for an unknown chapter href', () => {
    const env = loadScroll();
    addChapter(env, { href: 'c1.xhtml', index: 0, top: 0, bottom: 10 });

    env.window.scrollToChapter('missing.xhtml', 0.5);

    assert.deepStrictEqual(env.scrollCalls, []);
});

test('scrollToChapter scrolls to the requested fraction of the chapter', () => {
    const env = loadScroll();
    addChapter(env, {
        href: 'c2.xhtml', index: 1, top: 0, bottom: 10,
        offsetTop: 1000, offsetHeight: 400,
    });

    env.window.scrollToChapter('c2.xhtml', 0.25);

    assert.deepStrictEqual(env.scrollCalls, [{ x: 0, y: 1100 }]);
});

test('scrollToChapter without a relative position scrolls to the chapter top', () => {
    const env = loadScroll();
    addChapter(env, {
        href: 'c2.xhtml', index: 1, top: 0, bottom: 10,
        offsetTop: 1000, offsetHeight: 400,
    });

    env.window.scrollToChapter('c2.xhtml');

    assert.deepStrictEqual(env.scrollCalls, [{ x: 0, y: 1000 }]);
});
