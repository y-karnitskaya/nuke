using System;
using System.Linq;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Tooling;

/// <summary>Marks a class as CLI tool wrapper.</summary>
public abstract class ToolAttribute : Attribute
{
    internal abstract string GetToolPath(ToolOptions options);

    public string Arguments { get; set; }

    public Type EscapeType { get; set; }
    public string EscapeMethod { get; set; }
}

public class CommandAttribute : Attribute
{
    public Type Type { get; set; }
    public string Command { get; set; }
    public string Arguments { get; set; }
}

public class Builder : Attribute
{
    public Type Type { get; set; }
    public string Property { get; set; }
}


public class NuGetToolAttribute : ToolAttribute
{
    public string[] Executable { get; set; }
    public string[] PackageId { get; set; }
    public string FrameworkProperty { get; set; }

    internal override string GetToolPath(ToolOptions options)
    {
        var framework = FrameworkProperty != null
            ? options.GetType().GetProperty(FrameworkProperty)?.GetValue<string>(options)
            : null;
        return NuGetToolPathResolver.GetPackageExecutable(PackageId.Join("|"), Executable.Join("|"), framework);
    }
}

public class ArgumentAttribute : Attribute
{
    public int Position { get; set; }
    public string Format { get; set; }
    public string AlternativeFormat { get; set; }
    public bool? IsSecret { get; set; }

    public Type FormatterType { get; set; }
    public string FormatterMethod { get; set; }

    public string ListSeparator { get; set; }
    public string ItemSeparator { get; set; }
}
