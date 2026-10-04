using System.Collections.Generic;
using System.Text;

namespace Arash.Localization
{
    /// <summary>
    /// Prepares Persian (and Arabic) text for Unity's text renderers, which neither join letters nor
    /// lay out right-to-left: letters are replaced with their contextual presentation forms
    /// (isolated / initial / medial / final, plus lam-alef ligatures) and each line is reordered into
    /// visual order. Latin words and numbers keep their left-to-right order inside the line.
    /// The font must contain the Arabic Presentation Forms (Vazirmatn does).
    /// Lines are processed separately, so break long text with '\n' instead of relying on wrapping.
    /// </summary>
    public static class PersianShaper
    {
        struct Forms
        {
            public readonly char Isolated, Final, Initial, Medial;

            public Forms(char isolated, char final, char initial, char medial)
            {
                Isolated = isolated;
                Final = final;
                Initial = initial;
                Medial = medial;
            }

            public bool JoinsBoth { get { return Initial != '\0'; } }
        }

        const char Zwnj = '\u200C';
        const char Zwj = '\u200D';
        const char Tatweel = '\u0640';
        const char Lam = '\u0644';

        // Generated from the Unicode decompositions of the presentation-form blocks.
        static readonly Dictionary<char, Forms> Table = new Dictionary<char, Forms>
        {
            { '\u0621', new Forms('\uFE80', '\0', '\0', '\0') }, // hamza
            { '\u0622', new Forms('\uFE81', '\uFE82', '\0', '\0') }, // alef with madda above
            { '\u0623', new Forms('\uFE83', '\uFE84', '\0', '\0') }, // alef with hamza above
            { '\u0624', new Forms('\uFE85', '\uFE86', '\0', '\0') }, // waw with hamza above
            { '\u0625', new Forms('\uFE87', '\uFE88', '\0', '\0') }, // alef with hamza below
            { '\u0626', new Forms('\uFE89', '\uFE8A', '\uFE8B', '\uFE8C') }, // yeh with hamza above
            { '\u0627', new Forms('\uFE8D', '\uFE8E', '\0', '\0') }, // alef
            { '\u0628', new Forms('\uFE8F', '\uFE90', '\uFE91', '\uFE92') }, // beh
            { '\u0629', new Forms('\uFE93', '\uFE94', '\0', '\0') }, // teh marbuta
            { '\u062A', new Forms('\uFE95', '\uFE96', '\uFE97', '\uFE98') }, // teh
            { '\u062B', new Forms('\uFE99', '\uFE9A', '\uFE9B', '\uFE9C') }, // theh
            { '\u062C', new Forms('\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0') }, // jeem
            { '\u062D', new Forms('\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4') }, // hah
            { '\u062E', new Forms('\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8') }, // khah
            { '\u062F', new Forms('\uFEA9', '\uFEAA', '\0', '\0') }, // dal
            { '\u0630', new Forms('\uFEAB', '\uFEAC', '\0', '\0') }, // thal
            { '\u0631', new Forms('\uFEAD', '\uFEAE', '\0', '\0') }, // reh
            { '\u0632', new Forms('\uFEAF', '\uFEB0', '\0', '\0') }, // zain
            { '\u0633', new Forms('\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4') }, // seen
            { '\u0634', new Forms('\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8') }, // sheen
            { '\u0635', new Forms('\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC') }, // sad
            { '\u0636', new Forms('\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0') }, // dad
            { '\u0637', new Forms('\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4') }, // tah
            { '\u0638', new Forms('\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8') }, // zah
            { '\u0639', new Forms('\uFEC9', '\uFECA', '\uFECB', '\uFECC') }, // ain
            { '\u063A', new Forms('\uFECD', '\uFECE', '\uFECF', '\uFED0') }, // ghain
            { '\u0641', new Forms('\uFED1', '\uFED2', '\uFED3', '\uFED4') }, // feh
            { '\u0642', new Forms('\uFED5', '\uFED6', '\uFED7', '\uFED8') }, // qaf
            { '\u0643', new Forms('\uFED9', '\uFEDA', '\uFEDB', '\uFEDC') }, // kaf
            { '\u0644', new Forms('\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0') }, // lam
            { '\u0645', new Forms('\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4') }, // meem
            { '\u0646', new Forms('\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8') }, // noon
            { '\u0647', new Forms('\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC') }, // heh
            { '\u0648', new Forms('\uFEED', '\uFEEE', '\0', '\0') }, // waw
            { '\u0649', new Forms('\uFEEF', '\uFEF0', '\uFBE8', '\uFBE9') }, // alef maksura
            { '\u064A', new Forms('\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4') }, // yeh
            { '\u067E', new Forms('\uFB56', '\uFB57', '\uFB58', '\uFB59') }, // peh
            { '\u0686', new Forms('\uFB7A', '\uFB7B', '\uFB7C', '\uFB7D') }, // tcheh
            { '\u0698', new Forms('\uFB8A', '\uFB8B', '\0', '\0') }, // jeh
            { '\u06A9', new Forms('\uFB8E', '\uFB8F', '\uFB90', '\uFB91') }, // keheh
            { '\u06AF', new Forms('\uFB92', '\uFB93', '\uFB94', '\uFB95') }, // gaf
            { '\u06CC', new Forms('\uFBFC', '\uFBFD', '\uFBFE', '\uFBFF') }, // farsi yeh
        };

        static readonly Dictionary<char, char[]> LamAlef = new Dictionary<char, char[]>
        {
            // alef variant -> { isolated ligature, final ligature }
            { '\u0622', new[] { '\uFEF5', '\uFEF6' } },
            { '\u0623', new[] { '\uFEF7', '\uFEF8' } },
            { '\u0625', new[] { '\uFEF9', '\uFEFA' } },
            { '\u0627', new[] { '\uFEFB', '\uFEFC' } },
        };

        static readonly Dictionary<char, char> Mirrored = new Dictionary<char, char>
        {
            { '(', ')' }, { ')', '(' }, { '[', ']' }, { ']', '[' }, { '{', '}' }, { '}', '{' },
            { '<', '>' }, { '>', '<' }, { '\u00AB', '\u00BB' }, { '\u00BB', '\u00AB' },
        };

        public static bool IsRtl(char c)
        {
            return (c >= '\u0590' && c <= '\u08FF' && !IsDigit(c)) || (c >= '\uFB1D' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF');
        }

        public static bool ContainsRtl(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            foreach (var c in text)
                if (IsRtl(c))
                    return true;
            return false;
        }

        /// <summary>Shapes and reorders every line that contains right-to-left text.</summary>
        public static string Shape(string text)
        {
            if (!ContainsRtl(text))
                return text;

            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
                if (ContainsRtl(lines[i]))
                    lines[i] = ToVisualOrder(ShapeLetters(lines[i]));
            return string.Join("\n", lines);
        }

        /// <summary>Replaces letters with their joined forms; still in logical order. Removes ZWNJ/ZWJ.</summary>
        public static string ShapeLetters(string text)
        {
            var result = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == Zwnj || c == Zwj)
                    continue;

                Forms forms;
                if (!Table.TryGetValue(c, out forms))
                {
                    result.Append(c);
                    continue;
                }

                var previous = Neighbour(text, i, -1);
                var next = Neighbour(text, i, 1);
                var joinsPrevious = previous >= 0 && JoinsToNext(text[previous]) && forms.Final != '\0';

                char[] ligature;
                if (c == Lam && next >= 0 && LamAlef.TryGetValue(text[next], out ligature))
                {
                    result.Append(joinsPrevious ? ligature[1] : ligature[0]);
                    i = next;
                    continue;
                }

                var joinsNext = forms.JoinsBoth && next >= 0 && JoinsToPrevious(text[next]);
                if (joinsPrevious && joinsNext)
                    result.Append(forms.Medial);
                else if (joinsPrevious)
                    result.Append(forms.Final);
                else if (joinsNext)
                    result.Append(forms.Initial);
                else
                    result.Append(forms.Isolated);
            }
            return result.ToString();
        }

        /// <summary>
        /// Reorders one shaped line from logical to visual (left-to-right) order for a right-to-left
        /// paragraph: runs of Latin letters and digits keep their order, everything else is reversed
        /// and brackets are mirrored.
        /// </summary>
        public static string ToVisualOrder(string line)
        {
            var count = line.Length;
            var ltr = new bool[count];
            for (var i = 0; i < count; i++)
                ltr[i] = IsLtrStrong(line[i]);

            // A neutral (space, punctuation) is left-to-right only when surrounded by left-to-right text.
            for (var i = 0; i < count; i++)
            {
                if (ltr[i] || IsRtl(line[i]))
                    continue;
                ltr[i] = StrongIsLtr(line, i, -1) && StrongIsLtr(line, i, 1);
            }

            var result = new StringBuilder(count);
            var end = count;
            while (end > 0)
            {
                var start = end - 1;
                var runIsLtr = ltr[start];
                while (start > 0 && ltr[start - 1] == runIsLtr)
                    start--;

                if (runIsLtr)
                {
                    result.Append(line, start, end - start);
                }
                else
                {
                    for (var i = end - 1; i >= start; i--)
                    {
                        char mirror;
                        result.Append(Mirrored.TryGetValue(line[i], out mirror) ? mirror : line[i]);
                    }
                }
                end = start;
            }
            return result.ToString();
        }

        /// <summary>Converts ASCII digits to Persian digits.</summary>
        public static string ToPersianDigits(string text)
        {
            var chars = text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
                if (chars[i] >= '0' && chars[i] <= '9')
                    chars[i] = (char)('\u06F0' + (chars[i] - '0'));
            return new string(chars);
        }

        static bool IsDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= '\u06F0' && c <= '\u06F9') || (c >= '\u0660' && c <= '\u0669');
        }

        static bool IsLtrStrong(char c)
        {
            return IsDigit(c) || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '\u00C0' && c <= '\u024F');
        }

        static bool StrongIsLtr(string line, int from, int step)
        {
            for (var i = from + step; i >= 0 && i < line.Length; i += step)
            {
                if (IsLtrStrong(line[i]))
                    return true;
                if (IsRtl(line[i]))
                    return false;
            }
            return false; // the paragraph direction (right-to-left) wins at the edges
        }

        static bool IsTransparent(char c)
        {
            return (c >= '\u064B' && c <= '\u065F') || c == '\u0670';
        }

        /// <summary>Index of the nearest non-transparent character in a direction, or -1.</summary>
        static int Neighbour(string text, int index, int step)
        {
            for (var i = index + step; i >= 0 && i < text.Length; i += step)
                if (!IsTransparent(text[i]))
                    return i;
            return -1;
        }

        static bool JoinsToNext(char c)
        {
            Forms forms;
            return c == Zwj || c == Tatweel || (Table.TryGetValue(c, out forms) && forms.JoinsBoth);
        }

        static bool JoinsToPrevious(char c)
        {
            Forms forms;
            return c == Zwj || c == Tatweel || (Table.TryGetValue(c, out forms) && forms.Final != '\0');
        }
    }
}
