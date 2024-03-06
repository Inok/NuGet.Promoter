using System.IO;

namespace Promote.NuGet.TestInfrastructure;

public sealed class TempFile : IDisposable
{
    public string Path { get; }

    private TempFile(string path)
    {
        Path = path;
    }

    public async Task Write(string text)
    {
        await File.WriteAllTextAsync(Path, text);
    }

    public Stream OpenStream()
    {
        return File.Open(Path, FileMode.OpenOrCreate);
    }

    public static async Task<TempFile> Create(string text)
    {
        var file = Create();
        await file.Write(text);
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
