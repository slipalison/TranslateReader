using System.Globalization;
using TranslateReader.Contracts.Engines;
using TranslateReader.Models;

namespace TranslateReader.Business.Engines;

public class ThemeEngine : IThemeEngine
{
    public ThemeColors ResolveThemeColors(ThemeType theme) => theme switch
    {
        ThemeType.Light => new ThemeColors("#FFFFFF", "#1A1A1A", "#2563EB"),
        ThemeType.Dark  => new ThemeColors("#1A1A2E", "#E4E4E7", "#60A5FA"),
        ThemeType.Sepia => new ThemeColors("#F4ECD8", "#5B4636", "#8B6914"),
        _               => new ThemeColors("#FFFFFF", "#1A1A1A", "#2563EB")
    };

    public string GenerateReaderCss(ReadingSettings settings)
    {
        var c = ResolveThemeColors(settings.Theme);
        var inv = CultureInfo.InvariantCulture;
        var isPaginated = settings.ReadingMode == ReadingMode.Paginated;
        var isScroll = settings.ReadingMode == ReadingMode.Scroll;

        var css = "<style>" +
               (isPaginated ? "html{overflow:hidden;height:100vh;margin:0;padding:0;}" : "") +
               "body {" +
               $"background-color:{c.Background} !important;" +
               $"color:{c.Text} !important;" +
               $"font-family:{settings.FontFamily},serif !important;" +
               $"font-size:{settings.FontSize.ToString(inv)}px !important;" +
               $"line-height:{settings.LineSpacing.ToString(inv)} !important;" +
               $"letter-spacing:{settings.LetterSpacing.ToString(inv)}px;" +
               $"word-spacing:{settings.WordSpacing.ToString(inv)}px;" +
               "padding:16px 24px;" +
               (isPaginated ? "margin:0;" : "margin:0 auto;max-width:720px;") +
               (isPaginated ? "height:100vh;column-width:calc(100vw - 48px);column-gap:48px;overflow:visible;box-sizing:border-box;word-wrap:break-word;overflow-wrap:break-word;" : "") +
               "}" +
               "img { max-width: 100%; height: auto; display: block; margin: 1em auto; }" +
               "svg { max-width: 100%; height: auto; }" +
               $"a{{color:{c.Accent} !important;}}" +
               (isPaginated ? "img,svg,figure,table{break-inside:avoid;}img{max-height:calc(100vh - 32px) !important;object-fit:contain;}" : "") +
               (isScroll ? ".chapter-content{min-height:50vh;}" +
                           ".chapter-separator{border:none;border-top:1px solid rgba(128,128,128,0.3);margin:2em auto;width:60%;}" : "") +
               "</style>";

        if (isPaginated)
            css += GeneratePaginationScript();

        if (isScroll)
            css += GenerateScrollTrackingScript();

        return css;
    }

    private static string GenerateScrollTrackingScript() =>
        "<script>" +
        "(function(){" +
        "window.getScrollInfo=function(){" +
        "var chapters=document.querySelectorAll('.chapter-content');" +
        "var scrollY=window.scrollY||window.pageYOffset;" +
        "var viewH=window.innerHeight;" +
        "var visibleHref='';" +
        "var visibleIdx=0;" +
        "var relScroll=0;" +
        "for(var i=0;i<chapters.length;i++){" +
        "var rect=chapters[i].getBoundingClientRect();" +
        "if(rect.top<=viewH/2&&rect.bottom>viewH/2){" +
        "visibleHref=chapters[i].getAttribute('data-chapter-href');" +
        "visibleIdx=parseInt(chapters[i].getAttribute('data-chapter-index'));" +
        "var chapterTop=chapters[i].offsetTop;" +
        "var chapterH=chapters[i].offsetHeight;" +
        "relScroll=chapterH>0?(scrollY-chapterTop)/chapterH:0;" +
        "break;" +
        "}" +
        "}" +
        "if(!visibleHref&&chapters.length>0){" +
        "var last=chapters[chapters.length-1];" +
        "visibleHref=last.getAttribute('data-chapter-href');" +
        "visibleIdx=parseInt(last.getAttribute('data-chapter-index'));" +
        "relScroll=1;" +
        "}" +
        "return JSON.stringify({chapterHRef:visibleHref,chapterIndex:visibleIdx,relativeScroll:relScroll});" +
        "};" +
        "window.scrollToChapter=function(href,relPos){" +
        "var ch=document.querySelector('[data-chapter-href=\"'+href+'\"]');" +
        "if(!ch)return;" +
        "var targetY=ch.offsetTop+(ch.offsetHeight*(relPos||0));" +
        "window.scrollTo(0,targetY);" +
        "};" +
        "})();" +
        "</script>";

    private static string GeneratePaginationScript() =>
        "<script>" +
        "(function(){" +
        "var currentPage=0;" +
        "function pageWidth(){return window.innerWidth;}" +
        "window.getTotalPages=function(){return Math.max(1,Math.ceil(document.body.scrollWidth/pageWidth()));};" +
        "window.goToPage=function(n){" +
        "currentPage=Math.max(0,Math.min(n,window.getTotalPages()-1));" +
        "document.body.style.transform='translateX(-'+(currentPage*pageWidth())+'px)';" +
        "return JSON.stringify({current:currentPage,total:window.getTotalPages()});" +
        "};" +
        "window.nextPage=function(){return window.goToPage(currentPage+1);};" +
        "window.prevPage=function(){return window.goToPage(currentPage-1);};" +
        "window.getPageInfo=function(){return JSON.stringify({current:currentPage,total:window.getTotalPages()});};" +
        "window.goToLastPage=function(){return window.goToPage(window.getTotalPages()-1);};" +
        "})();" +
        "</script>";
}
