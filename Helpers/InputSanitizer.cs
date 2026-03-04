using System;
using System.Text.RegularExpressions;

namespace HospitalManagementAvolonia.Helpers
{
    public static class InputSanitizer
    {
        /// <summary>
        /// Sanitizes input strings for SQL queries and prevents XSS by removing common malicious patterns and HTML tags.
        /// Even with parameterized queries, this acts as a defense-in-depth layer.
        /// </summary>
        public static string SanitizeForSql(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Trim whitespace
            var sanitized = input.Trim();

            // Remove SQL comment sequences
            sanitized = sanitized.Replace("--", "")
                                 .Replace("/*", "")
                                 .Replace("*/", "");

            // Remove statement separators
            sanitized = sanitized.Replace(";", "");

            // Strip HTML/XML tags to prevent basic XSS
            sanitized = Regex.Replace(sanitized, @"<[^>]+>|&nbsp;", "").Trim();

            return sanitized;
        }

        /// <summary>
        /// Ensures a string contains only digits and optionally a leading '+', suitable for phones or national IDs.
        /// </summary>
        public static string SanitizeNumericString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var trimmed = input.Trim();
            // Remove everything except digits and plus sign
            var Cleaned = Regex.Replace(trimmed, @"[^\d+]", "");
            
            // Allow '+' only at the very beginning
            if (Cleaned.IndexOf('+') > 0)
            {
                // Remove all '+' signs and put one at the start if it had one
                bool startsWithPlus = Cleaned.StartsWith("+");
                Cleaned = Cleaned.Replace("+", "");
                if (startsWithPlus) Cleaned = "+" + Cleaned;
            }

            return Cleaned;
        }
    }
}
