using Markdig;
using System.Text.RegularExpressions;

namespace MDReader.Services;

public class MarkdownService
{
    public MarkdownResult ConvertToHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(markdown, pipeline);

        var toc = new List<TocEntry>();
        var headingRegex = new Regex(@"<h([1-6])\s*id=""([^""]+)""[^>]*>(.*?)</h\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var matches = headingRegex.Matches(html);

        foreach (Match match in matches)
        {
            int level = int.Parse(match.Groups[1].Value);
            string id = match.Groups[2].Value;
            string headingText = Regex.Replace(match.Groups[3].Value, @"<[^>]+>", "");
            toc.Add(new TocEntry { Id = id, Text = headingText, Level = level });
        }

        string fullHtml = GenerateHtmlTemplate(html);

        return new MarkdownResult
        {
            Html = fullHtml,
            TocEntries = toc
        };
    }

    private static string GenerateHtmlTemplate(string bodyHtml)
    {
        return $@"<!DOCTYPE html><html><head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1.0"">
<style>
* {{ margin: 0; padding: 0; box-sizing: border-box; }}
@media print {{ @page {{ margin: 10mm; }} body {{ padding: 0 !important; background: #fff !important; }} pre {{ overflow: visible !important; }} }}
body {{ font-family: -apple-system, Segoe UI, sans-serif; line-height: 1.7; padding: 40px 48px; color: #1a1a1a; background: #fff; }}
h1 {{ font-size: 2em; border-bottom: 2px solid #e0e0e0; padding-bottom: 12px; margin: 32px 0 20px; }}
h2 {{ font-size: 1.5em; margin: 28px 0 16px; color: #2c3e50; }}
h3 {{ font-size: 1.2em; margin: 24px 0 12px; color: #34495e; }}
h4,h5,h6 {{ margin: 20px 0 10px; color: #555; }}
p {{ margin: 0 0 16px; }}
code {{ background: #f5f5f5; padding: 2px 6px; border-radius: 4px; font-size: 0.9em; font-family: Consolas, monospace; }}
pre {{ background: #f5f5f5; padding: 16px; border-radius: 6px; overflow-x: auto; margin: 16px 0; }}
pre code {{ padding: 0; background: none; }}
blockquote {{ border-left: 4px solid #3498db; padding: 8px 20px; margin: 16px 0; background: #f8f9fa; }}
table {{ border-collapse: collapse; width: 100%; margin: 16px 0; }}
th, td {{ border: 1px solid #ddd; padding: 8px 14px; text-align: left; }}
th {{ background: #f0f0f0; }}
img {{ max-width: 100%; }}
ul, ol {{ margin: 8px 0 16px 24px; }}
li {{ margin: 4px 0; }}
hr {{ border: none; border-top: 1px solid #ddd; margin: 24px 0; }}
a {{ color: #3498db; text-decoration: none; }}
a:hover {{ text-decoration: underline; }}
mark {{ background: #fff176; padding: 0 2px; border-radius: 2px; }}
mark.active {{ background: #ff9100; outline: 2px solid #e65100; }}
.dark-mode {{ background: #1e1e1e !important; color: #d4d4d4 !important; }}
.dark-mode h1 {{ border-bottom-color: #333; color: #e0e0e0; }}
.dark-mode h2 {{ color: #c8c8c8; }}
.dark-mode h3 {{ color: #b0b0b0; }}
.dark-mode h4,h5,h6 {{ color: #999; }}
.dark-mode code {{ background: #2d2d2d; }}
.dark-mode pre {{ background: #2d2d2d; }}
.dark-mode blockquote {{ background: #2a2a2a; border-left-color: #569cd6; }}
.dark-mode th {{ background: #333; }}
.dark-mode th,.dark-mode td {{ border-color: #444; }}
.dark-mode a {{ color: #569cd6; }}
.dark-mode mark {{ background: #6b5900; }}
.dark-mode mark.active {{ background: #b36400; outline-color: #ff9100; }}
.dark-mode hr {{ border-top-color: #333; }}
@media print {{ .dark-mode {{ background: #fff !important; color: #1a1a1a !important; }} .dark-mode h1,.dark-mode h2,.dark-mode h3,.dark-mode h4,.dark-mode h5,.dark-mode h6 {{ color: #1a1a1a !important; border-bottom-color: #e0e0e0 !important; }} .dark-mode code,.dark-mode pre {{ background: #f5f5f5 !important; }} .dark-mode blockquote {{ background: #f8f9fa !important; border-left-color: #3498db !important; }} .dark-mode th {{ background: #f0f0f0 !important; }} .dark-mode th,.dark-mode td {{ border-color: #ddd !important; }} .dark-mode a {{ color: #3498db !important; }} .dark-mode hr {{ border-top-color: #ddd !important; }} }}
</style></head><body>
{bodyHtml}
<script>
var _matches=[],_currentIdx=-1;
function findText(text){{
    clearHighlights();
    if(!text) return 0;
    var re=new RegExp(text.replace(/[.*+?^$!<>(){{}}|\[\]\\]/g,'\\$&'),'gi');
    var nodes=[];
    function collect(n){{
        if(n.nodeType===3){{ if(n.parentNode.tagName!=='MARK'&&n.parentNode.tagName!=='SCRIPT'&&n.parentNode.tagName!=='STYLE') nodes.push(n); }}
        else if(n.nodeType===1&&n.tagName!=='SCRIPT'&&n.tagName!=='STYLE') for(var i=0;i<n.childNodes.length;i++) collect(n.childNodes[i]);
    }}
    collect(document.body);
    var count=0;
    for(var i=0;i<nodes.length;i++){{
        var node=nodes[i],txt=node.textContent,last=0,parts=[];
        re.lastIndex=0;
        var m;
        while((m=re.exec(txt))!==null){{
            if(m.index>last) parts.push(document.createTextNode(txt.slice(last,m.index)));
            var mark=document.createElement('mark');
            mark.textContent=m[0];
            mark.dataset.idx=count;
            parts.push(mark);
            _matches.push(mark);
            count++;
            last=re.lastIndex;
        }}
        if(parts.length>0){{
            if(last<txt.length) parts.push(document.createTextNode(txt.slice(last)));
            var frag=document.createDocumentFragment();
            for(var p=0;p<parts.length;p++) frag.appendChild(parts[p]);
            node.parentNode.replaceChild(frag,node);
        }}
    }}
    return count;
}}
function focusMatch(idx){{
    if(idx<0||idx>=_matches.length) return;
    _matches.forEach(function(m){{m.classList.remove('active')}});
    var el=_matches[idx]; el.classList.add('active');
    el.scrollIntoView({{behavior:'smooth',block:'center'}});
    _currentIdx=idx;
}}
function clearHighlights(){{
    document.querySelectorAll('mark').forEach(function(e){{
        var s=document.createElement('span');
        s.innerHTML=e.innerHTML;
        e.parentNode.replaceChild(s,e);
    }});
    _matches=[]; _currentIdx=-1;
}}
function scrollToTop(){{
    window.scrollTo({{top:0,behavior:'smooth'}});
}}
function scrollToId(id){{
    var el=document.getElementById(id);
    if(el) el.scrollIntoView({{behavior:'smooth',block:'start'}});
}}
function toggleDarkMode(enabled){{
    document.body.classList.toggle('dark-mode',enabled);
}}
</script></body></html>";
    }
}

public class MarkdownResult
{
    public string Html { get; set; } = "";
    public List<TocEntry> TocEntries { get; set; } = new();
}

public class TocEntry
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public int Level { get; set; }
}
