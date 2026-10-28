using System;

namespace WellLogs.Core.Las
{
    public static class HeaderValueResolver
    {
        public static string Resolve(string mnemonic, string value, string description)
        {
            if (value.Length == 0 || ContainsDigit(value))
            {
                return value;
            }

            if (description.Length == 0)
            {
                return string.Equals(value.Trim(), mnemonic.Trim(), StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
            }

            bool valueLooksLikeLabel = IsSubsequence(mnemonic, value);
            bool descLooksLikeLabel = IsSubsequence(mnemonic, description);

            if (valueLooksLikeLabel && (!descLooksLikeLabel || value.Length < description.Length))
            {
                return description;
            }

            return value;
        }

        private static bool ContainsDigit(string s)
        {
            foreach (char c in s)
            {
                if (char.IsDigit(c))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSubsequence(string needle, string haystack)
        {
            int i = 0;
            foreach (char c in haystack)
            {
                if (i < needle.Length && char.ToUpperInvariant(c) == char.ToUpperInvariant(needle[i]))
                {
                    i++;
                }
            }
            return i == needle.Length;
        }
    }
}