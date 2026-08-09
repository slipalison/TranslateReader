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

// The app's own accent (ColorAccent, #9184d9) marks the pill/hint icons and the primary button —
// distinct from the reading theme's accent (AC), which colors the blob and the snip chip.
var _APP_ACCENT = '#9184d9';

// Filled in by window.setSnippetLabels; every value starts empty so no pt-BR string lives in this
// file before the C# side supplies one.
var _labels = {
    selectHint: '', extendTip: '', sentenceOne: '', sentenceMany: '', translateSnip: '',
    extendSel: '', shrinkSel: '', onlySentence: '', toggleSnip: '', removeSnip: '', langMap: {},
};
var _accentRgb = '';
var _darkPage = false;

var _sel = null;
var _dragging = false;
var _dragStart = null;
var _dragMoved = false;
var _mounted = false;
var _selBlob = null;
var _pillEl = null;
var _hintEl = null;
var _hintDismissed = false;

var _SNIPPET_CSS = [
    "@font-face { font-family: 'Phosphor'; src: url('fonts/Phosphor.ttf') format('truetype'); }",
    "@font-face { font-family: 'Inter'; src: url('fonts/Inter-Regular.ttf') format('truetype'); font-weight: 400; }",
    "@font-face { font-family: 'Inter'; src: url('fonts/Inter-Medium.ttf') format('truetype'); font-weight: 500; }",
    ".ph { font-family: 'Phosphor'; line-height: 1; font-style: normal; }",
    '.ph-text-align-left:before{content:"\\e484"}',
    '.ph-minus:before{content:"\\e32a"}',
    '.ph-plus:before{content:"\\e3d4"}',
    '.ph-x:before{content:"\\e4f6"}',
    '.ph-cursor-text:before{content:"\\e7d8"}',
    '.ph-arrows-left-right:before{content:"\\e0a0"}',
    '.ph-translate:before{content:"\\e4a2"}',
    '.tr-sent { position: relative; cursor: pointer; user-select: none; -webkit-user-select: none; border-radius: 8px; padding: 0.1em 0.24em; margin: 0 -0.24em; box-decoration-break: clone; -webkit-box-decoration-break: clone; }',
    'html[data-idiom="desktop"] .tr-sent { transition: background 0.22s ease; }',
    'html[data-idiom="desktop"] .tr-sent:not(.tr-on):hover { background: rgba(127,127,168,0.14); }',
    '[data-snip] { position: relative; padding: 0.1em 0.24em; margin: 0 -0.24em; box-decoration-break: clone; -webkit-box-decoration-break: clone; cursor: pointer; user-select: none; -webkit-user-select: none; }',
    '.tr-blob { position: absolute; left: -8px; top: -8px; display: block; pointer-events: none; backdrop-filter: blur(9px) saturate(180%); -webkit-backdrop-filter: blur(9px) saturate(180%); animation: trGlassIn 0.25s ease; }',
    '.tr-blob-svg { position: absolute; left: -8px; top: -8px; overflow: visible; pointer-events: none; }',
    '.tr-loading .tr-blob { animation: trPulse 1.1s ease-in-out infinite; }',
    '@keyframes trGlassIn { from { opacity: 0; transform: scale(0.985); } to { opacity: 1; transform: scale(1); } }',
    '@keyframes trPulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.45; } }',
    '@keyframes trFadeUp { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }',
    ".tr-pill { position: fixed; left: 50%; transform: translateX(-50%); z-index: 35; display: flex; align-items: center; gap: 10px; padding: 7px 8px 7px 16px; border-radius: 999px; background: rgba(28,30,48,0.58); backdrop-filter: blur(26px) saturate(190%); -webkit-backdrop-filter: blur(26px) saturate(190%); box-shadow: inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -1px 0 rgba(0,0,0,0.35), 0 16px 40px -12px rgba(0,0,0,0.75); color: #e9e9ed; font-family: 'Inter', var(--font-body); animation: trFadeUp 0.22s ease; }",
    'html[data-idiom="phone"] .tr-pill { left: 10px; right: 10px; transform: none; z-index: 30; gap: 6px; padding: 6px 6px 6px 12px; background: rgba(28,30,48,0.6); box-shadow: inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -1px 0 rgba(0,0,0,0.35), 0 16px 40px -14px rgba(0,0,0,0.8); }',
    '.tr-pill-count { font-size: 12px; white-space: nowrap; }',
    'html[data-idiom="phone"] .tr-pill-count { font-size: 11px; }',
    '.tr-pill-tip { font-size: 11px; color: rgba(233,233,237,0.55); }',
    '.tr-pill-only { font-size: 11px; color: rgba(233,233,237,0.5); }',
    '.tr-pill-spacer { flex: 1; }',
    '.tr-pill-group { display: inline-flex; gap: 2px; padding: 2px; border-radius: 999px; background: rgba(255,255,255,0.07); }',
    '.tr-pill-group button { width: 26px; height: 26px; font-size: 13px; border: none; background: transparent; color: inherit; cursor: pointer; opacity: 1; }',
    'html[data-idiom="phone"] .tr-pill-group button { width: 28px; height: 28px; }',
    '.tr-pill-group button[disabled] { opacity: 0.35; cursor: default; }',
    '.tr-pill-divider { width: 1px; height: 20px; background: rgba(255,255,255,0.16); }',
    ".tr-pill-primary { min-height: 32px; border-radius: 999px; border: none; cursor: pointer; display: inline-flex; align-items: center; gap: 6px; padding: 0 14px; background: " + _APP_ACCENT + "; color: #fff; font-family: 'Inter', var(--font-body); font-size: 14px; font-weight: 500; }",
    'html[data-idiom="phone"] .tr-pill-primary { height: 32px; font-size: 12px; }',
    '.tr-pill-close { width: 28px; height: 28px; border: none; background: transparent; color: inherit; cursor: pointer; }',
    'html[data-idiom="phone"] .tr-pill-close { width: 30px; height: 30px; }',
    ".tr-hint { position: fixed; left: 50%; transform: translateX(-50%); z-index: 34; display: flex; align-items: center; gap: 9px; padding: 8px 16px; border-radius: 999px; background: rgba(28,30,48,0.5); backdrop-filter: blur(20px) saturate(180%); -webkit-backdrop-filter: blur(20px) saturate(180%); box-shadow: inset 0 1px 0 rgba(255,255,255,0.14), 0 12px 30px -14px rgba(0,0,0,0.8); color: rgba(233,233,237,0.82); font-size: 12px; font-family: 'Inter', var(--font-body); animation: trFadeUp 0.4s ease; }",
    'html[data-idiom="phone"] .tr-hint { gap: 8px; padding: 7px 14px; background: rgba(28,30,48,0.55); box-shadow: inset 0 1px 0 rgba(255,255,255,0.14), 0 12px 30px -16px rgba(0,0,0,0.85); font-size: 11px; }',
    ".tr-snip-chip { display: inline-flex; align-items: center; gap: 5px; vertical-align: 0.08em; margin-left: 7px; padding: 2px 8px; border-radius: 999px; font-family: var(--font-body); font-size: 0.6em; font-weight: 500; letter-spacing: 0.07em; white-space: nowrap; }",
    'html[data-idiom="phone"] .tr-snip-chip { gap: 4px; margin-left: 6px; padding: 2px 7px; }',
].join('\n');

function _idiom() {
    return document.documentElement.dataset.idiom;
}

// D-2026-08-09-snippet-translation-4 derivation B: the app footer is XAML outside the WebView, so
// the pill sits a fixed offset above the WebView's own bottom edge instead of the mockup's raw
// `bottom` (which assumed the footer shared the same window).
function _pillBottom() {
    return _currentMode === 'scroll' ? 32 : (_idiom() === 'phone' ? 10 : 24);
}

function _hintBottom() {
    return _idiom() === 'phone' ? _pillBottom() + 2 : _pillBottom();
}

function _ensureStyle() {
    if (document.getElementById('_snipStyle')) return;
    var style = document.createElement('style');
    style.id = '_snipStyle';
    style.textContent = _SNIPPET_CSS;
    (document.head || document.documentElement).appendChild(style);
}

function _blobFill() {
    return _darkPage
        ? 'linear-gradient(180deg, rgba(255,255,255,0.18), rgba(255,255,255,0.07))'
        : 'linear-gradient(180deg, rgba(' + _accentRgb + ',0.17), rgba(' + _accentRgb + ',0.07))';
}

function _blobStroke() {
    return 'rgba(' + _accentRgb + ',' + (_darkPage ? '0.45' : '0.34') + ')';
}

function _blobGlow() {
    return 'rgba(' + _accentRgb + ',0.3)';
}

// SVG elements need the SVG namespace to render in a real WebView; the test harness has no
// createElementNS, so it falls back to a plain (still testable) element.
function _svgEl(tag) {
    return document.createElementNS ? document.createElementNS('http://www.w3.org/2000/svg', tag) : document.createElement(tag);
}

function _makeBlob() {
    var mask = document.createElement('span');
    mask.className = 'tr-blob';
    var svg = _svgEl('svg');
    svg.setAttribute('class', 'tr-blob-svg');
    var path = _svgEl('path');
    path.setAttribute('fill', 'none');
    path.setAttribute('stroke-width', '1.25');
    svg.appendChild(path);
    return { mask: mask, svg: svg, path: path };
}

function _updateBlob(blob, geometry) {
    blob.mask.style.width = geometry.w + 'px';
    blob.mask.style.height = geometry.h + 'px';
    blob.mask.style.clipPath = "path('" + geometry.d + "')";
    blob.mask.style.background = _blobFill();
    blob.svg.setAttribute('width', String(geometry.w));
    blob.svg.setAttribute('height', String(geometry.h));
    blob.path.setAttribute('d', geometry.d);
    blob.path.setAttribute('stroke', _blobStroke());
    blob.path.style.filter = 'drop-shadow(0 6px 16px ' + _blobGlow() + ')';
}

function _ensureSelBlob(p) {
    if (_selBlob && _selBlob.mask.parentNode === p) return;
    _removeSelBlob();
    _selBlob = _makeBlob();
    p.appendChild(_selBlob.mask);
    p.appendChild(_selBlob.svg);
}

function _removeSelBlob() {
    if (!_selBlob) return;
    _selBlob.mask.remove();
    _selBlob.svg.remove();
    _selBlob = null;
}

// Every currently-wrapped period gets its selected state reflected as a class so the desktop hover
// rule (`:not(.tr-on):hover`) can stay pure CSS instead of tracking hover in JS too. Loading
// placeholders also carry `data-si` (so a later applySnippetTranslation can find them by range)
// but are not selectable, so they are left alone here.
function _updateSentClasses() {
    for (var span of document.querySelectorAll("[data-si]")) {
        if (span.className.indexOf('tr-loading') !== -1) continue;
        var isOn = !!(_sel && span.parentNode === _sel.p && _sel.set.has(Number(span.dataset.si)));
        span.className = isOn ? 'tr-sent tr-on' : 'tr-sent';
    }
}

function _sentEls(p, set) {
    return Array.from(p.querySelectorAll("[data-si]")).filter(function (el) {
        return set.has(Number(el.dataset.si));
    });
}

function _hidePill() {
    if (!_pillEl) return;
    _pillEl.remove();
    _pillEl = null;
}

function _onExtendClick() {
    if (!_sel) return;
    var total = _sel.p.querySelectorAll("[data-si]").length;
    var maxSelected = Math.max.apply(null, Array.from(_sel.set));
    if (maxSelected < total - 1) {
        _sel.set.add(maxSelected + 1);
        _renderSelection();
    }
}

function _onShrinkClick() {
    if (!_sel || _sel.set.size <= 1) return;
    var maxSelected = Math.max.apply(null, Array.from(_sel.set));
    _sel.set.delete(maxSelected);
    _renderSelection();
}

function _buildPill() {
    var idiom = _idiom();
    var totalSentences = _sel.p.querySelectorAll("[data-si]").length;
    var count = _sel.set.size;
    var maxSelected = Math.max.apply(null, Array.from(_sel.set));
    var isPhone = idiom === 'phone';

    var pill = document.createElement('div');
    pill.className = 'tr-pill';
    pill.style.bottom = _pillBottom() + 'px';

    if (!isPhone) {
        var icon = document.createElement('i');
        icon.className = 'ph ph-text-align-left';
        icon.style.fontSize = '15px';
        icon.style.color = _APP_ACCENT;
        pill.appendChild(icon);
    }

    var counter = document.createElement('span');
    counter.className = 'tr-pill-count';
    counter.textContent = count + ' ' + (count === 1 ? _labels.sentenceOne : _labels.sentenceMany);
    pill.appendChild(counter);

    if (!isPhone) {
        if (count === 1 && totalSentences > 1) {
            var tip = document.createElement('span');
            tip.className = 'tr-pill-tip';
            tip.textContent = '· ' + _labels.extendTip;
            pill.appendChild(tip);
        } else if (totalSentences === 1) {
            var only = document.createElement('span');
            only.className = 'tr-pill-only';
            only.textContent = _labels.onlySentence;
            pill.appendChild(only);
        }
    } else {
        var spacer = document.createElement('span');
        spacer.className = 'tr-pill-spacer';
        pill.appendChild(spacer);
    }

    var group = document.createElement('span');
    group.className = 'tr-pill-group';
    var minus = document.createElement('button');
    minus.className = 'ph ph-minus';
    minus.setAttribute('aria-label', _labels.shrinkSel);
    if (count <= 1) minus.setAttribute('disabled', 'disabled');
    minus.addEventListener('click', _onShrinkClick);
    var plus = document.createElement('button');
    plus.className = 'ph ph-plus';
    plus.setAttribute('aria-label', _labels.extendSel);
    if (maxSelected >= totalSentences - 1) plus.setAttribute('disabled', 'disabled');
    plus.addEventListener('click', _onExtendClick);
    group.appendChild(minus);
    group.appendChild(plus);
    pill.appendChild(group);

    if (!isPhone) {
        var divider = document.createElement('span');
        divider.className = 'tr-pill-divider';
        pill.appendChild(divider);
    }

    var primary = document.createElement('button');
    primary.className = 'tr-pill-primary';
    var translateIcon = document.createElement('i');
    translateIcon.className = 'ph ph-translate';
    translateIcon.style.fontSize = isPhone ? '14px' : '15px';
    primary.appendChild(translateIcon);
    var translateLabel = document.createElement('span');
    translateLabel.textContent = _labels.translateSnip;
    primary.appendChild(translateLabel);
    primary.addEventListener('click', _onTranslateClick);
    pill.appendChild(primary);

    var close = document.createElement('button');
    close.className = 'tr-pill-close ph ph-x';
    close.setAttribute('aria-label', _labels.removeSnip);
    close.addEventListener('click', function () { _clearSelection(); });
    pill.appendChild(close);

    return pill;
}

function _showPill() {
    _hidePill();
    _pillEl = _buildPill();
    (document.body).appendChild(_pillEl);
}

function _buildHint() {
    var hint = document.createElement('div');
    hint.className = 'tr-hint';
    hint.style.bottom = _hintBottom() + 'px';
    var icon = document.createElement('i');
    icon.className = 'ph ph-cursor-text';
    icon.style.fontSize = _idiom() === 'phone' ? '14px' : '15px';
    icon.style.color = _APP_ACCENT;
    hint.appendChild(icon);
    var text = document.createElement('span');
    text.textContent = _labels.selectHint;
    hint.appendChild(text);
    return hint;
}

function _removeHint() {
    if (!_hintEl) return;
    _hintEl.remove();
    _hintEl = null;
}

function _renderHint() {
    if (_hintDismissed || _sel) {
        _removeHint();
        return;
    }
    if (_hintEl) return;
    _hintEl = _buildHint();
    document.body.appendChild(_hintEl);
}

function _renderSelection() {
    _updateSentClasses();
    if (!_sel) {
        _removeSelBlob();
        _hidePill();
        _renderHint();
        return;
    }
    _hintDismissed = true;
    _removeHint();
    var els = _sentEls(_sel.p, _sel.set);
    if (els.length === 0) {
        _sel = null;
        _removeSelBlob();
        _hidePill();
        _renderHint();
        return;
    }
    _ensureSelBlob(_sel.p);
    _updateBlob(_selBlob, _blobFromEls(els));
    _showPill();
}

function _clearSelection() {
    _sel = null;
    _renderSelection();
}

window.clearSnippetSelection = function () {
    _clearSelection();
};

function _toggleTap(p, si) {
    if (_sel && _sel.p === p) {
        if (_sel.set.has(si)) {
            _sel.set.delete(si);
        } else {
            _sel.set.add(si);
        }
        if (_sel.set.size === 0) _sel = null;
    } else {
        _sel = { p: p, anchor: si, set: new Set([si]) };
    }
    _renderSelection();
}

function _onSentPointerDown(e) {
    var span = e.target && e.target.closest("[data-si]");
    if (!span) return;
    _dragStart = { p: span.closest("[data-pi]"), si: Number(span.dataset.si) };
    _dragMoved = false;
    _dragging = true;
}

function _onPointerMove(e) {
    if (!_dragging || !_dragStart) return;
    var hit = document.elementFromPoint(e.clientX, e.clientY);
    var span = hit && hit.closest("[data-si]");
    if (!span) return;
    var p = span.closest("[data-pi]");
    if (p !== _dragStart.p) return;
    var si = Number(span.dataset.si);
    if (si === _dragStart.si) return;
    _dragMoved = true;
    var lo = Math.min(_dragStart.si, si);
    var hi = Math.max(_dragStart.si, si);
    var set = new Set();
    for (var i = lo; i <= hi; i++) set.add(i);
    _sel = { p: p, anchor: _dragStart.si, set: set };
    _renderSelection();
}

function _onPointerUp() {
    if (!_dragging) return;
    _dragging = false;
    if (!_dragMoved && _dragStart) {
        _toggleTap(_dragStart.p, _dragStart.si);
    }
    _dragStart = null;
    _dragMoved = false;
}

function _onDocumentClick(e) {
    var target = e.target;
    if (target && (target.closest("[data-pi]") || target.closest(".tr-pill") || target.closest(".tr-hint"))) return;
    _clearSelection();
}

function _onKeyDown(e) {
    if (e.key === 'Escape' && _idiom() !== 'phone') {
        _clearSelection();
    }
}

function _onResize() {
    if (_sel) _renderSelection();
}

// A paragraph with element children (an inline `<em>`/`<a>`/`<img>` from the book, untrusted HTML
// per csharp.md §4) keeps that markup intact as ONE period by moving its childNodes rather than
// serializing and reparsing them; a text-only paragraph splits into one span per sentence.
function _wrapParagraph(el, pi) {
    if (el.dataset.original !== undefined) return;
    if (el.dataset.pi !== undefined) return;
    var hasElementChild = Array.from(el.childNodes).some(function (node) { return node.tagName; });
    el.dataset.pi = String(pi);
    el.style.position = 'relative';
    if (!hasElementChild) {
        var sentences = _splitSentences(el.textContent);
        el.textContent = '';
        sentences.forEach(function (sentence, index) {
            if (index > 0) el.appendChild(document.createTextNode(' '));
            var span = document.createElement('span');
            span.className = 'tr-sent';
            span.dataset.si = String(index);
            span.textContent = sentence;
            span.addEventListener('pointerdown', _onSentPointerDown);
            el.appendChild(span);
        });
    } else {
        var wrap = document.createElement('span');
        wrap.className = 'tr-sent';
        wrap.dataset.si = '0';
        wrap.addEventListener('pointerdown', _onSentPointerDown);
        for (var child of Array.from(el.childNodes)) {
            wrap.appendChild(child);
        }
        el.appendChild(wrap);
    }
}

// The inverse of _wrapParagraph: a plain period span gives back its own text, a snip gives back
// the ORIGINAL text it stored (not whatever is currently displayed), and a markup period gives
// back the exact nodes that were moved in — never a re-serialized/reparsed string.
function _unwrapParagraph(el) {
    var ordered = [];
    for (var node of Array.from(el.childNodes)) {
        if (!node.tagName) {
            ordered.push(node);
        } else if (node.className && node.className.indexOf('tr-blob') !== -1) {
            continue;
        } else if (node.dataset && node.dataset.snip !== undefined) {
            ordered.push(document.createTextNode(node.dataset.orig));
        } else if (node.dataset && node.dataset.si !== undefined) {
            for (var child of Array.from(node.childNodes)) {
                ordered.push(child);
            }
        } else {
            ordered.push(node);
        }
    }
    while (el.firstChild) el.removeChild(el.firstChild);
    for (var item of ordered) el.appendChild(item);
    delete el.dataset.pi;
    el.style.position = '';
}

window.mountSnippetLayer = function () {
    _ensureStyle();
    for (var rootInfo of _snippetRoots()) {
        var candidates = _translatableCandidates(rootInfo.root);
        for (var pi = 0; pi < candidates.length; pi++) {
            _wrapParagraph(candidates[pi], pi);
        }
    }
    if (!_mounted) {
        _mounted = true;
        document.addEventListener('pointermove', _onPointerMove);
        document.addEventListener('pointerup', _onPointerUp);
        document.addEventListener('click', _onDocumentClick);
        document.addEventListener('keydown', _onKeyDown);
        window.addEventListener('resize', _onResize);
    }
    _renderHint();
};

window.unmountSnippetLayer = function () {
    _sel = null;
    _dragging = false;
    _dragStart = null;
    _removeSelBlob();
    _hidePill();
    _removeHint();
    for (var el of Array.from(document.querySelectorAll("[data-pi]"))) {
        _unwrapParagraph(el);
    }
    _mounted = false;
};

function _hexRgb(hex) {
    var clean = String(hex).replace('#', '');
    var r = Number.parseInt(clean.substring(0, 2), 16);
    var g = Number.parseInt(clean.substring(2, 4), 16);
    var b = Number.parseInt(clean.substring(4, 6), 16);
    return r + ',' + g + ',' + b;
}

function _luma(hex) {
    var clean = String(hex).replace('#', '');
    var r = Number.parseInt(clean.substring(0, 2), 16);
    var g = Number.parseInt(clean.substring(2, 4), 16);
    var b = Number.parseInt(clean.substring(4, 6), 16);
    return (0.299 * r + 0.587 * g + 0.114 * b) / 255;
}

var _sourceLanguage = '';
var _targetLanguage = '';

// Called once by the C# side after InjectChapterAsync: labels carry the pt-BR UI strings, the
// active theme's bg/accent (derivation H — Client cannot call an Engine directly, so ReaderPage
// resolves them via ISettingsManager and hands them over here), and the book's language pair, so
// the snip chip can show the right side's language without a per-snippet language column.
window.setSnippetLabels = function (labels) {
    _labels = labels;
    _accentRgb = _hexRgb(labels.theme.accent);
    _darkPage = _luma(labels.theme.bg) < 0.5;
    _sourceLanguage = labels.sourceLanguage;
    _targetLanguage = labels.targetLanguage;
};

// The anchor format the DoD depends on: chapterHRef:paragraphIndex:a:b. A paginated chapterHRef of
// null stringifies to the literal "null" here and is parsed back to null in _parseSnipKey — the
// paginated root is the only one _snippetRoots ever reports with a null href, so that round-trip
// is unambiguous.
function _snipKey(chapterHRef, paragraphIndex, a, b) {
    return chapterHRef + ':' + paragraphIndex + ':' + a + ':' + b;
}

function _parseSnipKey(key) {
    var parts = String(key).split(':');
    return {
        chapterHRef: parts[0] === 'null' ? null : parts[0],
        paragraphIndex: Number(parts[1]),
        a: Number(parts[2]),
        b: Number(parts[3]),
    };
}

function _langLabel(name) {
    if (_labels.langMap && Object.prototype.hasOwnProperty.call(_labels.langMap, name)) {
        return _labels.langMap[name];
    }
    return String(name).slice(0, 2).toUpperCase();
}

function _onSnipRemoveClick(e) {
    if (e && typeof e.stopPropagation === 'function') e.stopPropagation();
    var span = e && e.target && e.target.closest("[data-snip]");
    if (!span) return;
    var info = _parseSnipKey(span.dataset.snip);
    _restoreSnipToPeriods(span, info);
    window.sendRawMessage('snip-remove|' + JSON.stringify({
        chapterHRef: info.chapterHRef, paragraphIndex: info.paragraphIndex,
        sentenceStart: info.a, sentenceEnd: info.b,
    }));
}

function _buildChip(showingOriginal) {
    var chip = document.createElement('span');
    chip.className = 'tr-snip-chip';
    chip.style.color = 'rgb(' + _accentRgb + ')';
    chip.style.background = 'rgba(' + _accentRgb + ',0.13)';
    chip.style.boxShadow = '0 0 0 1px rgba(' + _accentRgb + ',0.38)';
    var swap = document.createElement('i');
    swap.className = 'ph ph-arrows-left-right';
    swap.style.fontSize = '1.25em';
    chip.appendChild(swap);
    var label = document.createElement('span');
    label.textContent = _langLabel(showingOriginal ? _sourceLanguage : _targetLanguage);
    chip.appendChild(label);
    var close = document.createElement('i');
    close.className = 'ph ph-x';
    close.style.fontSize = '1.15em';
    close.style.opacity = '0.65';
    close.style.cursor = 'pointer';
    close.addEventListener('click', _onSnipRemoveClick);
    chip.appendChild(close);
    return chip;
}

function _onSnipClick(e) {
    var span = e && e.target && e.target.closest("[data-snip]");
    if (!span) return;
    var info = _parseSnipKey(span.dataset.snip);
    var showingOriginal = span.dataset.showing !== '1';
    _renderSnipSpan(span, showingOriginal);
    window.sendRawMessage('snip-toggle|' + JSON.stringify({
        chapterHRef: info.chapterHRef, paragraphIndex: info.paragraphIndex,
        sentenceStart: info.a, sentenceEnd: info.b, showingOriginal: showingOriginal,
    }));
}

function _renderSnipSpan(span, showingOriginal) {
    span.dataset.showing = showingOriginal ? '1' : '0';
    span.childNodes[0].textContent = showingOriginal ? span.dataset.orig : span.dataset.trans;
    var oldChip = span.querySelector(".tr-snip-chip");
    if (oldChip) oldChip.remove();
    span.appendChild(_buildChip(showingOriginal));
}

function _buildSnipSpan(chapterHRef, pi, a, b, original, translated, showingOriginal) {
    var span = document.createElement('span');
    span.dataset.snip = _snipKey(chapterHRef, pi, a, b);
    span.dataset.orig = original;
    span.dataset.trans = translated;
    span.dataset.showing = showingOriginal ? '1' : '0';
    span.appendChild(document.createTextNode(showingOriginal ? original : translated));
    span.appendChild(_buildChip(showingOriginal));
    span.addEventListener('click', _onSnipClick);
    return span;
}

// The inverse of _buildSnipSpan: turns a snip back into individual, re-selectable periods,
// discarding the translation. si continues from the range's own start so later selections index
// consistently with the rest of the paragraph.
function _restoreSnipToPeriods(span, info) {
    var parent = span.parentNode;
    var nodes = Array.from(parent.childNodes);
    var idx = nodes.indexOf(span);
    if (idx === -1) return;
    var sentences = _splitSentences(span.dataset.orig);
    var replacement = [];
    sentences.forEach(function (sentence, offset) {
        if (offset > 0) replacement.push(document.createTextNode(' '));
        var s = document.createElement('span');
        s.className = 'tr-sent';
        s.dataset.si = String(info.a + offset);
        s.textContent = sentence;
        s.addEventListener('pointerdown', _onSentPointerDown);
        replacement.push(s);
    });
    var ordered = nodes.slice(0, idx).concat(replacement, nodes.slice(idx + 1));
    while (parent.firstChild) parent.removeChild(parent.firstChild);
    for (var item of ordered) parent.appendChild(item);
}

// A root with a null chapterHRef is the single paginated pager, which always represents whichever
// chapter is currently loaded, so it matches any requested chapterHRef; a scroll root's real href
// must match exactly.
function _findParagraph(chapterHRef, paragraphIndex) {
    for (var rootInfo of _snippetRoots()) {
        if (rootInfo.chapterHRef !== null && rootInfo.chapterHRef !== chapterHRef) continue;
        var candidates = _translatableCandidates(rootInfo.root);
        if (candidates[paragraphIndex]) return candidates[paragraphIndex];
    }
    return null;
}

function _rangeText(p, a, b) {
    var spans = Array.from(p.querySelectorAll("[data-si]")).filter(function (el) {
        var si = Number(el.dataset.si);
        return si >= a && si <= b;
    });
    return spans.map(function (el) { return el.textContent; }).join(' ');
}

// Splices out every node whose `data-si` falls in [a, b] — periods AND a loading placeholder
// alike, since both carry the attribute — and puts `replacement` in their place, keeping the
// separators before and after the range untouched.
function _spliceRange(p, a, b, replacement) {
    var nodes = Array.from(p.childNodes);
    var firstIdx = -1;
    var lastIdx = -1;
    nodes.forEach(function (node, idx) {
        if (node.dataset && node.dataset.si !== undefined) {
            var si = Number(node.dataset.si);
            if (si >= a && si <= b) {
                if (firstIdx === -1) firstIdx = idx;
                lastIdx = idx;
            }
        }
    });
    if (firstIdx === -1) return false;
    var ordered = nodes.slice(0, firstIdx).concat(replacement, nodes.slice(lastIdx + 1));
    while (p.firstChild) p.removeChild(p.firstChild);
    for (var item of ordered) p.appendChild(item);
    return true;
}

function _replaceRangeWithSnip(p, chapterHRef, pi, a, b, original, translated, showingOriginal) {
    var span = _buildSnipSpan(chapterHRef, pi, a, b, original, translated, showingOriginal);
    _spliceRange(p, a, b, [span]);
}

// D-2026-08-09-snippet-translation-5: overlap is destructive. Any existing snip in the same
// paragraph that intersects the new range is dropped back to plain periods first, so the whole
// [a, b] span is made of `[data-si]` nodes again before the new snip is spliced in.
function _removeOverlappingSnips(p, a, b) {
    for (var span of Array.from(p.querySelectorAll("[data-snip]"))) {
        var info = _parseSnipKey(span.dataset.snip);
        if (!(info.b < a || info.a > b)) {
            _restoreSnipToPeriods(span, info);
        }
    }
}

window.restoreSnippets = function (list) {
    for (var item of list) {
        var p = _findParagraph(item.chapterHRef, item.paragraphIndex);
        if (!p) continue;
        var original = _rangeText(p, item.sentenceStart, item.sentenceEnd);
        if (!original || _snipHash(original) !== item.originalHash) continue;
        _replaceRangeWithSnip(
            p, item.chapterHRef, item.paragraphIndex, item.sentenceStart, item.sentenceEnd,
            original, item.translatedText, item.showingOriginal);
    }
};

window.applySnippetTranslation = function (items) {
    for (var item of items) {
        var p = _findParagraph(item.chapterHRef, item.paragraphIndex);
        if (!p) continue;
        _removeOverlappingSnips(p, item.sentenceStart, item.sentenceEnd);
        var original = _rangeText(p, item.sentenceStart, item.sentenceEnd);
        _replaceRangeWithSnip(
            p, item.chapterHRef, item.paragraphIndex, item.sentenceStart, item.sentenceEnd,
            original, item.translatedText, item.showingOriginal);
    }
};

window.setSnippetLoading = function (keys) {
    for (var key of keys) {
        var info = _parseSnipKey(key);
        var p = _findParagraph(info.chapterHRef, info.paragraphIndex);
        if (!p) continue;
        _removeOverlappingSnips(p, info.a, info.b);
        var original = _rangeText(p, info.a, info.b);
        var span = document.createElement('span');
        span.className = 'tr-loading';
        span.dataset.si = String(info.a);
        span.style.position = 'relative';
        span.textContent = original;
        if (_spliceRange(p, info.a, info.b, [span])) {
            var blob = _makeBlob();
            span.appendChild(blob.mask);
            span.appendChild(blob.svg);
            _updateBlob(blob, _blobFromEls([span]));
        }
    }
};

function _chapterHRefFor(p) {
    for (var rootInfo of _snippetRoots()) {
        if (_translatableCandidates(rootInfo.root).includes(p)) return rootInfo.chapterHRef;
    }
    return null;
}

function _onTranslateClick() {
    if (!_sel) return;
    var chapterHRef = _chapterHRefFor(_sel.p);
    var pi = Number(_sel.p.dataset.pi);
    var paragraph = _sel.p.textContent;
    var keys = [];
    var payload = _runsOf(_sel.set).map(function (run) {
        keys.push(_snipKey(chapterHRef, pi, run.a, run.b));
        return {
            chapterHRef: chapterHRef, paragraphIndex: pi,
            sentenceStart: run.a, sentenceEnd: run.b,
            text: _rangeText(_sel.p, run.a, run.b), paragraph: paragraph,
        };
    });
    _clearSelection();
    window.setSnippetLoading(keys);
    window.sendRawMessage('snip|' + JSON.stringify(payload));
}
