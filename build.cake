using System.Xml.Linq;
#addin nuget:?package=Cake.DocFx
#addin nuget:?package=Cake.Git&version=0.22.0
#addin nuget:?package=Cake.Incubator&version=3.1.0
//#l "build/appveyor-util.cake"
#l "build/common.cake"
#l "build/git-util.cake"
#tool "docfx.console"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var ToolsDirectory = Directory("./tools/");
var ArtifactsDirectory = Directory("./artifacts");
var SolutionFile = "./AlphaFs.sln";
var DocFxFile = "./docs/docfx.json";
var DocFxArtifactsDirectory = ArtifactsDirectory.Path.Combine("docs");
var WorkDirectory = ToolsDirectory.Path.Combine("_work");
var GitHubProject = BuildSystem.AppVeyor.IsRunningOnAppVeyor ? BuildSystem.AppVeyor.Environment.Repository.Name : "alphaleonis/AlphaFS";
var DocFxBranchName = "gh-pages-lab";
var DocFxArtifactName = "artifacts/docs.zip";
var GitHubCommitName = "AppVeyor";
var GitHubCommitEMail = "alphaleonis-build@users.noreply.github.com";
var AppVeyorApiBaseUrl = "https://ci.appveyor.com/api";
var TestProjectsPattern = "./tests/**/*.csproj";
var PackProjectsPattern = "./src/**/*.csproj";

var msBuildSettings = new DotNetCoreMSBuildSettings
{
    //MaxCpuCount = 1
};

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
});

Teardown(ctx =>
{    
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
    {        
        CleanDirectory(ArtifactsDirectory);
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => 
    {
        DotNetCoreRestore(SolutionFile);
    });
    
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var path = MakeAbsolute(new DirectoryPath(SolutionFile));
        DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            NoRestore = true,
            DiagnosticOutput = true,
            MSBuildSettings = msBuildSettings,
            Verbosity = DotNetCoreVerbosity.Minimal
        });
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCoreTestSettings settings = new DotNetCoreTestSettings()
        {
            NoBuild = true,
            NoRestore = true,
            Logger = "trx"            
        };
        var projects = GetFiles(TestProjectsPattern);
        foreach (var project in projects)
        {
            DotNetCoreTest(project.FullPath, settings);
        }        
    })
    .OnError(error => 
    {
        UploadTestResults();        
    });

void UploadTestResults()
{
    if (BuildSystem.IsRunningOnAppVeyor)
    {
    }
}

Task("PublishTestResults")
    .IsDependentOn("Test")
    .WithCriteria(() => BuildSystem.IsRunningOnAppVeyor)
    .Does(() => 
    {
        UploadTestResults();
    });

Task("Pack")
    .IsDependentOn("PublishTestResults")    
    .Does(() => 
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = ArtifactsDirectory,
            MSBuildSettings = msBuildSettings, 
            NoBuild = true
        };

        var projects = GetFiles(PackProjectsPattern);
        foreach(var project in projects)
        {
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("DocClean")
    .Does(() => 
    {        
        CleanDirectory(DocFxArtifactsDirectory, true);
    });

Task("DocBuild")
    .IsDependentOn("DocClean")
    .Does(() => 
    {        
        DocFxMetadata(DocFxFile);
        DocFxBuild(DocFxFile);
    });

Task("Default")
    .IsDependentOn("Pack")
    .IsDependentOn("DocBuild");


RunTarget(target);




