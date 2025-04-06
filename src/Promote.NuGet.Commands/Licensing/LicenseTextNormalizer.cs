using System.Text;
using System.Text.RegularExpressions;

namespace Promote.NuGet.Commands.Licensing;

public static partial class LicenseTextNormalizer
{
    public static string NormalizeLicense(string license)
    {
        license = NormalizeWhiteSpaces(license);
        license = CopyrightRegex().Replace(license, "<year>");

        return license;
    }

    private static string NormalizeWhiteSpaces(string source)
    {
        var normalized = new StringBuilder();

        for (var i = 0; i < source.Length; i++)
        {
            var ch = source[i];

            if (char.IsWhiteSpace(ch))
            {
                if (normalized.Length > 0 && normalized[^1] != ' ')
                {
                    normalized.Append(' ');
                }

                continue;
            }

            if (char.IsUpper(ch))
            {
                ch = char.ToLowerInvariant(ch);
            }

            normalized.Append(ch);
        }

        if (normalized.Length > 0 && normalized[^1] == ' ')
        {
            normalized.Length -= 1;
        }

        return normalized.ToString();
    }

    [GeneratedRegex(@"(?<=copyright\s?(?:\(c\)|©)?\s?)\d{4}(?:\s?-\s?\d{4})?(?=$|\D)", RegexOptions.IgnoreCase)]
    private static partial Regex CopyrightRegex();
}
