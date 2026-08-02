using System.Collections.Generic;

namespace MyToolz.Localization
{
    public static class LocalizationOrientationDetector
    {
        public static bool TryDetect(List<string[]> rows, HashSet<string> knownLanguageNames, out LocalizationCsvOrientation orientation)
        {
            orientation = LocalizationCsvOrientation.KeysAsRows;

            if (rows == null || rows.Count == 0)
            {
                return false;
            }

            if (TryDetectFromCornerLabel(rows[0], out orientation))
            {
                return true;
            }

            if (knownLanguageNames == null || knownLanguageNames.Count == 0)
            {
                return false;
            }

            string[] headerRow = rows[0];
            int headerMatches = 0;
            for (int c = 1; c < headerRow.Length; c++)
            {
                if (IsKnown(knownLanguageNames, headerRow[c]))
                {
                    headerMatches++;
                }
            }

            int columnMatches = 0;
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length > 0 && IsKnown(knownLanguageNames, row[0]))
                {
                    columnMatches++;
                }
            }

            if (headerMatches == 0 && columnMatches == 0)
            {
                return false;
            }

            orientation = columnMatches > headerMatches
                ? LocalizationCsvOrientation.LanguagesAsRows
                : LocalizationCsvOrientation.KeysAsRows;
            return true;
        }

        private static bool TryDetectFromCornerLabel(string[] headerRow, out LocalizationCsvOrientation orientation)
        {
            orientation = LocalizationCsvOrientation.KeysAsRows;

            string corner = headerRow != null && headerRow.Length > 0 ? headerRow[0]?.Trim() : null;
            if (string.IsNullOrEmpty(corner))
            {
                return false;
            }

            if (corner.Equals("language", System.StringComparison.OrdinalIgnoreCase) ||
                corner.Equals("languages", System.StringComparison.OrdinalIgnoreCase))
            {
                orientation = LocalizationCsvOrientation.LanguagesAsRows;
                return true;
            }

            if (corner.Equals("key", System.StringComparison.OrdinalIgnoreCase) ||
                corner.Equals("keys", System.StringComparison.OrdinalIgnoreCase))
            {
                orientation = LocalizationCsvOrientation.KeysAsRows;
                return true;
            }

            return false;
        }

        private static bool IsKnown(HashSet<string> knownLanguageNames, string name)
            => !string.IsNullOrEmpty(name) && knownLanguageNames.Contains(name.Trim());
    }
}
