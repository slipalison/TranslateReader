var _vp, _pg, _currentPage = 0;

window.initPagination = function () {
    console.log('[JS] initPagination called');
    document.body.style.overflow = 'hidden'; // Disable body scroll in paginated mode
    var oldVp = document.getElementById('_viewport');
    if (oldVp) {
        console.log('[JS] Removing old viewport');
        oldVp.remove();
    }

    _vp = document.createElement('div');
    _vp.id = '_viewport';
    var vs = _vp.style;
    vs.overflow = 'hidden';
    vs.width = '100vw';
    vs.height = '100vh';
    vs.position = 'absolute'; // Changed from relative to absolute to avoid layout shifts
    vs.top = '0';
    vs.left = '0';

    _pg = document.createElement('div');
    _pg.id = '_pager';

    var container = document.getElementById('chapter-container');
    if (!container) {
        console.warn('[JS] chapter-container NOT FOUND during initPagination');
    } else {
        container.style.display = 'block';
    }
    var source = container || document.body;
    console.log('[JS] Source child nodes count: ' + source.childNodes.length);
    
    // Move all children from source to pager
    while (source.firstChild) {
        _pg.appendChild(source.firstChild);
    }

    if (container) {
        container.style.display = 'none';
        container.innerHTML = '';
    }
    
    _vp.appendChild(_pg);
    document.body.appendChild(_vp);

    _currentPage = 0;
    _applyLayout();
};

function _applyLayout() {
    var w = _vp.offsetWidth;
    var pad = 24;
    var colW = w - pad * 2;
    var s = _pg.style;
    s.height = '100vh';
    s.padding = '16px ' + pad + 'px';
    s.boxSizing = 'border-box';
    s.columnWidth = colW + 'px';
    s.columnGap = pad * 2 + 'px';
    s.columnFill = 'auto';
    s.wordWrap = 'break-word';
    s.overflowWrap = 'break-word';
    s.transform = 'translateX(0px)';
}

function _stepW() {
    return _vp ? _vp.offsetWidth : window.innerWidth;
}

window.addEventListener('resize', function () {
    if (!_pg) return;
    _applyLayout();
    window.goToPage(_currentPage);
});

window.getTotalPages = function () {
    if (!_pg) return 1;
    var w = _stepW();
    return w > 0 ? Math.max(1, Math.ceil(_pg.scrollWidth / w)) : 1;
};

window.goToPage = function (n) {
    if (!_pg) return { current: 0, total: 1 };
    var total = window.getTotalPages();
    _currentPage = Math.max(0, Math.min(n, total - 1));
    _pg.style.transform = 'translateX(-' + (_currentPage * _stepW()) + 'px)';
    return { current: _currentPage, total: total };
};

window.nextPage = function () {
    return window.goToPage(_currentPage + 1);
};

window.prevPage = function () {
    return window.goToPage(_currentPage - 1);
};

window.getPageInfo = function () {
    return { current: _currentPage, total: window.getTotalPages() };
};

window.goToLastPage = function () {
    return window.goToPage(window.getTotalPages() - 1);
};
