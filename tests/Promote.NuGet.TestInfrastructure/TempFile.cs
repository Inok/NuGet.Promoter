using System.IO;

namespace Promote.NuGet.TestInfrastructure;

public sealed class TempFile : IDisposable
{
    public string Path { get; }

    private TempFile(string path)
    {
        Path = path;
    }

    public async Task WriteLines(params string[] lines)
    {
        await File.WriteAllLinesAsync(Path, lines);
    }

    public Stream OpenStream()
    {
        return File.Open(Path, FileMode.OpenOrCreate);
    }

    public static async Task<TempFile> Create(params string[] lines)
    {
        var file = Create();
        await file.WriteLines(lines);
        return file;
    }

    public static TempFile Create()
    {
        var path = System.IO.Path.GetTempFileName();
        return new TempFile(path);
    }

    public void Dispose()
    {
        File.Delete(Path);
    }
}
