using System.Text;
using System.Text.RegularExpressions;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal static partial class SmtpHelper
{
    internal static string ClearHtml(string html)
    {
        var clear = new StringBuilder(html)
            .Replace("<br>", null)
            .Replace("<p>", null).Replace("</p>", null)
            .Replace("<ol>", null).Replace("</ol>", null)
            .Replace("<ul>", null).Replace("</ul>", null)
            .Replace("<strong>", null).Replace("</strong>", null)
            .Replace("<li>", "- ").Replace("</li>", null)
            .ToString();

        return LinkRegex().Replace(clear, m => $"{m.Groups["text"]} ({m.Groups["url"]})");
    }

    [GeneratedRegex(@"<a href=""(?<url>[^""]+)"">(?<text>[^<]+)</a>")]
    private static partial Regex LinkRegex();
}
