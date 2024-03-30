// Copyright 2024 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using FluentAssertions;
using Nuke.Common.Tooling;
using Nuke.Tooling;
using Xunit;

namespace Nuke.Common.Tests;

public partial class ToolOptionsTest
{
    public ToolOptionsTest()
    {
        var rootDirectory = Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory);
        NuGetToolPathResolver.NuGetPackagesConfigFile = rootDirectory / "build" / "_build.csproj";
    }

    [Fact]
    public void TestSimpleToolPath() => new SimpleToolPathToolOptions().GetToolPath().Should().EndWith("xunit.console.exe");

    [Fact]
    public void TestUserCustomToolPath() => new SimpleToolPathToolOptions().SetProcessToolPath("/some/path").GetToolPath().Should().Be("/some/path");

    [Fact]
    public void TestToolCustomToolPath() => new ToolCustomToolPathOptions().GetToolPath().Should().Be(nameof(ToolCustomToolPathTool));

    [Fact]
    public void TestOptionsCustomToolPath() => new OptionsCustomToolPathOptions().GetToolPath().Should().Be(nameof(OptionsCustomToolPathTool));
}

[NuGetTool(PackageId = ["xunit.runner.console"], Executable = ["xunit.console.exe"])]
file class SimpleTool;

[Command(Type = typeof(SimpleTool))]
file class SimpleToolPathToolOptions : ToolOptions;

[NuGetTool]
file class ToolCustomToolPathTool : IToolWithCustomToolPath
{
    string IToolWithCustomToolPath.GetToolPath(ToolOptions options) => nameof(ToolCustomToolPathTool);
}

[Command(Type = typeof(ToolCustomToolPathTool))]
file class ToolCustomToolPathOptions : ToolOptions;

[NuGetTool]
file class OptionsCustomToolPathTool;

[Command(Type = typeof(OptionsCustomToolPathTool))]
file class OptionsCustomToolPathOptions : ToolOptions, IToolOptionsWithCustomToolPath
{
    string IToolOptionsWithCustomToolPath.GetToolPath() => nameof(OptionsCustomToolPathTool);
}
