// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner.AzureDevOps;

namespace SourceBuild.Pipelines;

public static class PipelineParameters
{
    public const string StagingPipelineName = "dotnet-staging-pipeline-resource";

    public static StringParameter DotnetStagingPipelineResource { get; } =
        new("dotnetStagingPipelineResource");

    public static StringParameter DotnetMajorVersion { get; } =
        new("dotnetMajorVersion","Major .NET version being released", allowedValues: new[] { "6.0", "7.0", "8.0" });
    public static StringParameter ReleaseName { get; } =
        new("releaseName", "Release name (e.g. \".NET 8.0 Preview 1\")");
    public static StringParameter ReleaseBranchName { get; } =
        new("releaseBranchName", "Release branch name (e.g. release/8.0.1xx-preview1)");
    public static BooleanParameter IsPreviewRelease { get; } =
        new("isPreviewRelease", "Preview release");

    public static BooleanParameter UseCustomTag { get; } =
        new("useCustomTag", "Use custom tag");
    public static StringParameter CustomTag { get; } =
        new("customTag", "Custom release tag (e.g. v6.0.XYY-source-build)");

    public static BooleanParameter UseSpecificPipelineRunIDs { get; } =
        new("useSpecificPipelineRunIDs", "Use specific pipeline run IDs");
    public static StringParameter DotnetDotnetRunID { get; } =
        new("dotnetDotnetRunID", "[⚠️ 8.0] Specific dotnet-dotnet run name");
    public static StringParameter DotnetInstallerOfficialRunID { get; } =
        new("dotnetInstallerOfficialRunID", "[⚠️ 6.0 / 7.0] Specific dotnet-installer-official-ci run name");
    public static StringParameter DotnetInstallerTarballBuildRunID { get; } =
        new("dotnetInstallerTarballBuildRunID", "[⚠️ 6.0 / 7.0] Specific dotnet-installer-source-build-tarball-build run name");
    public static BooleanParameter VerifyBuildSuccess { get; } =
        new("verifyBuildSuccess", "Verify that associated pipeline runs succeeded");

    public static BooleanParameter SkipPackageMirroring { get; } =
        new("skipPackageMirroring", "Skip package mirroring");

    public static BooleanParameter CreateReleaseAnnouncement { get; } =
        new("createReleaseAnnouncement", "Create release announcement");
    public static StringParameter AnnouncementGist { get; } =
        new("announcementGist", "Release announcement gist URL");

    public static BooleanParameter SubmitReleasePR { get; } =
        new("submitReleasePR", "Submit release PR");

    // Auto means that for dry run, we only create a draft release; full otherwise.
    public static StringParameter CreateGitHubRelease { get; } =
        new("createGitHubRelease", "[⚠️ 8.0] Create tag & release in dotnet/dotnet", allowedValues: new[] { "auto", "skip", "draft", "full" });

    public static BooleanParameter IsDryRun { get; } =
        new BooleanParameter("isDryRun", "Dry run");
}
