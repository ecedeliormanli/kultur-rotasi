using System.Reflection;

namespace KulturRotasi.Domain;

/// <summary>Marker used to locate this assembly (architecture tests, scanning).</summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
