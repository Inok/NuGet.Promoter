using NuGet.Packaging;
using NuGet.Protocol.Core.Types;

namespace Promote.NuGet.Commands.Licensing;

public static class PackageLicenseInfoConverter
{
    private const string MsNetLibraryLicenseId = "MICROSOFT .NET LIBRARY";

    private static readonly HashSet<string> _msNetLibraryLicenseUrls =
    [
        "http://go.microsoft.com/fwlink/?LinkId=329770",
        "https://go.microsoft.com/fwlink/?LinkId=329770",
        "https://dotnet.microsoft.com/en-us/dotnet_library_license.htm"
    ];

    public static PackageLicenseInfo FromPackageSearchMetadata(IPackageSearchMetadata metadata)
    {
        var licenseMetadata = metadata.LicenseMetadata;
        if (licenseMetadata != null)
        {
            if (licenseMetadata.Type == LicenseType.Expression)
            {
                return new PackageLicenseInfo(licenseMetadata.License, licenseMetadata.LicenseUrl);
            }

            if (licenseMetadata.Type == LicenseType.File)
            {
                return new PackageLicenseInfo(licenseMetadata.License, null);
            }
        }

        if (metadata.LicenseUrl != null)
        {
            if (_msNetLibraryLicenseUrls.Contains(metadata.LicenseUrl.ToString()))
            {
                return new PackageLicenseInfo(MsNetLibraryLicenseId, metadata.LicenseUrl);
            }

            return new PackageLicenseInfo(metadata.LicenseUrl.ToString(), metadata.LicenseUrl);
        }

        return new PackageLicenseInfo("<not set>", null);
    }
}
