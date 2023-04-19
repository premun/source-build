// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner.AzureDevOps;

namespace SourceBuild.Pipelines;

public static class ReleaseParameters
{
    public const string StagingPipelineName = "dotnet-staging-pipeline-resource";
    public const string StagingPipelineResource = "dotnetStagingPipelineResource";

    public static Parameter DotnetMajorVersion { get; } =
        new StringParameter("dotnetMajorVersion","Major .NET version being released", allowedValues: new[] { "6.0", "7.0", "8.0" });
    public static Parameter ReleaseName { get; } =
        new StringParameter("releaseName", "Release name (e.g. \".NET 8.0 Preview 1\")");
    public static Parameter ReleaseBranchName { get; } =
        new StringParameter("releaseBranchName", "Release branch name (e.g. release/8.0.1xx-preview1)");
    public static Parameter IsPreviewRelease { get; } =
        new BooleanParameter("isPreviewRelease", "Preview release", false);

    public static Parameter UseCustomTag { get; } =
        new BooleanParameter("useCustomTag", "Use custom tag", false);
    public static Parameter CustomTag { get; } =
        new StringParameter("customTag", "Custom release tag (e.g. v6.0.XYY-source-build)", " ");

    public static Parameter UseSpecificPipelineRunIDs { get; } =
        new BooleanParameter("useSpecificPipelineRunIDs", "Use specific pipeline run IDs", false);
    public static Parameter DotnetDotnetRunID { get; } =
        new StringParameter("dotnetDotnetRunID", "[⚠️ 8.0] Specific dotnet-dotnet run name", "202XXXXX.Y");
    public static Parameter DotnetInstallerOfficialRunID { get; } =
        new StringParameter("dotnetInstallerOfficialRunID", "[⚠️ 6.0 / 7.0] Specific dotnet-installer-official-ci run name", "202XXXXX.Y");
    public static Parameter DotnetInstallerTarballBuildRunID { get; } =
        new StringParameter("dotnetInstallerTarballBuildRunID", "[⚠️ 6.0 / 7.0] Specific dotnet-installer-source-build-tarball-build run name", "202XXXXX.Y");
    public static Parameter VerifyBuildSuccess { get; } =
        new BooleanParameter("verifyBuildSuccess", "Verify that associated pipeline runs succeeded", true);

    public static Parameter SkipPackageMirroring { get; } =
        new BooleanParameter("skipPackageMirroring", "Skip package mirroring", false);

    public static Parameter CreateReleaseAnnouncement { get; } =
        new BooleanParameter("createReleaseAnnouncement", "Create release announcement", true);
    public static Parameter AnnouncementGist { get; } =
        new StringParameter("announcementGist", "Release announcement gist URL", " ");

    public static Parameter SubmitReleasePR { get; } =
        new BooleanParameter("submitReleasePR", "Submit release PR", true);

    // Auto means that for dry run, we only create a draft release; full otherwise.
    public static Parameter CreateGitHubRelease { get; } =
        new StringParameter("createGitHubRelease", "[⚠️ 8.0] Create tag & release in dotnet/dotnet", "auto", new[] { "auto", "skip", "draft", "full" });

    public static Parameter IsDryRun { get; } =
        new BooleanParameter("isDryRun", "Dry run", false);
}
