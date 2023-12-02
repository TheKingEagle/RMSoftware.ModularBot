using ModularBOT.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Entity
{
    internal class WebPortalPage
    {
        internal string Title { get; set; }
        internal string Content { get; set; }
        internal string LogoUrl { get; set; }

        internal List<string> ScriptSources { get; set; }
        internal List<string> StyleSheetSources { get; set; }


        internal string ToHTML()
        {
            string stylesheets = "";
            string scripts = "";

            if(ScriptSources != null) {
                foreach (var item in ScriptSources)
                {
                    scripts += $"<script src='{item}'></script>\n";
                }
            }

            if(StyleSheetSources != null)
            {
                foreach (var item in StyleSheetSources)
                {
                    stylesheets += $"<link rel='stylesheet' href='{item}'/>\n";
                }
            }
            return Resources.Page
                .Replace("<!--TEMPLATE_CONTENT-->", Content)
                .Replace("<!--TEMPLATE_STYLESHEETS-->", stylesheets)
                .Replace("<!--TEMPLATE_SCRIPTS-->", scripts)
                .Replace("<!--TEMPLATE_LOGO-->", LogoUrl)
                .Replace("<!--TEMPLATE_TITLE-->", Title);
        }

    }
}
