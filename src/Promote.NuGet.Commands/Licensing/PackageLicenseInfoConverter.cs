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
            if (licenseMetadata.Type == LicenseType.Expression && licenseMetadata.LicenseExpression.ToString() is { } expr && expr.Length > 0)
            {
                return new PackageLicenseInfo.Expression(expr, licenseMetadata.LicenseUrl);
            }

            if (licenseMetadata is { Type: LicenseType.File })
            {
                return new PackageLicenseInfo.File(licenseMetadata.License);
            }
        }

        if (metadata.LicenseUrl != null)
        {
            return new PackageLicenseInfo.Url(metadata.LicenseUrl);
        }

        return new PackageLicenseInfo.None();
    }
}
