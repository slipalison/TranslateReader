// Splits a paragraph's text into sentences on the same boundary the v0.2.0 mockups use: a
// sentence-ending mark (optionally followed by a closing quote/paren) then whitespace then a
// capital letter or an opening quote/paren. This is the ONLY place the regex lives — every other
// function that needs sentence boundaries calls this one.
function _splitSentences(text) {
    return String(text).split(/(?<=[.!?…]["”’»)\]]?)\s+(?=[A-ZÀ-Þ"“«'(])/).map(s => s.trim()).filter(Boolean);
}

// Turns a selection's sentence-index set into contiguous [a, b] runs, so a drag over sentences
// 2, 3, 4 becomes one range instead of three.
function _runsOf(set) {
    var sorted = Array.from(set).sort(function (a, b) { return a - b; });
    var runs = [];
    for (var i = 0; i < sorted.length; i++) {
        if (i === 0 || sorted[i] !== sorted[i - 1] + 1) {
            runs.push({ a: sorted[i], b: sorted[i] });
        } else {
            runs[runs.length - 1].b = sorted[i];
        }
    }
    return runs;
}

// FNV-1a 32-bit over UTF-16 code units. The C# side (TranslationManager.ComputeSnippetHash)
// reimplements the same offset basis and prime so restoreSnippets can compare a hash computed here
// against one persisted there without an async digest API in the WebView.
function _snipHash(text) {
    var h = 0x811c9dc5;
    for (var i = 0; i < text.length; i++) {
        h ^= text.charCodeAt(i);
        h = Math.imul(h, 0x01000193) >>> 0;
    }
    return h.toString(16).padStart(8, '0');
}

function _n(v) {
    return v.toFixed(1);
}

// Emits one rounded-rect sub-path for a band, corner radius already clamped by the caller.
function _blobBand(x1, y1, x2, y2, rr) {
    return [
        'M', _n(x1 + rr), _n(y1),
        'L', _n(x2 - rr), _n(y1),
        'Q', _n(x2), _n(y1), _n(x2), _n(y1 + rr),
        'L', _n(x2), _n(y2 - rr),
        'Q', _n(x2), _n(y2), _n(x2 - rr), _n(y2),
        'L', _n(x1 + rr), _n(y2),
        'Q', _n(x1), _n(y2), _n(x1), _n(y2 - rr),
        'L', _n(x1), _n(y1 + rr),
        'Q', _n(x1), _n(y1), _n(x1 + rr), _n(y1),
        'Z',
    ].join(' ');
}

// Pure rects-to-SVG-path geometry: no DOM reads here, so it is testable without the WebView.
// Adjacent bands meet at the midpoint between them so a selection spanning a line break reads as
// one continuous glass shape instead of two rounded rects touching edge to edge.
function _blobPath(bands, r) {
    for (var i = 0; i < bands.length - 1; i++) {
        var mid = (bands[i].y2 + bands[i + 1].y1) / 2;
        bands[i].y2 = mid;
        bands[i + 1].y1 = mid;
    }
    return bands.map(function (band) {
        var rr = Math.min(r, (band.x2 - band.x1) / 2, (band.y2 - band.y1) / 2);
        return _blobBand(band.x1, band.y1, band.x2, band.y2, rr);
    }).join(' ');
}

// Measures the selected period elements and turns their client rects into the band list
// `_blobPath` needs. Rects under 1 pixel wide or tall are layout noise (a wrapped inline element
// with no visible box), not a real line, so they are dropped before grouping.
function _blobFromEls(els) {
    var OFF = 8;
    var padX = 5;
    var padY = 1.5;
    var par = els[0].closest("[data-pi]");
    var parRect = par.getBoundingClientRect();
    var points = [];
    for (var el of els) {
        for (var r of el.getClientRects()) {
            if (r.width > 1 && r.height > 1) {
                points.push({
                    x1: r.left - parRect.left + OFF,
                    y1: r.top - parRect.top + OFF,
                    x2: r.right - parRect.left + OFF,
                    y2: r.bottom - parRect.top + OFF,
                    cy: (r.top + r.bottom) / 2 - parRect.top + OFF,
                    height: r.height,
                });
            }
        }
    }
    points.sort(function (a, b) { return a.y1 - b.y1 || a.x1 - b.x1; });
    var lines = [];
    for (var p of points) {
        var line = lines[lines.length - 1];
        if (line && Math.abs(line.cy - p.cy) < p.height * 0.6) {
            line.points.push(p);
        } else {
            lines.push({ cy: p.cy, points: [p] });
        }
    }
    var bands = lines.map(function (line) {
        return {
            x1: Math.min.apply(null, line.points.map(function (p) { return p.x1; })) - padX,
            x2: Math.max.apply(null, line.points.map(function (p) { return p.x2; })) + padX,
            y1: Math.min.apply(null, line.points.map(function (p) { return p.y1; })) - padY,
            y2: Math.max.apply(null, line.points.map(function (p) { return p.y2; })) + padY,
        };
    });
    return { d: _blobPath(bands, 10), w: Math.ceil(parRect.width) + 16, h: Math.ceil(parRect.height) + 16 };
}

// The paginated view always mounts exactly one root; scroll view may have several chapters
// mounted at once, so each of its roots carries its own anchor href instead of the null the
// paginated root reports (the current chapter fills that in on the C# side).
function _snippetRoots() {
    var roots;
    if (_currentMode === 'scroll') {
        roots = Array.from(document.querySelectorAll(".chapter-content")).map(function (el) {
            return { root: el, chapterHRef: el.dataset.chapterHref };
        });
    } else {
        roots = [{ root: document.getElementById("_pager"), chapterHRef: null }];
    }
    return roots.filter(function (item) { return item.root; });
}
