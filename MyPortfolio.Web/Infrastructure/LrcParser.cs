using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MyPortfolio.Web.Infrastructure
{
    public class LrcLine
    {
        public double Time { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public static class LrcParser
    {
        private static readonly Regex LrcRegex = new Regex(@"^\s*\[(\d{2}):(\d{2})(?:[.:](\d{2,3}))?\](.*)", RegexOptions.Compiled);

        public static List<LrcLine> Parse(string? lrcContent)
        {
            var result = new List<LrcLine>();
            if (string.IsNullOrWhiteSpace(lrcContent))
                return result;

            var lines = lrcContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = LrcRegex.Match(line);
                if (match.Success)
                {
                    int minutes = int.Parse(match.Groups[1].Value);
                    int seconds = int.Parse(match.Groups[2].Value);
                    double msValue = 0;
                    if (match.Groups[3].Success)
                    {
                        string msStr = match.Groups[3].Value;
                        msValue = double.Parse(msStr) / Math.Pow(10, msStr.Length);
                    }
                    double time = minutes * 60 + seconds + msValue;
                    string text = match.Groups[4].Value.Trim();
                    result.Add(new LrcLine { Time = time, Text = text });
                }
            }

            return result;
        }
    }
}
