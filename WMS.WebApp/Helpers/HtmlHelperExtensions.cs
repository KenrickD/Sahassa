using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace WMS.WebApp.Helpers
{
    // Add this extension method in a Helpers class
    public static class HtmlHelperExtensions
    {
        public static string GetRawString(this IHtmlContent content)
        {
            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }
        // In HtmlHelperExtensions.cs, add this method:

        /// <summary>
        /// Generates a meta tag with the anti-forgery token for AJAX requests
        /// </summary>
        /// <param name="html">The HTML helper</param>
        /// <returns>An HTML meta tag containing the anti-forgery token</returns>
        public static IHtmlContent AntiForgeryTokenMeta(this IHtmlHelper html)
        {
            var antiForgeryToken = html.AntiForgeryToken();
            var tokenValue = GetRawString(antiForgeryToken);

            // Extract just the value from the input tag
            var tokenStart = tokenValue.IndexOf("value=\"") + 7;
            var tokenEnd = tokenValue.IndexOf("\"", tokenStart);
            var extractedToken = tokenValue.Substring(tokenStart, tokenEnd - tokenStart);

            var meta = new TagBuilder("meta");
            meta.Attributes.Add("name", "csrf-token");
            meta.Attributes.Add("content", extractedToken);

            return meta;
        }
    }
}