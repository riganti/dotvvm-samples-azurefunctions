using System;
using System.Collections.Generic;
using System.Text;

namespace DotvvmSamples.AzureFunctions.Functions
{
    public static class Helpers
    {

        public static string RemoveUnsafeChars(string value, bool allowUrlChars = false)
        {
            value = value.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]) || (allowUrlChars && (value[i] == '.' || value[i] == '_' || value[i] == '-')))
                {
                    sb.Append(value[i]);
                }
                else if (allowUrlChars && sb.Length > 0 && sb[sb.Length - 1] != '-')
                {
                    sb.Append('-');
                }
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == '-')
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

    }
}
