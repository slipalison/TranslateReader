window.getVisibleParagraphs = function () {
    var pg = document.getElementById('_pager');
    if (!pg) return [];
    var w = _stepW();
    var left = _currentPage * w;
    var right = left + w;
    var ps = pg.querySelectorAll('p');
    var result = [];

    for (var i = 0; i < ps.length; i++) {
        var el = ps[i];
        var t = el.hasAttribute('data-original')
            ? el.getAttribute('data-original')
            : el.textContent.trim();
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
        for (var i = 0; i < items.length; i++) {
            var idx = items[i].index;
            var tr = items[i].translated;
            if (idx >= 0 && idx < ps.length) {
                if (!ps[idx].hasAttribute('data-original'))
                    ps[idx].setAttribute('data-original', ps[idx].textContent);
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
    for (var i = 0; i < ps.length; i++) {
        ps[i].textContent = ps[i].getAttribute('data-original');
        ps[i].removeAttribute('data-original');
    }
    window.goToPage(_currentPage);
};
