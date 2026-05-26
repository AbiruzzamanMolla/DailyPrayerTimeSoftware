using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DailyPrayerTime.Native.Helpers
{
    public static class HtmlTextBlockHelper
    {
        public static void ParseHtmlToTextBlock(TextBlock textBlock, string html)
        {
            textBlock.Inlines.Clear();
            if (string.IsNullOrEmpty(html)) return;

            // Simple parser for basic tags: <b>, <strong>, <i>, <em>, <br>, <br/>, <br />
            // It splits the string by tags
            string pattern = @"(</?b>|</?strong>|</?i>|</?em>|<br\s*/?>)";
            var parts = Regex.Split(html, pattern, RegexOptions.IgnoreCase);

            bool isBold = false;
            bool isItalic = false;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                string lowerPart = part.ToLower();

                if (lowerPart == "<b>" || lowerPart == "<strong>")
                {
                    isBold = true;
                }
                else if (lowerPart == "</b>" || lowerPart == "</strong>")
                {
                    isBold = false;
                }
                else if (lowerPart == "<i>" || lowerPart == "<em>")
                {
                    isItalic = true;
                }
                else if (lowerPart == "</i>" || lowerPart == "</em>")
                {
                    isItalic = false;
                }
                else if (lowerPart == "<br>" || lowerPart == "<br/>" || lowerPart == "<br />")
                {
                    textBlock.Inlines.Add(new LineBreak());
                }
                else
                {
                    // It's text content. If it contains other HTML tags, just strip them as a fallback.
                    string cleanText = Regex.Replace(part, "<.*?>", string.Empty);
                    if (!string.IsNullOrEmpty(cleanText))
                    {
                        // Decode common HTML entities
                        cleanText = System.Net.WebUtility.HtmlDecode(cleanText);

                        Run run = new Run(cleanText);
                        if (isBold) run.FontWeight = FontWeights.Bold;
                        if (isItalic) run.FontStyle = FontStyles.Italic;
                        
                        textBlock.Inlines.Add(run);
                    }
                }
            }
        }
    }
}
