// The sentence-boundary pattern lives here ONCE: a sentence-ending mark (optionally followed by a
// closing quote/paren) then whitespace then a capital letter or an opening quote/paren, exactly the
// boundary the v0.2.0 mockups use. Both _splitSentences (trimmed pieces) and _sentenceBoundaryMatches
// (raw offsets, for _wrapMarkupParagraph) read this SAME RegExp object instead of writing the
// pattern a second time.
var _SENTENCE_BOUNDARY_RE = /(?<=[.!?…]["”’»)\]]?)\s+(?=[A-ZÀ-Þ"“«'(])/;

function _splitSentences(text) {
    return String(text).split(_SENTENCE_BOUNDARY_RE).map(s => s.trim()).filter(Boolean);
}

// Sibling of _splitSentences: same pattern (via .source, never a second literal), but returns the
// [start, end) offset of every boundary's own whitespace in `text` instead of the trimmed pieces
// around it — what _wrapMarkupParagraph needs to map a period onto specific DOM nodes.
function _sentenceBoundaryMatches(text) {
    var re = new RegExp(_SENTENCE_BOUNDARY_RE.source, 'g');
    var matches = [];
    var m = re.exec(text);
    while (m !== null) {
        matches.push({ start: m.index, end: m.index + m[0].length });
        if (re.lastIndex === m.index) re.lastIndex++;
        m = re.exec(text);
    }
    return matches;
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

// The inverse-ish helper: expands an inclusive [a, b] range of sentence indices into a Set, shared
// by drag-selection and the blob sweep so both agree on what "sentences a..b" means.
function _rangeSet(a, b) {
    var set = new Set();
    for (var i = a; i <= b; i++) set.add(i);
    return set;
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

// Mirrors TranslationManager.IsSnippetTranslationTooLong: EN->PT rarely expands past ~1.6x, so a
// translation more than 3x the original excerpt's length (plus slack for short excerpts) means the
// model echoed back more than just the requested excerpt - most often the whole surrounding
// paragraph. Used on restore to auto-purge rows persisted before that guard existed, since the C#
// side only validates new translations, never rewrites an already-saved row on its own.
function _isSnippetTranslationTooLong(originalText, translatedText) {
    return translatedText.length > (originalText.length * 3) + 120;
}

function _n(v) {
    return v.toFixed(1);
}

// Pure rects-to-SVG-path geometry: no DOM reads here, so it is testable without the WebView.
// Adjacent bands meet at the midpoint between them, then a SINGLE contour is traced: across the top
// of the first band, down the right edge of every band (an S-curve where two bands' right edges
// don't line up), across the bottom of the last band, and back up the left edge the same way. That
// is one continuous glass shape with no internal seam, instead of one independent rounded rect
// stacked per line (which drew a visible edge at every line join). A caller with no bands — a range
// whose elements have not been laid out yet — gets an empty path instead of a crash, since
// `_renderAllBlobs` calls this on every DOM change, not just once per selection.
function _blobPath(bands, r) {
    if (bands.length === 0) return '';
    for (var i = 0; i < bands.length - 1; i++) {
        var mid = (bands[i].y2 + bands[i + 1].y1) / 2;
        bands[i].y2 = mid;
        bands[i + 1].y1 = mid;
    }
    var rOf = function (band) { return Math.min(r, (band.x2 - band.x1) / 2, (band.y2 - band.y1) / 2); };
    var q = function (x, y) { return _n(x) + ' ' + _n(y); };
    var n = bands.length;
    var first = bands[0];
    var last = bands[n - 1];
    var d = 'M ' + q(first.x1 + rOf(first), first.y1) +
        ' L ' + q(first.x2 - rOf(first), first.y1) +
        ' Q ' + q(first.x2, first.y1) + ' ' + q(first.x2, first.y1 + rOf(first));
    for (var right = 0; right < n; right++) {
        var band = bands[right];
        if (right === n - 1) {
            d += ' L ' + q(band.x2, band.y2 - rOf(band)) + ' Q ' + q(band.x2, band.y2) + ' ' + q(band.x2 - rOf(band), band.y2);
            continue;
        }
        var below = bands[right + 1];
        var dx = below.x2 - band.x2;
        var sign = Math.sign(dx);
        var rr = Math.min(Math.abs(dx) / 2, rOf(band), rOf(below));
        d += ' L ' + q(band.x2, band.y2 - rr) + ' Q ' + q(band.x2, band.y2) + ' ' + q(band.x2 + sign * rr, band.y2);
        d += ' L ' + q(below.x2 - sign * rr, below.y1) + ' Q ' + q(below.x2, below.y1) + ' ' + q(below.x2, below.y1 + rr);
    }
    d += ' L ' + q(last.x1 + rOf(last), last.y2) + ' Q ' + q(last.x1, last.y2) + ' ' + q(last.x1, last.y2 - rOf(last));
    for (var left = n - 1; left >= 0; left--) {
        var band2 = bands[left];
        if (left === 0) {
            d += ' L ' + q(band2.x1, band2.y1 + rOf(band2)) + ' Q ' + q(band2.x1, band2.y1) + ' ' + q(band2.x1 + rOf(band2), band2.y1);
            continue;
        }
        var above = bands[left - 1];
        var dx2 = above.x1 - band2.x1;
        var sign2 = Math.sign(dx2);
        var rr2 = Math.min(Math.abs(dx2) / 2, rOf(band2), rOf(above));
        d += ' L ' + q(band2.x1, band2.y1 + rr2) + ' Q ' + q(band2.x1, band2.y1) + ' ' + q(band2.x1 + sign2 * rr2, band2.y1);
        d += ' L ' + q(above.x1 - sign2 * rr2, above.y2) + ' Q ' + q(above.x1, above.y2) + ' ' + q(above.x1, above.y2 - rr2);
    }
    return d + ' Z';
}

// CSS multi-column pagination can fragment a single paragraph across two columns/pages: the tail of
// column N and the head of column N+1 sit far apart horizontally but can land at similar or even
// overlapping heights, which used to make the height-only line grouping below merge rows from BOTH
// columns into one band spanning the visual gap between them. Lines are read in the order they
// arrived (see _blobFromEls: never globally sorted), so a column wrap is the one deterministic
// signal available here: the next line's top is ABOVE the current line's, because a new column
// restarts layout at its own top. Each group later traces its OWN contour, so no band ever bridges
// the gap.
function _columnGroupsOf(lines) {
    var groups = [];
    var previousTop = null;
    for (var line of lines) {
        var top = Math.min.apply(null, line.points.map(function (p) { return p.y1; }));
        if (previousTop === null || top < previousTop) {
            groups.push([line]);
        } else {
            groups[groups.length - 1].push(line);
        }
        previousTop = top;
    }
    return groups;
}

// Measures the selected period elements and turns their client rects into the glass geometry a
// blob's mask/svg need, in coordinates relative to the snippet ROOT rather than the paragraph
// (D-B round 2): a CSS multi-column pager fragments a paragraph's own generated boxes across
// columns, so an element position:absolute inside the paragraph anchors to whichever fragment box
// the browser picked as "the" box, while the old geometry was measured off the bounding union of
// every fragment (whose top can belong to a LATER column) - that anchor/origin mismatch is exactly
// what produced invisible (clipped) or floating-orphan glass. The root never fragments itself (see
// mountSnippetLayer/_ensureLayerFor), so its rect is a stable coordinate origin no matter how many
// columns/pages the text spans. The returned box is sized TIGHTLY around the measured rects, not the
// whole root - in paginated mode the root spans every page of the chapter at once, so anchoring the
// mask/svg to its full size would be enormous. `left`/`top` place that tight box inside the
// root-anchored layer; `d` is in coordinates local to the box itself, same convention as before.
// Rects under 1 pixel wide or tall are layout noise (a wrapped inline element with no visible box),
// not a real line, so they are dropped before grouping. Points are grouped in the NATURAL order
// getClientRects()/els arrive in, never sorted by position: sorting by y would reorder a
// column-wrapped fragment ahead of the column it continues from, hiding the exact backward jump
// _columnGroupsOf relies on to tell two columns apart.
function _blobFromEls(els) {
    var OFF = 8;
    var padX = 5;
    var padY = 1.5;
    var root = _rootFor(els[0]);
    var rootRect = root.getBoundingClientRect();
    var points = [];
    for (var el of els) {
        for (var r of el.getClientRects()) {
            if (r.width > 1 && r.height > 1) {
                points.push({
                    x1: r.left - rootRect.left,
                    y1: r.top - rootRect.top,
                    x2: r.right - rootRect.left,
                    y2: r.bottom - rootRect.top,
                    cy: (r.top + r.bottom) / 2 - rootRect.top,
                    height: r.height,
                });
            }
        }
    }
    if (points.length === 0) return { d: '', left: 0, top: 0, w: 0, h: 0 };
    var lines = [];
    for (var p of points) {
        var line = lines[lines.length - 1];
        if (line && Math.abs(line.cy - p.cy) < p.height * 0.6) {
            line.points.push(p);
        } else {
            lines.push({ cy: p.cy, points: [p] });
        }
    }
    var bandFor = function (line) {
        return {
            x1: Math.min.apply(null, line.points.map(function (p) { return p.x1; })) - padX,
            x2: Math.max.apply(null, line.points.map(function (p) { return p.x2; })) + padX,
            y1: Math.min.apply(null, line.points.map(function (p) { return p.y1; })) - padY,
            y2: Math.max.apply(null, line.points.map(function (p) { return p.y2; })) + padY,
        };
    };
    var groups = _columnGroupsOf(lines).map(function (group) { return group.map(bandFor); });
    var allBands = [].concat.apply([], groups);
    var left = Math.min.apply(null, allBands.map(function (b) { return b.x1; })) - OFF;
    var top = Math.min.apply(null, allBands.map(function (b) { return b.y1; })) - OFF;
    var right = Math.max.apply(null, allBands.map(function (b) { return b.x2; })) + OFF;
    var bottom = Math.max.apply(null, allBands.map(function (b) { return b.y2; })) + OFF;
    var d = groups.map(function (group) {
        var bands = group.map(function (b) {
            return { x1: b.x1 - left, y1: b.y1 - top, x2: b.x2 - left, y2: b.y2 - top };
        });
        return _blobPath(bands, 10);
    }).filter(Boolean).join(' ');
    return { d: d, left: left, top: top, w: Math.ceil(right - left), h: Math.ceil(bottom - top) };
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

// Resolves which snippet root (the pager in paginated mode, or the owning .chapter-content in
// scroll mode) contains a given element, walking the CURRENT root list rather than hardcoding
// either selector string anywhere outside _snippetRoots itself (the DoD 6 gate depends on that).
function _rootFor(el) {
    for (var rootInfo of _snippetRoots()) {
        if (rootInfo.root.contains(el)) return rootInfo.root;
    }
    return null;
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
var _pillEl = null;
var _hintEl = null;
var _hintDismissed = false;

// Single registry of every live glass blob (selection runs, loading placeholders, finished snips),
// keyed by 'sel:<pi>:<runStart>' / 'load:<snipKey>' / 'snip:<snipKey>'. `_renderAllBlobs` is a
// declarative sweep: it decides from DOM state alone what should exist, creates what is missing,
// re-measures what remains, and removes what fell off the desired list.
var _blobs = new Map();

// Original DOM nodes of a range, stashed by setSnippetLoading right before it first replaces them
// with a plain-text loading placeholder, keyed by the same 'chapterHRef:pi:a:b' string a snip later
// carries in dataset.snip. Consumed (and deleted) by _spliceSpanBackToPeriods so a snip removed by
// the user, or a loading placeholder undone after a failed/superseded translation, gets its markup
// back verbatim instead of a freshly re-split, plain-text approximation. Cleared in full on unmount —
// the same bound as _blobs: nothing here outlives the chapter it was captured from.
var _snipOriginalNodes = new Map();

// One glass layer per snippet root: a direct, absolutely-positioned FIRST child of the root itself
// (never the paragraph — a paragraph fragmented across the pager's CSS columns still fragments its
// own generated boxes, so a child anchored there lands on whichever fragment the browser picked
// while the geometry was measured off a different one; see _blobFromEls). A WeakMap, not a Map: the
// paginated root is a brand new #_pager element every chapter load (paginated.js's initPagination
// tears the old one down), so a strong-keyed registry would grow one dead entry per chapter turned
// forever - the WeakMap lets a detached root's entry go with it once nothing else references it.
var _snippetLayers = new WeakMap();

// Creates the layer for `root` the first time it is needed (mount, or on demand from a sweep) and
// reuses it afterwards. `ownedPosition` remembers whether THIS call claimed `position: relative` on
// a root that had none of its own, so unmount only ever undoes what it set here, never a position
// the book's markup or another script already relied on.
function _ensureLayerFor(root) {
    var info = _snippetLayers.get(root);
    if (info) return info.layer;
    var ownedPosition = getComputedStyle(root).position === 'static';
    if (ownedPosition) root.style.position = 'relative';
    var layer = document.createElement('div');
    layer.className = 'tr-blob-layer';
    root.prepend(layer);
    _snippetLayers.set(root, { layer: layer, ownedPosition: ownedPosition });
    return layer;
}

// Removing the layer takes every blob mask/svg it holds down with it in one call; the registry
// itself only needs to forget the entry, not walk its children.
function _removeLayerFor(root) {
    var info = _snippetLayers.get(root);
    if (!info) return;
    info.layer.remove();
    if (info.ownedPosition) root.style.position = '';
    _snippetLayers.delete(root);
}

// Reused across the whole session (not re-created per mount) so mountSnippetLayer can simply
// disconnect + re-observe on every call; null on a host with no ResizeObserver support, which the
// caller must check itself.
var _resizeObserver = typeof ResizeObserver !== 'undefined' ? new ResizeObserver(_onBlobLayoutChanged) : null;
var _blobRefreshScheduled = false;

// Re-measures every live blob after a layout change this file cannot observe synchronously: an
// async web font swapping in (document.fonts.ready) or a size change on a wrapped paragraph
// (ResizeObserver) both leave stale clip-path coordinates from before the reflow otherwise. Multiple
// callbacks in the same frame collapse into one sweep via requestAnimationFrame, falling back to
// setTimeout where rAF is not available (older WebViews, this test harness).
function _scheduleBlobRefresh() {
    if (_blobRefreshScheduled) return;
    _blobRefreshScheduled = true;
    var schedule = typeof requestAnimationFrame === 'function' ? requestAnimationFrame : function (cb) { return setTimeout(cb, 0); };
    schedule(function () {
        _blobRefreshScheduled = false;
        _renderAllBlobs();
    });
}

function _onBlobLayoutChanged() {
    _scheduleBlobRefresh();
}

// WHY: ThemeEngine's body rule sets font-family with !important, so pill/hint/chip must too.
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
    '.tr-blob-layer { position: absolute; left: 0; top: 0; width: 0; height: 0; pointer-events: none; }',
    '.tr-blob { position: absolute; display: block; pointer-events: none; backdrop-filter: blur(9px) saturate(180%); -webkit-backdrop-filter: blur(9px) saturate(180%); animation: trGlassIn 0.25s ease; }',
    '.tr-blob-svg { position: absolute; overflow: visible; pointer-events: none; }',
    '.tr-blob-pulse { animation: trPulse 1.1s ease-in-out infinite; }',
    '@keyframes trGlassIn { from { opacity: 0; transform: scale(0.985); } to { opacity: 1; transform: scale(1); } }',
    '@keyframes trPulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.45; } }',
    '@keyframes trFadeUp { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }',
    ".tr-pill { position: fixed; left: 50%; transform: translateX(-50%); z-index: 35; display: flex; align-items: center; gap: 10px; padding: 7px 8px 7px 16px; border-radius: 999px; background: rgba(28,30,48,0.58); backdrop-filter: blur(26px) saturate(190%); -webkit-backdrop-filter: blur(26px) saturate(190%); box-shadow: inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -1px 0 rgba(0,0,0,0.35), 0 16px 40px -12px rgba(0,0,0,0.75); color: #e9e9ed; font-family: 'Inter', sans-serif !important; max-width: calc(100vw - 24px); animation: trFadeUp 0.22s ease; }",
    'html[data-idiom="phone"] .tr-pill { left: 10px; right: 10px; transform: none; z-index: 30; gap: 6px; padding: 6px 6px 6px 12px; background: rgba(28,30,48,0.6); box-shadow: inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -1px 0 rgba(0,0,0,0.35), 0 16px 40px -14px rgba(0,0,0,0.8); }',
    '.tr-pill-count { font-size: 12px; white-space: nowrap; }',
    'html[data-idiom="phone"] .tr-pill-count { font-size: 11px; }',
    '.tr-pill-tip { font-size: 11px; color: rgba(233,233,237,0.55); white-space: nowrap; }',
    '.tr-pill-only { font-size: 11px; color: rgba(233,233,237,0.5); white-space: nowrap; }',
    '.tr-pill-spacer { flex: 1; }',
    '.tr-pill-group { display: inline-flex; gap: 2px; padding: 2px; border-radius: 999px; background: rgba(255,255,255,0.07); }',
    '.tr-pill-group button { width: 26px; height: 26px; font-size: 13px; border: none; background: transparent; color: inherit; cursor: pointer; opacity: 1; }',
    'html[data-idiom="phone"] .tr-pill-group button { width: 28px; height: 28px; }',
    '.tr-pill-group button[disabled] { opacity: 0.35; cursor: default; }',
    '.tr-pill-divider { width: 1px; height: 20px; background: rgba(255,255,255,0.16); }',
    ".tr-pill-primary { min-height: 32px; border-radius: 999px; border: none; cursor: pointer; display: inline-flex; align-items: center; gap: 6px; padding: 0 14px; background: " + _APP_ACCENT + "; color: #fff; font-family: 'Inter', sans-serif !important; font-size: 14px; font-weight: 500; white-space: nowrap; }",
    'html[data-idiom="phone"] .tr-pill-primary { height: 32px; font-size: 12px; }',
    '.tr-pill-close { width: 28px; height: 28px; border: none; background: transparent; color: inherit; cursor: pointer; }',
    'html[data-idiom="phone"] .tr-pill-close { width: 30px; height: 30px; }',
    ".tr-hint { position: fixed; left: 50%; transform: translateX(-50%); z-index: 34; display: flex; align-items: center; gap: 9px; padding: 8px 16px; border-radius: 999px; background: rgba(28,30,48,0.5); backdrop-filter: blur(20px) saturate(180%); -webkit-backdrop-filter: blur(20px) saturate(180%); box-shadow: inset 0 1px 0 rgba(255,255,255,0.14), 0 12px 30px -14px rgba(0,0,0,0.8); color: rgba(233,233,237,0.82); font-size: 12px; font-family: 'Inter', sans-serif !important; white-space: nowrap; max-width: calc(100vw - 24px); animation: trFadeUp 0.4s ease; }",
    'html[data-idiom="phone"] .tr-hint { gap: 8px; padding: 7px 14px; background: rgba(28,30,48,0.55); box-shadow: inset 0 1px 0 rgba(255,255,255,0.14), 0 12px 30px -16px rgba(0,0,0,0.85); font-size: 11px; }',
    ".tr-snip-chip { display: inline-flex; align-items: center; gap: 5px; vertical-align: 0.08em; margin-left: 7px; padding: 2px 8px; border-radius: 999px; font-family: 'Inter', sans-serif !important; font-size: 0.6em; font-weight: 500; letter-spacing: 0.07em; white-space: nowrap; }",
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

// The pill/hint sit at `left: 50%; transform: translateX(-50%)` with no explicit width, so a real
// WebView's shrink-to-fit math constrains their content to 50vw of the ACTUAL viewport, not the
// mockup's 1280px desktop frame — a narrow window can overflow even with nowrap everywhere. 24
// mirrors the CSS `max-width: calc(100vw - 24px)` belt above.
function _availableWidth() {
    return document.documentElement.clientWidth - 24;
}

function _fits(el) {
    return el.scrollWidth <= _availableWidth();
}

// Degrades the pill in the same order the phone layout already omits by design — tip/onlySentence,
// then the primary button's label — re-measuring after each step since removing one may already be
// enough. This is the last resort once CSS nowrap alone cannot make everything fit; it never lets a
// period wrap internally.
function _fitPill(pill) {
    if (_fits(pill)) return;
    var extra = pill.querySelector(".tr-pill-tip") || pill.querySelector(".tr-pill-only");
    if (extra) extra.remove();
    if (_fits(pill)) return;
    var primary = pill.querySelector(".tr-pill-primary");
    var label = primary && primary.querySelector("span");
    if (label) {
        primary.setAttribute('title', _labels.translateSnip);
        primary.setAttribute('aria-label', _labels.translateSnip);
        label.remove();
    }
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

// SVG elements need the SVG namespace to render in a real WebView, where className is a read-only
// SVGAnimatedString (no .indexOf) instead of a plain string like every other element this file
// creates. The test harness's createElementNS mimics that exact shape (see harness.js) so this
// hazard is exercised in tests too, instead of hiding behind a fallback plain element (B-2).
function _svgEl(tag) {
    return document.createElementNS('http://www.w3.org/2000/svg', tag);
}

// className is a string on every element this file builds via createElement, but never on an
// svg/path built by _svgEl above — reading the reflected "class" attribute instead is safe on both,
// in every WebView engine, and keeps the exact substring semantics this file already relies on.
// Never call `.indexOf` on a node's `className` directly (B-2).
function _hasClass(node, cls) {
    var value = typeof node.className === 'string' ? node.className : (node.getAttribute && node.getAttribute('class'));
    return typeof value === 'string' && value.indexOf(cls) !== -1;
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
    blob.mask.style.left = geometry.left + 'px';
    blob.mask.style.top = geometry.top + 'px';
    blob.mask.style.width = geometry.w + 'px';
    blob.mask.style.height = geometry.h + 'px';
    blob.mask.style.clipPath = "path('" + geometry.d + "')";
    blob.mask.style.background = _blobFill();
    blob.svg.style.left = geometry.left + 'px';
    blob.svg.style.top = geometry.top + 'px';
    blob.svg.setAttribute('width', String(geometry.w));
    blob.svg.setAttribute('height', String(geometry.h));
    blob.path.setAttribute('d', geometry.d);
    blob.path.setAttribute('stroke', _blobStroke());
    blob.path.style.filter = 'drop-shadow(0 6px 16px ' + _blobGlow() + ')';
}

// Builds the list of blobs the current DOM/selection state wants: one per contiguous selection run
// (never one for the whole, possibly non-contiguous, set — that would draw glass across an
// unselected period), one per in-flight loading placeholder, one per finished snip. Which ROOT (and
// therefore which layer) owns each entry is resolved later, in _renderAllBlobs.
function _blobDescriptors() {
    var list = [];
    if (_sel) {
        var pi = _sel.p.dataset.pi;
        for (var run of _runsOf(_sel.set)) {
            var els = _sentEls(_sel.p, _rangeSet(run.a, run.b));
            if (els.length > 0) {
                list.push({ key: 'sel:' + pi + ':' + run.a, kind: 'sel', els: els });
            }
        }
    }
    for (var loadSpan of document.querySelectorAll(".tr-loading")) {
        list.push({ key: 'load:' + loadSpan.dataset.loadKey, kind: 'load', els: [loadSpan] });
    }
    for (var snipSpan of document.querySelectorAll("[data-snip]")) {
        list.push({ key: 'snip:' + snipSpan.dataset.snip, kind: 'snip', els: [snipSpan] });
    }
    return list;
}

// The one place that creates, measures and retires glass blobs. Every caller that changes what
// should be selected/loading/translated calls this once when it is done, instead of poking at a
// single blob reference — that is what let a snip ship with no blob at all before. A blob's mask+svg
// live in its ROOT's glass layer (_ensureLayerFor), never inside the paragraph: the layer is always
// the first child of the root, painting before every in-flow paragraph, so the glass stays under the
// text regardless of which paragraph(s) it decorates or how many pager columns they span.
function _renderAllBlobs() {
    var desired = _blobDescriptors();
    var seen = new Set();
    for (var entry of desired) {
        var root = _rootFor(entry.els[0]);
        if (!root) continue;
        var layer = _ensureLayerFor(root);
        seen.add(entry.key);
        var blob = _blobs.get(entry.key);
        if (!blob) {
            blob = _makeBlob();
            if (entry.kind === 'load') blob.mask.className += ' tr-blob-pulse';
            layer.appendChild(blob.mask);
            layer.appendChild(blob.svg);
            _blobs.set(entry.key, blob);
        }
        _updateBlob(blob, _blobFromEls(entry.els));
    }
    for (var key of Array.from(_blobs.keys())) {
        if (seen.has(key)) continue;
        var stale = _blobs.get(key);
        stale.mask.remove();
        stale.svg.remove();
        _blobs.delete(key);
    }
}

// Exposed so the C# side can force a re-measure after events this file never sees directly (a page
// navigation that refragments the pager's CSS columns) - belt and suspenders alongside the
// fonts/ResizeObserver triggers above, cheap because a sweep with nothing desired is a no-op walk.
window.refreshSnippetBlobs = _renderAllBlobs;

// Every currently-wrapped period gets its selected state reflected as a class so the desktop hover
// rule (`:not(.tr-on):hover`) can stay pure CSS instead of tracking hover in JS too. Loading
// placeholders also carry `data-si` (so a later applySnippetTranslation can find them by range)
// but are not selectable, so they are left alone here.
function _updateSentClasses() {
    for (var span of document.querySelectorAll("[data-si]")) {
        if (_hasClass(span, 'tr-loading')) continue;
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
    _fitPill(_pillEl);
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
    var hint = _buildHint();
    document.body.appendChild(hint);
    // The hint is disposable, unlike the pill (which always has an icon-only fallback): a viewport
    // too narrow for even its shortest form just goes without one instead of ever wrapping.
    if (!_fits(hint)) {
        hint.remove();
        return;
    }
    _hintEl = hint;
}

function _renderSelection() {
    _updateSentClasses();
    if (!_sel) {
        _hidePill();
        _renderHint();
        _renderAllBlobs();
        return;
    }
    _hintDismissed = true;
    _removeHint();
    if (_sentEls(_sel.p, _sel.set).length === 0) {
        _sel = null;
        _hidePill();
        _renderHint();
        _renderAllBlobs();
        return;
    }
    _showPill();
    _renderAllBlobs();
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
    _sel = { p: p, anchor: _dragStart.si, set: _rangeSet(Math.min(_dragStart.si, si), Math.max(_dragStart.si, si)) };
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

// Loading/snip blobs need re-measuring on resize just as much as a selection does, so this always
// sweeps — not only when there is an active `_sel` like the previous, selection-only implementation.
// An active selection goes through `_renderSelection` instead, which rebuilds the pill from scratch
// (`_showPill` -> `_fitPill`) rather than leaving a stale, already-degraded pill at the old width.
function _onResize() {
    if (_sel) {
        _renderSelection();
    } else {
        _renderAllBlobs();
    }
}

// A text-only paragraph splits on real sentence boundaries; a paragraph with element children (an
// inline `<em>`/`<a>`/`<img>` from the book, untrusted HTML per csharp.md §4) splits the SAME way
// but keeps every element intact — see _wrapMarkupParagraph.
function _wrapParagraph(el, pi) {
    if (el.dataset.original !== undefined) return;
    if (el.dataset.pi !== undefined) return;
    var hasElementChild = Array.from(el.childNodes).some(function (node) { return node.tagName; });
    el.dataset.pi = String(pi);
    if (hasElementChild) {
        _wrapMarkupParagraph(el);
    } else {
        _wrapPlainParagraph(el);
    }
}

function _wrapPlainParagraph(el) {
    var sentences = _splitSentences(el.textContent);
    el.textContent = '';
    sentences.forEach(function (sentence, index) {
        if (index > 0) el.appendChild(document.createTextNode(' '));
        el.appendChild(_periodSpan(sentence, index));
    });
}

function _periodSpan(text, index) {
    var span = _emptyPeriodSpan(index);
    span.textContent = text;
    return span;
}

function _emptyPeriodSpan(index) {
    var span = document.createElement('span');
    span.className = 'tr-sent';
    span.dataset.si = String(index);
    span.addEventListener('pointerdown', _onSentPointerDown);
    return span;
}

// Splits a paragraph that has element children on the SAME sentence boundaries _splitSentences
// finds (via _sentenceBoundaryMatches, the shared regex — never a second literal), located as
// offsets in the flattened text rather than re-parsed from the trimmed strings _splitSentences
// returns. An inline element is atomic: any boundary whose matched whitespace sits inside one is
// dropped before the walk even starts, so the period that would have ended there simply keeps going
// until the next boundary that actually lands in free text — the element itself is never cut, and
// no node is ever serialized/reparsed (csharp.md §4: book HTML is untrusted input).
function _wrapMarkupParagraph(el) {
    var elementRanges = _topLevelElementRanges(el);
    var matches = _sentenceBoundaryMatches(el.textContent).filter(function (m) {
        return !elementRanges.some(function (r) { return m.start >= r.start && m.start < r.end; });
    });
    var state = { ordered: [], si: 0, span: _emptyPeriodSpan(0), matchIdx: 0, pos: 0 };
    for (var node of Array.from(el.childNodes)) {
        if (node.tagName) {
            state.span.appendChild(node);
            state.pos += node.textContent.length;
        } else {
            _consumeTextNode(node, state, matches);
        }
    }
    state.ordered.push(state.span);
    while (el.firstChild) el.removeChild(el.firstChild);
    for (var item of state.ordered) el.appendChild(item);
}

// Flattened-text [start, end) range of every node that is a DIRECT child of `el` and an element —
// the only nodes _wrapMarkupParagraph ever moves whole, so the only ranges a boundary must avoid.
function _topLevelElementRanges(el) {
    var ranges = [];
    var pos = 0;
    for (var node of Array.from(el.childNodes)) {
        var len = node.textContent.length;
        if (node.tagName) ranges.push({ start: pos, end: pos + len });
        pos += len;
    }
    return ranges;
}

// Walks one top-level text node against every remaining boundary that starts before it ends,
// cutting it with the native Text.splitText at each one: the piece before a match closes the
// CURRENT period span, the matched whitespace itself is left in `state.ordered` as its own loose
// node (never inside a span — _unwrapParagraph already restores a plain child untouched), and a
// fresh span opens for the period that follows. Whatever is left after the last boundary stays in
// the still-open span, since a later sibling node may continue the same period.
function _consumeTextNode(node, state, matches) {
    var nodeStart = state.pos;
    var nodeEnd = nodeStart + node.data.length;
    var remaining = node;
    var remainingStart = nodeStart;
    while (state.matchIdx < matches.length && matches[state.matchIdx].start < nodeEnd) {
        var m = matches[state.matchIdx];
        if (m.start > remainingStart) {
            var beforeSep = remaining.splitText(m.start - remainingStart);
            state.span.appendChild(remaining);
            remaining = beforeSep;
        }
        state.ordered.push(state.span);
        state.si++;
        state.span = _emptyPeriodSpan(state.si);
        var afterSep = remaining.splitText(m.end - m.start);
        state.ordered.push(remaining);
        remaining = afterSep;
        remainingStart = m.end;
        state.matchIdx++;
    }
    state.span.appendChild(remaining);
    state.pos = nodeEnd;
}

// The inverse of _wrapParagraph: a plain period span gives back its own text, a snip gives back
// the ORIGINAL text it stored (not whatever is currently displayed), and a markup period gives
// back the exact nodes that were moved in — never a re-serialized/reparsed string.
function _unwrapParagraph(el) {
    var ordered = [];
    for (var node of Array.from(el.childNodes)) {
        if (!node.tagName) {
            ordered.push(node);
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
}

window.mountSnippetLayer = function () {
    _ensureStyle();
    // Disconnected and rebuilt on every mount (not just the first): the wrapped paragraphs are new
    // DOM nodes every time a chapter loads, so yesterday's observations would otherwise pile up on
    // detached elements forever.
    if (_resizeObserver) _resizeObserver.disconnect();
    for (var rootInfo of _snippetRoots()) {
        _ensureLayerFor(rootInfo.root);
        var candidates = _translatableCandidates(rootInfo.root);
        for (var pi = 0; pi < candidates.length; pi++) {
            _wrapParagraph(candidates[pi], pi);
            if (_resizeObserver) _resizeObserver.observe(candidates[pi]);
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
    // The book font (and Inter, for the pill/hint/chip) loads asynchronously; every blob measured
    // before it lands has stale coordinates once the swap reflows the text underneath it.
    if (document.fonts) {
        document.fonts.ready.then(_renderAllBlobs);
    }
    _renderHint();
    _renderAllBlobs();
};

window.unmountSnippetLayer = function () {
    _sel = null;
    _dragging = false;
    _dragStart = null;
    _hidePill();
    _removeHint();
    if (_resizeObserver) _resizeObserver.disconnect();
    // Removing each root's layer takes every blob mask/svg it holds down with it in one call - a
    // blob is never a child of the paragraph it decorates (see _renderAllBlobs), so unwrapping a
    // paragraph below never has to recognize or skip one (B-2 is structurally unreachable now).
    for (var rootInfo of _snippetRoots()) {
        _removeLayerFor(rootInfo.root);
    }
    _blobs.clear();
    _snipOriginalNodes.clear();
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
    _renderAllBlobs();
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
    _renderAllBlobs();
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

// Shared by _restoreSnipToPeriods (undo a translated snip) and clearSnippetLoading (undo a loading
// placeholder that failed or was superseded): splices the range back into individual periods at
// span's own position. When the exact original nodes are still stashed under `key` (captured by
// setSnippetLoading before it first replaced them — see _snipOriginalNodes), those nodes are
// spliced back verbatim so inline markup like <em> survives; otherwise (a snip restored straight
// from a persisted session, which only ever carries plain text server-side) the text is re-split
// into fresh plain-text spans, losing any markup until the chapter is re-injected.
function _spliceSpanBackToPeriods(span, originalText, startIndex, key) {
    var parent = span.parentNode;
    var nodes = Array.from(parent.childNodes);
    var idx = nodes.indexOf(span);
    if (idx === -1) return;
    var stashed = key ? _snipOriginalNodes.get(key) : null;
    var replacement = stashed || _plainPeriodSpans(originalText, startIndex);
    if (key) _snipOriginalNodes.delete(key);
    var ordered = nodes.slice(0, idx).concat(replacement, nodes.slice(idx + 1));
    while (parent.firstChild) parent.removeChild(parent.firstChild);
    for (var item of ordered) parent.appendChild(item);
}

// Fallback for _spliceSpanBackToPeriods when no original nodes were stashed: re-splits plain text
// into fresh period spans, indexed from startIndex so later selections stay consistent with the
// rest of the paragraph.
function _plainPeriodSpans(originalText, startIndex) {
    var sentences = _splitSentences(originalText);
    var replacement = [];
    sentences.forEach(function (sentence, offset) {
        if (offset > 0) replacement.push(document.createTextNode(' '));
        replacement.push(_periodSpan(sentence, startIndex + offset));
    });
    return replacement;
}

// The inverse of _buildSnipSpan: turns a snip back into individual, re-selectable periods,
// discarding the translation.
function _restoreSnipToPeriods(span, info) {
    _spliceSpanBackToPeriods(span, span.dataset.orig, info.a, span.dataset.snip);
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

// Finds the [firstIdx, lastIdx] span of p.childNodes covered by data-si in [a, b] — shared by
// _spliceRange (replace) and _captureRangeNodes (stash before replacing), so the two never disagree
// on what counts as "the range". Periods AND a loading placeholder both qualify, since both carry
// the attribute.
function _rangeNodeIndices(p, a, b) {
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
    return firstIdx === -1 ? null : { nodes: nodes, firstIdx: firstIdx, lastIdx: lastIdx };
}

// Puts `replacement` where the range used to be, keeping the separators before and after it
// untouched. Returns false (no-op) when the range does not exist in `p` at all.
function _spliceRange(p, a, b, replacement) {
    var range = _rangeNodeIndices(p, a, b);
    if (!range) return false;
    var ordered = range.nodes.slice(0, range.firstIdx).concat(replacement, range.nodes.slice(range.lastIdx + 1));
    while (p.firstChild) p.removeChild(p.firstChild);
    for (var item of ordered) p.appendChild(item);
    return true;
}

// Reads (without removing) the live nodes a range currently occupies, so setSnippetLoading can
// stash them in _snipOriginalNodes before _spliceRange discards them.
function _captureRangeNodes(p, a, b) {
    var range = _rangeNodeIndices(p, a, b);
    return range ? range.nodes.slice(range.firstIdx, range.lastIdx + 1) : null;
}

function _replaceRangeWithSnip(p, chapterHRef, pi, a, b, original, translated, showingOriginal) {
    var span = _buildSnipSpan(chapterHRef, pi, a, b, original, translated, showingOriginal);
    return _spliceRange(p, a, b, [span]);
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
        if (_isSnippetTranslationTooLong(original, item.translatedText)) {
            // A row poisoned before the length guard existed (or by a stale cache entry from an
            // earlier session): skip applying it AND ask the C# side to delete the dead row, so the
            // very first time the book reopens after this fix quietly cleans up past damage instead
            // of re-rendering it forever.
            window.sendRawMessage('snip-remove|' + JSON.stringify({
                chapterHRef: item.chapterHRef, paragraphIndex: item.paragraphIndex,
                sentenceStart: item.sentenceStart, sentenceEnd: item.sentenceEnd,
            }));
            continue;
        }
        _replaceRangeWithSnip(
            p, item.chapterHRef, item.paragraphIndex, item.sentenceStart, item.sentenceEnd,
            original, item.translatedText, item.showingOriginal);
    }
    _renderAllBlobs();
};

// Applies one item: finds its paragraph, drops any overlapping snip, and splices the finished snip
// in over the range setSnippetLoading placeholder-ed earlier. Returns whether it actually landed —
// false either means the paragraph itself could not be resolved, or the specific range inside it no
// longer exists (both count as "did not apply" for the orphan cleanup in applySnippetTranslation).
function _applySnippetItem(item) {
    var p = _findParagraph(item.chapterHRef, item.paragraphIndex);
    if (!p) return false;
    _removeOverlappingSnips(p, item.sentenceStart, item.sentenceEnd);
    var original = _rangeText(p, item.sentenceStart, item.sentenceEnd);
    return _replaceRangeWithSnip(
        p, item.chapterHRef, item.paragraphIndex, item.sentenceStart, item.sentenceEnd,
        original, item.translatedText, item.showingOriginal);
}

// The SAME tolerant chapterHRef semantics _findParagraph uses for the paginated root — null on
// EITHER side matches anything, two non-null values must be equal — applied symmetrically here
// because the side carrying null can be either one: setSnippetLoading always keys a paginated-mode
// placeholder with null, but the item that comes back off an in-flight translation can carry the
// concrete chapterHRef of whatever chapter was current when the result landed. paragraphIndex and
// sentenceStart (`a`) must still agree exactly — `a` is what actually distinguishes two different
// runs translating inside the SAME paragraph at once (a loading placeholder's own data-si IS its
// `a`, see _loadingSpanAt), so a coincidental pi match alone must never clear the wrong one.
function _anchorMatches(itemAnchor, spanAnchor) {
    if (itemAnchor.paragraphIndex !== spanAnchor.paragraphIndex) return false;
    if (itemAnchor.a !== spanAnchor.a) return false;
    return itemAnchor.chapterHRef === null || spanAnchor.chapterHRef === null ||
        itemAnchor.chapterHRef === spanAnchor.chapterHRef;
}

// Undoes a loading placeholder an apply item could not land on (its own paragraph/range vanished
// between setSnippetLoading and the response arriving — e.g. a navigation tore down the root that
// owned the request). Searches the WHOLE document, not just whatever _findParagraph could resolve,
// since that is exactly the lookup that just failed: a translation result that IS present but
// inapplicable must never leave its own `.tr-loading` pulsing forever (D-B). Matches by parsed
// anchor (_anchorMatches), never by exact key string — the item's own chapterHRef and the
// placeholder's stored one can legitimately disagree (see _anchorMatches).
function _clearOrphanedLoading(item) {
    var itemAnchor = {
        chapterHRef: item.chapterHRef, paragraphIndex: item.paragraphIndex, a: item.sentenceStart,
    };
    for (var span of document.querySelectorAll(".tr-loading")) {
        var spanAnchor = _parseSnipKey(span.dataset.loadKey);
        if (_anchorMatches(itemAnchor, spanAnchor)) {
            _spliceSpanBackToPeriods(span, span.textContent, spanAnchor.a, span.dataset.loadKey);
            return;
        }
    }
}

window.applySnippetTranslation = function (items) {
    for (var item of items) {
        if (!_applySnippetItem(item)) _clearOrphanedLoading(item);
    }
    _renderAllBlobs();
};

window.setSnippetLoading = function (keys) {
    for (var key of keys) {
        var info = _parseSnipKey(key);
        var p = _findParagraph(info.chapterHRef, info.paragraphIndex);
        if (!p) continue;
        _removeOverlappingSnips(p, info.a, info.b);
        var captured = _captureRangeNodes(p, info.a, info.b);
        if (captured) _snipOriginalNodes.set(key, captured);
        var original = _rangeText(p, info.a, info.b);
        var span = document.createElement('span');
        span.className = 'tr-loading';
        span.dataset.si = String(info.a);
        span.dataset.loadKey = key;
        span.textContent = original;
        _spliceRange(p, info.a, info.b, [span]);
    }
    _renderAllBlobs();
};

function _loadingSpanAt(p, a) {
    for (var span of p.querySelectorAll("[data-si]")) {
        if (_hasClass(span, 'tr-loading') && Number(span.dataset.si) === a) {
            return span;
        }
    }
    return null;
}

// Undoes setSnippetLoading for a run that failed or was superseded by a newer selection. The
// placeholder's own text IS its textContent now (the blob lives in the root's layer, never nested
// inside it), so splicing it back into periods needs no server round trip and this is only ever
// called for a range whose translation never arrived.
window.clearSnippetLoading = function (keys) {
    for (var key of keys) {
        var info = _parseSnipKey(key);
        var p = _findParagraph(info.chapterHRef, info.paragraphIndex);
        if (!p) continue;
        var span = _loadingSpanAt(p, info.a);
        if (!span) continue;
        _spliceSpanBackToPeriods(span, span.textContent, info.a, key);
    }
    _renderAllBlobs();
};

function _chapterHRefFor(p) {
    for (var rootInfo of _snippetRoots()) {
        if (_translatableCandidates(rootInfo.root).includes(p)) return rootInfo.chapterHRef;
    }
    return null;
}

// Reconstructs the paragraph's ORIGINAL text straight from the DOM, in document order, regardless
// of what is currently on screen: a snip contributes the original text it stored (dataset.orig),
// never its current display text or its language chip's label — both live inside that same span; a
// period/loading span contributes its own sentence text; a plain text node (the space between spans)
// is used as-is. A glass blob is never a child of the paragraph (it lives in its root's layer, see
// _renderAllBlobs), so there is no decoration node to skip here anymore. This is the only safe
// source for the "paragraph" context field sent to the translator: `p.textContent` leaks whichever
// side of the toggle every existing snip in the paragraph happens to be showing, plus its chip's
// "EN"/"PT-BR" label, straight into the prompt — which is what made a small model translate the
// entire paragraph (labels and all) instead of just the newly selected excerpt.
function _originalParagraphText(p) {
    var parts = [];
    for (var node of Array.from(p.childNodes)) {
        if (!node.tagName) {
            parts.push(node.textContent);
        } else if (node.dataset && node.dataset.snip !== undefined) {
            parts.push(node.dataset.orig);
        } else if (node.dataset && node.dataset.si !== undefined) {
            // The FULL flattened text, not just the first child: a plain period/loading placeholder
            // has one text child either way, but a period carrying inline markup (_wrapMarkupParagraph)
            // can hold several nodes, and childNodes[0] alone silently truncated it at the first one.
            parts.push(node.textContent);
        }
    }
    return parts.join('');
}

// The C# side drives the loading placeholder (ReaderPage calls setSnippetLoading as soon as the
// message arrives, before starting inference) rather than this handler doing it eagerly, so there
// is exactly one place that decides a run is "in flight".
function _onTranslateClick() {
    if (!_sel) return;
    var chapterHRef = _chapterHRefFor(_sel.p);
    var pi = Number(_sel.p.dataset.pi);
    var paragraph = _originalParagraphText(_sel.p);
    var payload = _runsOf(_sel.set).map(function (run) {
        return {
            chapterHRef: chapterHRef, paragraphIndex: pi,
            sentenceStart: run.a, sentenceEnd: run.b,
            text: _rangeText(_sel.p, run.a, run.b), paragraph: paragraph,
        };
    });
    _clearSelection();
    window.sendRawMessage('snip|' + JSON.stringify(payload));
}
