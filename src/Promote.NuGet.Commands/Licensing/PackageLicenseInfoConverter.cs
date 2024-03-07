using NuGet.Packaging;
using NuGet.Protocol.Core.Types;

namespace Promote.NuGet.Commands.Licensing;

public static class PackageLicenseInfoConverter
{
    public static PackageLicenseInfo FromPackageSearchMetadata(IPackageSearchMetadata metadata)
    {
        var licenseMetadata = metadata.LicenseMetadata;
        if (licenseMetadata != null)
        {
            if (licenseMetadata.Type == LicenseType.Expression)
            {
                return new PackageLicenseInfo(licenseMetadata.License, licenseMetadata.LicenseUrl);
            }

            if (licenseMetadata is { Type: LicenseType.File })
            {
                return new PackageLicenseInfo(licenseMetadata.License, null);
            }
        }

        if (metadata.LicenseUrl != null)
        {
            return new PackageLicenseInfo(metadata.LicenseUrl.ToString(), metadata.LicenseUrl);
        }

        return new PackageLicenseInfo("<not set>", null);
    }
}
