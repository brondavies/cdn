using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace example.TagHelpers
{
    [HtmlTargetElement("css")]
    public class FingerprintCssTagHelper : TagHelper
    {
        public string href { get; set; }
        
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "link";
            output.Attributes.SetAttribute("rel", "stylesheet");
            output.Attributes.SetAttribute("href", CDN.Url(href));
        }
    }

    [HtmlTargetElement("js")]
    public class FingerprintJsTagHelper : TagHelper
    {
        public string src { get; set; }
        
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "script";
            output.Attributes.SetAttribute("type", "text/javascript");
            output.Attributes.SetAttribute("src", CDN.Url(src));
        }
    }

    internal class CDN
    {
        public static Dictionary<string, string> Fingerprints { get; set; }

        internal static string Url(string path)
        {
            string url = null;
            if (Fingerprints == null)
            {
                var file = Path.Combine("wwwroot", "content", "fingerprints.json");
                if (File.Exists(file))
                {
                    Fingerprints = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                }
                else
                {
                    Fingerprints = new Dictionary<string, string>();
                }
            }
            if (Fingerprints.ContainsKey(path))
            {
                url = Fingerprints[path];
            }
            if (string.IsNullOrEmpty(url))
            {
                url = path;
            }
            return url;
        }
    }
}
