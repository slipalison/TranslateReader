window.getVisibleParagraphs = function () {
    var pg = document.getElementById('_pager');
    if (!pg) return [];
    var w = _stepW();
    var left = _currentPage * w;
    var right = left + w;
    var ps = pg.querySelectorAll('p');
    var result = [];

    for (const [i, el] of ps.entries()) {
        var t = el.dataset.original ?? el.textContent.trim();
        if (!t) continue;
        var ol = el.offsetLeft;
        if (ol >= left && ol < right) {
            result.push({ index: i, text: t });
        }
    }
    return result;
};

window.applyTranslations = function (items) {
    try {
        if (!items) return;
        var pg = document.getElementById('_pager');
        if (!pg) return;
        var ps = pg.querySelectorAll('p');
        for (const item of items) {
            var idx = item.index;
            var tr = item.translated;
            if (idx >= 0 && idx < ps.length) {
                if (ps[idx].dataset.original === undefined)
                    ps[idx].dataset.original = ps[idx].textContent;
                ps[idx].textContent = tr;
            }
        }
        window.goToPage(_currentPage);
    } catch (e) {
        console.error('applyTranslations error:', e);
    }
};

window.clearTranslations = function () {
    var pg = document.getElementById('_pager');
    if (!pg) return;
    var ps = pg.querySelectorAll('p[data-original]');
    for (const p of ps) {
        p.textContent = p.dataset.original;
        delete p.dataset.original;
    }
    window.goToPage(_currentPage);
};
