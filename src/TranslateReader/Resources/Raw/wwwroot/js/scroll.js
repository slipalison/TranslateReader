window.getScrollInfo = function () {
    var chapters = document.querySelectorAll('.chapter-content');
    var scrollY = window.scrollY || window.pageYOffset;
    var viewH = window.innerHeight;
    var visibleHref = '';
    var visibleIdx = 0;
    var relScroll = 0;

    for (const chapter of chapters) {
        var rect = chapter.getBoundingClientRect();
        if (rect.top <= viewH / 2 && rect.bottom > viewH / 2) {
            visibleHref = chapter.dataset.chapterHref;
            visibleIdx = Number.parseInt(chapter.dataset.chapterIndex);
            var chapterTop = chapter.offsetTop;
            var chapterH = chapter.offsetHeight;
            relScroll = chapterH > 0 ? (scrollY - chapterTop) / chapterH : 0;
            break;
        }
    }

    if (!visibleHref && chapters.length > 0) {
        var last = chapters[chapters.length - 1];
        visibleHref = last.dataset.chapterHref;
        visibleIdx = Number.parseInt(last.dataset.chapterIndex);
        relScroll = 1;
    }

    return { chapterHRef: visibleHref, chapterIndex: visibleIdx, relativeScroll: relScroll };
};

window.scrollToChapter = function (href, relPos) {
    var ch = document.querySelector('[data-chapter-href="' + href + '"]');
    if (!ch) return;
    var targetY = ch.offsetTop + (ch.offsetHeight * (relPos || 0));
    window.scrollTo(0, targetY);
};
