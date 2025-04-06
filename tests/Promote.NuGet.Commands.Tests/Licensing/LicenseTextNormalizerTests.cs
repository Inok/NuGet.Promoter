using Promote.NuGet.Commands.Licensing;

namespace Promote.NuGet.Commands.Tests.Licensing;

[TestFixture]
public class LicenseTextNormalizerTests
{
    [TestCase("  line   1 \r\n  \r line \n  2 \r \n line 3 \r\n  \r\n", "line 1 line 2 line 3")]
    [TestCase("", "")]
    [TestCase("\n\r\n\t  \r\r\r  \r", "")]
    public void NormalizeLicense_normalizes_white_spaces(string source, string expected)
    {
        var actual = LicenseTextNormalizer.NormalizeLicense(source);

        actual.Should().Be(expected);
    }

    [TestCase("LINE 1\r\nline 2\r\nLiNe 3", "line 1 line 2 line 3")]
    public void NormalizeLicense_normalizes_case(string source, string expected)
    {
        var actual = LicenseTextNormalizer.NormalizeLicense(source);

        actual.Should().Be(expected);
    }

    [TestCase("copyright (c) 2025", "copyright (c) <year>")]
    [TestCase("copyright (c) 100500", "copyright (c) 100500")]
    [TestCase("copyright (c) 2025-19th-century-movies", "copyright (c) <year>-19th-century-movies")]
    [TestCase("line 1\r\ncopyright (c) 2025 line 2\r\n line 3", "line 1 copyright (c) <year> line 2 line 3")]
    [TestCase("line 1\r\nline 2 copyright © 2020-2025\r\n line 3", "line 1 line 2 copyright © <year> line 3")]
    [TestCase("\r\nCoPyRiGhT\r\n(C)\r\n1990\r\n", "copyright (c) <year>")]
    public void NormalizeLicense_normalizes_copyright(string source, string expected)
    {
        var actual = LicenseTextNormalizer.NormalizeLicense(source);

        actual.Should().Be(expected);
    }
}
