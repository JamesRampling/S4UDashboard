using System;
using System.IO;

namespace S4UDashboard.Model;

/// <summary>
/// Represents a dataset's location, whether it is backed by a file or only in memory.
/// </summary>
public interface ILocation
{
    /// <summary>
    /// If a location is physical, it means it has a non-volatile
    /// backing representation that can be written to.
    /// </summary>
    public bool IsPhysical { get; }

    /// <summary>
    /// A user-facing string that hints to where the location is stored.
    /// </summary>
    public string LocationHint { get; }

    /// <summary>
    /// Opens a readable stream from the location.
    /// May not be implemented on non-physical locations.
    /// </summary>
    public Stream OpenReadStream();

    /// <summary>
    /// Opens a writable stream to the location.
    /// May not be implemented on non-physical locations.
    /// </summary>
    public Stream OpenWriteStream();
}

/// <summary>Represents an unnamed location stored only in memory.
/// <para>
/// Only equal by reference equality, as each unnamed location is a unique location.
/// This location is not physical.
/// </para>
/// </summary>
public class UnnamedLocation : ILocation
{
    public bool IsPhysical => false;
    public string LocationHint => "<memory>";

    /// <summary>Not implemented for this class.</summary>
    public Stream OpenReadStream() => throw new NotSupportedException();

    /// <summary>Not implemented for this class.</summary>
    public Stream OpenWriteStream() => throw new NotSupportedException();
}

/// <summary>Represents a location with a corresponding file on disk.
/// <para>
/// Equal by file path.
/// This location is physical.
/// </para>
/// </summary>
public readonly record struct FileLocation(string LocationPath) : ILocation
{
    /// <summary>Constructs a location from the absolute path of a URI.</summary>
    public FileLocation(Uri uri) : this(uri.AbsolutePath) { }

    public bool IsPhysical => true;
    public string LocationHint => Path.GetFileName(LocationPath);

    /// <summary>Returns a readable stream from the file at this location.</summary>
    public Stream OpenReadStream() => File.OpenRead(LocationPath);

    /// <summary>Returns a writable stream to the file at this location.</summary>
    public Stream OpenWriteStream() => File.Open(LocationPath, FileMode.Create, FileAccess.Write);
}
