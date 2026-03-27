var _currentMode = 'paginated';

window.setMode = function (mode) {
    _currentMode = mode;
};

window.applyCss = function (css) {
    var el = document.getElementById('reader-theme');
    if (el) el.textContent = css;
};

window.loadChapter = function (html) {
    console.log('[JS] loadChapter called, html length: ' + (html ? html.length : 0));
    var container = document.getElementById('chapter-container');
    if (!container) {
        console.log('[JS] chapter-container not found, creating it');
        container = document.createElement('div');
        container.id = 'chapter-container';
        document.body.prepend(container);
    }
    
    // Reset state before injection
    var vp = document.getElementById('_viewport');
    if (vp) {
        console.log('[JS] Removing old viewport before loadChapter');
        vp.remove();
    }
    
    container.style.display = 'block';
    container.innerHTML = html;
    console.log('[JS] HTML injected into container, child nodes: ' + container.childNodes.length);
    
    if (_currentMode === 'paginated') {
        window.initPagination();
    }
};

window.loadScrollContent = function (html) {
    console.log('[JS] loadScrollContent called, html length: ' + (html ? html.length : 0));
    document.body.style.overflow = 'auto'; // Enable scroll
    var container = document.getElementById('chapter-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'chapter-container';
        document.body.prepend(container);
    }
    container.style.display = 'block';
    container.innerHTML = html;
    
    // In scroll mode, if there was a pager, we should probably remove it
    var vp = document.getElementById('_viewport');
    if (vp) vp.remove();
};

window.__injectionBuffer = '';
window.appendChunk = function (chunk) {
    window.__injectionBuffer += chunk;
};
window.flushChunk = function (fn) {
    console.log('[JS] Flushing chunk for function: ' + fn + ' (total length: ' + window.__injectionBuffer.length + ')');
    if (window[fn]) {
        try {
            window[fn](window.__injectionBuffer);
        } catch (e) {
            console.error('[JS] Error calling ' + fn + ': ' + e);
        }
    } else {
        console.error('[JS] Function not found: ' + fn);
    }
    window.__injectionBuffer = '';
};

function _sendReady() {
    try {
        if (window.HybridWebView && typeof window.HybridWebView.SendRawMessage === 'function') {
            window.HybridWebView.SendRawMessage('ready');
        } else if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage('__RawMessage|ready');
        } else if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.webwindowinterop) {
            window.webkit.messageHandlers.webwindowinterop.postMessage('__RawMessage|ready');
        } else if (window.hybridWebViewHost) {
            window.hybridWebViewHost.sendMessage('__RawMessage|ready');
        } else {
            setTimeout(_sendReady, 100);
        }
    } catch (e) {
        setTimeout(_sendReady, 100);
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', _sendReady);
} else {
    _sendReady();
}
