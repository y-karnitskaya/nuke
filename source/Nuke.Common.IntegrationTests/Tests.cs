// Copyright 2022 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Xunit;
using Xunit.Abstractions;

namespace Nuke.Common.IntegrationTests;

public class Tests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private static AbsolutePath RootDirectory => Constants.TryGetRootDirectoryFrom(Directory.GetCurrentDirectory()).NotNull();
    private static AbsolutePath SourceDirectory => RootDirectory / "source";
    private static AbsolutePath IntegrationTestsDirectory => SourceDirectory / "Nuke.Common.IntegrationTests";

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        DotNetTasks.DotNetPack(_ => _
            .SetProject(SourceDirectory / "Nuke.Common" / "Nuke.Common.csproj")
            .SetOutputDirectory(IntegrationTestsDirectory / "nuget"));
    }

    // exit code
    // output
    // file structure
    // file content (logs)

    // --help output
    // generated ci/cd files
    // error output:  target doesn't exist
    [Fact]
    public void Foo()
    {
        DotNetTasks.DotNetRun(_ => _
            .SetProjectFile(IntegrationTestsDirectory / "data" / "SimpleBuild" / "SimpleBuild.csproj")
            .SetProcessWorkingDirectory(IntegrationTestsDirectory / "data" / "SimpleBuild")
            .SetProcessEnvironmentVariable("NUGET_PACKAGES", IntegrationTestsDirectory / "packages")
            .SetProcessEnvironmentVariable(Telemetry.OptOutEnvironmentKey, "1"));
    }

    [Fact]
    public void Bar()
    {
        var nukeGlobalTool = ToolPathResolver.GetPathExecutable("nuke");

        var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = nukeGlobalTool,
                Arguments = ":setup --root",
                WorkingDirectory = IntegrationTestsDirectory / "data" / "Setup",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8
            });

        var builder = new StringBuilder();
        process.OutputDataReceived += (_, args) => _testOutputHelper.WriteLine(args.Data);
        process.BeginOutputReadLine();

        _testOutputHelper.WriteLine("fooo");
        process.StandardInput.AutoFlush = true;
        process.StandardInput.WriteLine();
        process.StandardInput.WriteLine();
        process.StandardInput.WriteLine();
        process.StandardInput.WriteLine();
        process.StandardInput.WriteLine();
        process.StandardInput.WriteLine();

        process.WaitForExit();
    }
}
