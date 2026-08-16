'use strict';

const test = require('node:test');
const assert = require('node:assert');
const { createEnv } = require('./harness');

const READY = 'ready';
const RAW_READY = '__RawMessage|ready';

function loadBridge(options, configure) {
    const env = createEnv(options);
    if (configure) {
        configure(env);
    }
    env.load('bridge.js');
    return env;
}

function addContainer(env) {
    const container = env.document.createElement('div');
    container.id = 'chapter-container';
    env.document.body.appendChild(container);
    return container;
}

test('applyCss writes into the reader theme style element', () => {
    const env = loadBridge();
    const style = env.document.createElement('style');
    style.id = 'reader-theme';
    env.document.body.appendChild(style);

    env.window.applyCss('body{color:red}');

    assert.strictEqual(style.textContent, 'body{color:red}');
});

test('applyCss is a no-op when the theme element is absent', () => {
    const env = loadBridge();

    env.window.applyCss('body{color:red}');

    assert.strictEqual(env.document.getElementById('reader-theme'), null);
});

test('loadChapter injects the html and repaginates in paginated mode', () => {
    const env = loadBridge();
    const container = addContainer(env);
    let paginated = 0;
    env.window.initPagination = () => { paginated += 1; };

    env.window.loadChapter('<p>capitulo</p>');

    assert.strictEqual(container.style.display, 'block');
    assert.strictEqual(container.childNodes.length, 1);
    assert.strictEqual(paginated, 1);
});

test('loadChapter creates the container when the document has none', () => {
    const env = loadBridge();
    env.window.setMode('scroll');

    env.window.loadChapter('<p>capitulo</p>');

    const container = env.document.getElementById('chapter-container');
    assert.notStrictEqual(container, null);
    assert.strictEqual(container.parentNode, env.document.body);
    assert.ok(env.logged('log', 'chapter-container not found, creating it'));
});

test('loadChapter drops a viewport left over from the previous chapter', () => {
    const env = loadBridge();
    addContainer(env);
    const viewport = env.document.createElement('div');
    viewport.id = '_viewport';
    env.document.body.appendChild(viewport);
    env.window.setMode('scroll');

    env.window.loadChapter('<p>capitulo</p>');

    assert.strictEqual(env.document.getElementById('_viewport'), null);
    assert.ok(env.logged('log', 'Removing old viewport before loadChapter'));
});

test('setMode switches loadChapter away from repagination', () => {
    const env = loadBridge();
    addContainer(env);
    let paginated = 0;
    env.window.initPagination = () => { paginated += 1; };

    env.window.setMode('scroll');
    env.window.loadChapter('<p>capitulo</p>');

    assert.strictEqual(paginated, 0);
});

test('loadChapter accepts empty html without breaking the length log', () => {
    const env = loadBridge();
    const container = addContainer(env);
    env.window.setMode('scroll');

    env.window.loadChapter('');

    assert.strictEqual(container.childNodes.length, 0);
    assert.ok(env.logged('log', 'loadChapter called, html length: 0'));
});

test('loadScrollContent accepts empty html without breaking the length log', () => {
    const env = loadBridge();
    addContainer(env);

    env.window.loadScrollContent('');

    assert.ok(env.logged('log', 'loadScrollContent called, html length: 0'));
});

test('loadScrollContent re-enables body scrolling and injects the html', () => {
    const env = loadBridge();
    const container = addContainer(env);

    env.window.loadScrollContent('<p>a</p><p>b</p>');

    assert.strictEqual(env.document.body.style.overflow, 'auto');
    assert.strictEqual(container.style.display, 'block');
    assert.strictEqual(container.childNodes.length, 2);
});

test('loadScrollContent creates the container and removes the pager viewport', () => {
    const env = loadBridge();
    const viewport = env.document.createElement('div');
    viewport.id = '_viewport';
    env.document.body.appendChild(viewport);

    env.window.loadScrollContent('<p>a</p>');

    assert.notStrictEqual(env.document.getElementById('chapter-container'), null);
    assert.strictEqual(env.document.getElementById('_viewport'), null);
});

test('appendChunk accumulates and flushChunk forwards the whole buffer', () => {
    const env = loadBridge();
    const received = [];
    env.window.sink = (text) => received.push(text);

    env.window.appendChunk('<p>par');
    env.window.appendChunk('te</p>');
    env.window.flushChunk('sink');

    assert.deepStrictEqual(received, ['<p>parte</p>']);
    assert.strictEqual(env.window.__injectionBuffer, '');
});

test('flushChunk clears the buffer and logs when the target function throws', () => {
    const env = loadBridge();
    env.window.sink = () => { throw new Error('boom'); };

    env.window.appendChunk('x');
    env.window.flushChunk('sink');

    assert.ok(env.logged('error', 'Error calling sink'));
    assert.strictEqual(env.window.__injectionBuffer, '');
});

test('flushChunk reports a missing target function', () => {
    const env = loadBridge();

    env.window.appendChunk('x');
    env.window.flushChunk('doesNotExist');

    assert.ok(env.logged('error', 'Function not found: doesNotExist'));
    assert.strictEqual(env.window.__injectionBuffer, '');
});

test('the ready signal prefers the HybridWebView raw message channel', () => {
    const messages = [];
    loadBridge(undefined, (env) => {
        env.window.HybridWebView = { SendRawMessage: (message) => messages.push(message) };
    });

    assert.deepStrictEqual(messages, [READY]);
});

test('the ready signal falls back to the WebView2 host', () => {
    const messages = [];
    const env = loadBridge(undefined, (candidate) => {
        candidate.window.chrome = { webview: { postMessage: (message) => messages.push(message) } };
    });

    assert.deepStrictEqual(messages, [RAW_READY]);
    assert.strictEqual(env.timers.length, 0);
});

test('the ready signal falls back to the WKWebView message handler', () => {
    const messages = [];
    loadBridge(undefined, (env) => {
        env.window.webkit = {
            messageHandlers: { webwindowinterop: { postMessage: (message) => messages.push(message) } },
        };
    });

    assert.deepStrictEqual(messages, [RAW_READY]);
});

test('the ready signal falls back to the Android host object', () => {
    const messages = [];
    loadBridge(undefined, (env) => {
        env.window.hybridWebViewHost = { sendMessage: (message) => messages.push(message) };
    });

    assert.deepStrictEqual(messages, [RAW_READY]);
});

test('the ready signal retries later when no host is available yet', () => {
    const env = loadBridge();

    assert.strictEqual(env.timers.length, 1);
    assert.strictEqual(env.timers[0].delay, 100);

    const messages = [];
    env.window.hybridWebViewHost = { sendMessage: (message) => messages.push(message) };
    env.runTimers();

    assert.deepStrictEqual(messages, [RAW_READY]);
});

test('the ready signal retries later when probing the host throws', () => {
    const env = loadBridge(undefined, (candidate) => {
        candidate.window.HybridWebView = {
            get SendRawMessage() { throw new Error('host not ready'); },
        };
    });

    assert.strictEqual(env.timers.length, 1);
    assert.strictEqual(env.timers[0].delay, 100);
});

test('sendRawMessage reports false when no host is available', () => {
    const env = loadBridge();

    assert.strictEqual(env.window.sendRawMessage('snip|[]'), false);
});

test('the ready signal waits for DOMContentLoaded while the document is still loading', () => {
    const messages = [];
    const env = loadBridge({ readyState: 'loading' }, (candidate) => {
        candidate.window.hybridWebViewHost = { sendMessage: (message) => messages.push(message) };
    });

    assert.deepStrictEqual(messages, []);

    env.fireDocument('DOMContentLoaded');

    assert.deepStrictEqual(messages, [RAW_READY]);
});
