using System;
using System.IO;

namespace S4UDashboard.Model;

public interface ILocation
{
    public bool IsPhysical { get; }
    public string LocationHint { get; }

    public Stream OpenReadStream();
    public Stream OpenWriteStream();
}

public class UnnamedLocation : ILocation
{
    public bool IsPhysical => false;
    public string LocationHint => "<memory>";

    public Stream OpenReadStream() => throw new NotSupportedException();
    public Stream OpenWriteStream() => throw new NotSupportedException();
}

public readonly record struct FileLocation(string LocationPath) : ILocation
{
    public FileLocation(Uri uri) : this(uri.AbsolutePath) { }

    public bool IsPhysical => true;
    public string LocationHint => Path.GetFileName(LocationPath);

    public Stream OpenReadStream() => File.OpenRead(LocationPath);
    public Stream OpenWriteStream() => File.Open(LocationPath, FileMode.Create, FileAccess.Write);
}
