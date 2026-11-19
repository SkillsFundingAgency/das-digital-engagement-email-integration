using System.Data;
using System.Net;
using System.Text.RegularExpressions;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Validation;

public static class PerformanceImportValidation
{
    private const int MaxHtmlDecodePasses = 3;
    private const string UrlColumnName = "URL";

    private const string HtmlElementNames =
        "a|abbr|address|animate|applet|area|article|aside|audio|b|base|basefont|bdi|bdo|bgsound|big|blink|" +
        "blockquote|body|br|button|canvas|caption|center|cite|code|col|colgroup|data|datalist|dd|del|details|" +
        "dfn|dialog|dir|div|dl|dt|em|embed|fieldset|figcaption|figure|font|footer|foreignobject|form|frame|" +
        "frameset|h[1-6]|head|header|hgroup|hr|html|i|iframe|image|img|input|ins|isindex|kbd|keygen|label|" +
        "legend|li|link|listing|main|map|mark|marquee|math|menu|meta|meter|nav|nobr|noembed|noframes|" +
        "noscript|object|ol|optgroup|option|output|p|param|picture|plaintext|pre|progress|q|rp|rt|ruby|s|" +
        "samp|script|section|select|set|slot|small|source|span|strike|strong|style|sub|summary|sup|svg|" +
        "table|tbody|td|template|textarea|tfoot|th|thead|time|title|tr|track|tt|u|ul|use|var|video|wbr|xml|xmp";

    // As in browsers, '<' must be immediately followed by the tag name, so "a < b > c",
    // "<postmaster>" and "<FirstName>" are plain text, while "<svg/onload=...>" is markup.
    private static readonly Regex HtmlMarkupPattern = new(
        $@"</?(?:{HtmlElementNames})(?=[\s/>])[^<>]*>" +
        @"|<[a-z][a-z0-9-]*(?:[\s/][^<>]*?)?[\s/""']on[a-z]+\s*=[^<>]*>" +
        @"|<!doctype\b|<!--|<!\[cdata\[|<\?[a-z]",
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    private static readonly Regex UnsafeUrlSchemePattern = new(
        @"^\s*(?:javascript|vbscript|data)\s*:",
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    public static void ValidateTable(DataTable table, string tableName)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        foreach (DataRow row in table.Rows)
        {
            foreach (DataColumn column in table.Columns)
            {
                if (row[column] is string text)
                {
                    ValidateString(tableName, column.ColumnName, text);
                }
            }
        }
    }

    private static void ValidateString(
        string tableName,
        string columnName,
        string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        // Decode for validation only; preserve the original value.
        var decoded = HtmlDecodeRepeatedly(value);

        if (ContainsDisallowedCharacter(decoded))
        {
            throw ValidationFailure(
                tableName,
                columnName,
                "unexpected control characters are not allowed");
        }

        bool containsHtml;
        bool containsUnsafeUrlScheme;

        try
        {
            containsHtml = HtmlMarkupPattern.IsMatch(decoded);
            containsUnsafeUrlScheme =
                string.Equals(columnName, UrlColumnName, StringComparison.OrdinalIgnoreCase) &&
                UnsafeUrlSchemePattern.IsMatch(decoded);
        }
        catch (RegexMatchTimeoutException)
        {
            throw ValidationFailure(
                tableName,
                columnName,
                "text validation exceeded the allowed time");
        }

        if (containsHtml)
        {
            throw ValidationFailure(
                tableName,
                columnName,
                "HTML markup is not allowed in plain-text fields");
        }

        if (containsUnsafeUrlScheme)
        {
            throw ValidationFailure(
                tableName,
                columnName,
                "script or data URL schemes are not allowed");
        }
    }

    private static string HtmlDecodeRepeatedly(string value)
    {
        var current = value;

        for (var pass = 0; pass < MaxHtmlDecodePasses; pass++)
        {
            var next = WebUtility.HtmlDecode(current);
            if (string.Equals(next, current, StringComparison.Ordinal))
            {
                break;
            }

            current = next;
        }

        return current;
    }

    private static bool ContainsDisallowedCharacter(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];

            if (char.IsHighSurrogate(character))
            {
                if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    i++;
                    continue;
                }

                return true;
            }

            if (char.IsLowSurrogate(character) || IsDisallowedControlCharacter(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDisallowedControlCharacter(char character)
    {
        // Allow tab, LF and CR. Reject other C0 controls, DEL, C1 controls and
        // bidi override/isolate characters. Zero-width joiners are allowed for emoji.
        return (character < ' ' && character is not '\t' and not '\n' and not '\r')
               || character is >= '\u007F' and <= '\u009F'
               || character is >= '\u202A' and <= '\u202E'
               || character is >= '\u2066' and <= '\u2069';
    }

    private static InvalidOperationException ValidationFailure(
        string tableName,
        string columnName,
        string reason)
    {
        return new InvalidOperationException(
            $"Performance import validation failed for " +
            $"{tableName}.{columnName}: {reason}.");
    }
}
