using System.Linq;
using System.Reflection;
using Nuke.Common.Tools.MSBuild;
using Nuke.Core;
using Nuke.Core.BuildServers;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Core.IO.FileSystemTasks;
using static Nuke.Core.IO.PathConstruction;

class Build : NukeBuild
{
    // Auto-injection fields:
    //  - [GitVersion] must have 'GitVersion.CommandLine' referenced
    //  - [GitRepository] parses the origin from git config
    //  - [Parameter] retrieves its value from command-line arguments or environment variables
    //
    //[GitVersion] readonly GitVersion GitVersion;
    //[GitRepository] readonly GitRepository GitRepository;
    //[Parameter] readonly string MyGetApiKey;

    Target Clean => _ => _
        .OnlyWhen(() => false) // Disabled for safety.
        .Executes(() =>
        {
            DeleteDirectories(GlobDirectories(SourceDirectory, "**/bin", "**/obj"));
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() => { MSBuild(s => DefaultMSBuildRestore); });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // NOTE: workaround until official bugfix
            var av = (AppVeyor) typeof(AppVeyor).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(c => !c.GetParameters().Any())?.Invoke(new object[0]);

            MSBuild(s => DefaultMSBuildCompile
                .SetAssemblyVersion(av?.BuildVersion)
                .SetFileVersion(av?.BuildVersion)
                .SetInformationalVersion(av?.BuildVersion));
        });

    // This is the application entry point for the build.
    // It also defines the default target to execute.
    public static int Main()
    {
        return Execute<Build>(x => x.Compile);
    }
}