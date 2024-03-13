namespace Promote.NuGet.Commands.Licensing;

public sealed record PackageLicenseInfo(string License, Uri? Url)
{
    public string PrettyPrint()
    {
        if (Url != null && !string.Equals(Url.ToString(), License, StringComparison.OrdinalIgnoreCase))
        {
            return $"{License} ({Url})";
        }

        return License;
    }
}
