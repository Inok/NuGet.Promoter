namespace Promote.NuGet.Commands.Licensing;

public abstract record PackageLicenseInfo
{
    public sealed record None : PackageLicenseInfo;

    public sealed record Expression(string License, Uri Uri) : PackageLicenseInfo;

    public sealed record Url(Uri Uri) : PackageLicenseInfo;

    public sealed record File(string FileName) : PackageLicenseInfo;
}
