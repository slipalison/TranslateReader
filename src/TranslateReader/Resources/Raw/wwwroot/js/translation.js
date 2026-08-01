var _CANDIDATE_SELECTOR = 'p, h1, h2, h3, h4, h5, h6, li, div';
var _BLOCK_CHILD_SELECTOR = 'div, p, h1, h2, h3, h4, h5, h6, li';
var _LETTER_RE = /\p{L}/u;

// The one place that decides what counts as a paragraph. Reading, writing and clearing all walk
// this list, so an index reported by getVisibleParagraphs always names the same element later on.
// A div joins only when it is a leaf and carries a letter: calibre exports hold their prose in
// `div class="calibreN"` and use that very same shape for images, bullets and the wrapper that
// holds the whole chapter.
function _translatableCandidates(pg) {
    var candidates = [];
    for (const el of pg.querySelectorAll(_CANDIDATE_SELECTOR)) {
        if (el.tagName === 'DIV') {
            if (el.querySelector(_BLOCK_CHILD_SELECTOR)) continue;
            if (!_LETTER_RE.test(el.dataset.original ?? el.textContent)) continue;
        } else if (!_paragraphText(el)) {
            continue;
        }
        candidates.push(el);
    }
    return candidates;
}

function _paragraphText(el) {
    return el.dataset.original ?? el.textContent.trim();
}

window.getVisibleParagraphs = function () {
    var pg = document.getElementById('_pager');
    if (!pg) return [];
    var w = _stepW();
    var left = _currentPage * w;
    var right = left + w;
    var ps = _translatableCandidates(pg);
    if (ps.length === 0) {
        if (pg.textContent.trim()) {
            console.warn('[JS] no translatable paragraph found in this chapter');
        }
        return [];
    }
    var result = [];

    for (const [i, el] of ps.entries()) {
        var ol = el.offsetLeft;
        if (ol >= left && ol < right) {
            result.push({ index: i, text: _paragraphText(el) });
        }
    }
    return result;
};

window.applyTranslations = function (items) {
    try {
        if (!items) return;
        var pg = document.getElementById('_pager');
        if (!pg) return;
        var ps = _translatableCandidates(pg);
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
    for (const el of _translatableCandidates(pg)) {
        if (el.dataset.original === undefined) continue;
        el.textContent = el.dataset.original;
        delete el.dataset.original;
    }
    window.goToPage(_currentPage);
};
