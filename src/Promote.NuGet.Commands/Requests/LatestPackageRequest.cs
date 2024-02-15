namespace Promote.NuGet.Commands.Requests;

public class LatestPackageRequest : IPackageRequest
{
    public string Id { get; }

    public LatestPackageRequest(string id)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));

        Id = id;
    }

    public Task<T> Accept<T>(IPackageRequestVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return $"{Id} @latest";
    }
}
