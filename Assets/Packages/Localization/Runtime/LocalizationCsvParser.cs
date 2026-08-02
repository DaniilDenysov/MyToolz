using System.Collections.Generic;
using System.Text;

namespace MyToolz.Localization
{
    public static class LocalizationCsvParser
    {
        private const char Bom = (char)0xFEFF;

        private static readonly char[] CandidateDelimiters = { ',', ';', '\t' };

        public static List<string[]> Parse(string content)
        {
            content = StripBom(content);
            return Parse(content, DetectDelimiter(content));
        }

        public static List<string[]> Parse(string content, char delimiter)
        {
            List<string[]> rows = new List<string[]>();

            content = StripBom(content);
            if (string.IsNullOrEmpty(content))
            {
                return rows;
            }

            List<string> fields = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;
            int i = 0;

            while (i < content.Length)
            {
                char c = content[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        bool escapedQuote = i + 1 < content.Length && content[i + 1] == '"';
                        if (escapedQuote)
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }

                        inQuotes = false;
                        i++;
                        continue;
                    }

                    field.Append(c);
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\r')
                {
                }
                else if (c == '\n')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    rows.Add(fields.ToArray());
                    fields.Clear();
                }
                else
                {
                    field.Append(c);
                }

                i++;
            }

            if (field.Length > 0 || fields.Count > 0)
            {
                fields.Add(field.ToString());
                rows.Add(fields.ToArray());
            }

            return rows;
        }

        public static char DetectDelimiter(string content)
        {
            content = StripBom(content);
            if (string.IsNullOrEmpty(content))
            {
                return ',';
            }

            int lineEnd = content.IndexOf('\n');
            string firstLine = lineEnd >= 0 ? content.Substring(0, lineEnd) : content;

            char best = ',';
            int bestCount = CountOutsideQuotes(firstLine, ',');

            foreach (char candidate in CandidateDelimiters)
            {
                if (candidate == ',')
                {
                    continue;
                }

                int count = CountOutsideQuotes(firstLine, candidate);
                if (count > bestCount)
                {
                    best = candidate;
                    bestCount = count;
                }
            }

            return best;
        }

        private static int CountOutsideQuotes(string line, char target)
        {
            int count = 0;
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == target && !inQuotes)
                {
                    count++;
                }
            }

            return count;
        }

        private static string StripBom(string content)
            => !string.IsNullOrEmpty(content) && content[0] == Bom ? content.Substring(1) : content;
    }
}
