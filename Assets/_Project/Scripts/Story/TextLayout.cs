using System;
using System.Collections.Generic;
using System.Text;

namespace Arash.Story
{
    /// <summary>
    /// Word wrapping done before Persian shaping (Unity's own wrapping would break right-to-left
    /// lines in the wrong order). Works on logical text with any width measure.
    /// </summary>
    public static class TextLayout
    {
        /// <summary>Greedy word wrap; <paramref name="measure"/> returns the display width of a line.</summary>
        public static List<string> Wrap(string text, Func<string, float> measure, float maxWidth)
        {
            var lines = new List<string>();
            foreach (var paragraph in (text ?? string.Empty).Split('\n'))
            {
                var words = paragraph.Split(' ');
                var line = new StringBuilder();
                foreach (var word in words)
                {
                    if (word.Length == 0)
                        continue;
                    var candidate = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && measure(candidate) > maxWidth)
                    {
                        lines.Add(line.ToString());
                        line.Length = 0;
                        line.Append(word);
                    }
                    else
                    {
                        line.Length = 0;
                        line.Append(candidate);
                    }
                }
                lines.Add(line.ToString());
            }
            return lines;
        }
    }
}
