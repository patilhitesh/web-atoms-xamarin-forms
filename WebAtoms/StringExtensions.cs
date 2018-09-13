using System;
using System.Collections.Generic;
using System.Text;

namespace WebAtoms
{
    public static class StringExtensions
    {

        public static bool EqualsIgnoreCase(this string text, string compare) {
            if (string.IsNullOrEmpty(text))
                return string.IsNullOrEmpty(compare);
            return text.Equals(compare, StringComparison.OrdinalIgnoreCase);
        }


    }
}
