window.getScrollInfo = function () {
    var chapters = document.querySelectorAll('.chapter-content');
    var scrollY = window.scrollY || window.pageYOffset;
    var viewH = window.innerHeight;
    var visibleHref = '';
    var visibleIdx = 0;
    var relScroll = 0;

    for (var i = 0; i < chapters.length; i++) {
        var rect = chapters[i].getBoundingClientRect();
        if (rect.top <= viewH / 2 && rect.bottom > viewH / 2) {
            visibleHref = chapters[i].getAttribute('data-chapter-href');
            visibleIdx = parseInt(chapters[i].getAttribute('data-chapter-index'));
            var chapterTop = chapters[i].offsetTop;
            var chapterH = chapters[i].offsetHeight;
            relScroll = chapterH > 0 ? (scrollY - chapterTop) / chapterH : 0;
            break;
        }
    }

    if (!visibleHref && chapters.length > 0) {
        var last = chapters[chapters.length - 1];
        visibleHref = last.getAttribute('data-chapter-href');
        visibleIdx = parseInt(last.getAttribute('data-chapter-index'));
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
