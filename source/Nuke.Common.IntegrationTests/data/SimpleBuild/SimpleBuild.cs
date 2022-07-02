// Copyright 2022 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using Nuke.Common;

public class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Hello);

    Target Hello => _ => _
        .Executes(() =>
        {
        });
}
