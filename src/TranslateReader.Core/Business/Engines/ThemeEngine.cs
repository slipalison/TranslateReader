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
               (isPaginated ? "html{overflow:hidden;height:100vh;width:100vw;margin:0;padding:0;}" : "") +
               "body {" +
               $"background-color:{c.Background} !important;" +
               $"color:{c.Text} !important;" +
               $"font-family:{settings.FontFamily},serif !important;" +
               $"font-size:{settings.FontSize.ToString(inv)}px !important;" +
               $"line-height:{settings.LineSpacing.ToString(inv)} !important;" +
               $"letter-spacing:{settings.LetterSpacing.ToString(inv)}px;" +
               $"word-spacing:{settings.WordSpacing.ToString(inv)}px;" +
               (isPaginated ? "margin:0;padding:0;height:100vh;overflow:hidden;" : "padding:16px 24px;margin:0 auto;max-width:720px;") +
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
        "var vp,pg,currentPage=0;" +
        "function init(){" +
        "vp=document.createElement('div');vp.id='_viewport';" +
        "var vs=vp.style;vs.overflow='hidden';vs.width='100vw';vs.height='100vh';vs.position='relative';" +
        "pg=document.createElement('div');pg.id='_pager';" +
        "while(document.body.firstChild)pg.appendChild(document.body.firstChild);" +
        "vp.appendChild(pg);document.body.appendChild(vp);" +
        "applyLayout();" +
        "}" +
        "function applyLayout(){" +
        "var w=vp.offsetWidth;var pad=24;var colW=w-pad*2;" +
        "var s=pg.style;" +
        "s.height='100vh';s.padding='16px '+pad+'px';s.boxSizing='border-box';" +
        "s.columnWidth=colW+'px';s.columnGap=pad*2+'px';s.columnFill='auto';" +
        "s.wordWrap='break-word';s.overflowWrap='break-word';" +
        "s.transform='translateX(0px)';" +
        "}" +
        "function stepW(){return vp.offsetWidth;}" +
        "if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();" +
        "window.addEventListener('resize',function(){if(!pg)return;applyLayout();window.goToPage(currentPage);});" +
        "window.getTotalPages=function(){if(!pg)return 1;var w=stepW();return w>0?Math.max(1,Math.ceil(pg.scrollWidth/w)):1;};" +
        "window.goToPage=function(n){" +
        "if(!pg)return JSON.stringify({current:0,total:1});" +
        "var total=window.getTotalPages();" +
        "currentPage=Math.max(0,Math.min(n,total-1));" +
        "pg.style.transform='translateX(-'+(currentPage*stepW())+'px)';" +
        "return JSON.stringify({current:currentPage,total:total});" +
        "};" +
        "window.nextPage=function(){return window.goToPage(currentPage+1);};" +
        "window.prevPage=function(){return window.goToPage(currentPage-1);};" +
        "window.getPageInfo=function(){return JSON.stringify({current:currentPage,total:window.getTotalPages()});};" +
        "window.goToLastPage=function(){return window.goToPage(window.getTotalPages()-1);};" +
        "window.getVisibleParagraphs=function(){" +
        "var w=stepW();var left=currentPage*w;var right=left+w;" +
        "var ps=pg.querySelectorAll('p');var result=[];" +
        "for(var i=0;i<ps.length;i++){" +
        "var el=ps[i];var t=el.hasAttribute('data-original')?el.getAttribute('data-original'):el.textContent.trim();if(!t)continue;" +
        "var ol=el.offsetLeft;if(ol>=left&&ol<right){result.push({index:i,text:t});}" +
        "}" +
        "return JSON.stringify(result);" +
        "};" +
        "window.applyTranslations=function(items){" +
        "var ps=pg.querySelectorAll('p');" +
        "for(var i=0;i<items.length;i++){" +
        "var idx=items[i].index;var tr=items[i].translated;" +
        "if(idx<ps.length){" +
        "if(!ps[idx].hasAttribute('data-original'))ps[idx].setAttribute('data-original',ps[idx].textContent);" +
        "ps[idx].textContent=tr;" +
        "}}" +
        "};" +
        "window.clearTranslations=function(){" +
        "var ps=pg.querySelectorAll('p[data-original]');" +
        "for(var i=0;i<ps.length;i++){ps[i].textContent=ps[i].getAttribute('data-original');ps[i].removeAttribute('data-original');}" +
        "};" +
        "})();" +
        "</script>";
}
