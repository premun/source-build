// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner;
using Sharpliner.AzureDevOps;

namespace SourceBuild.Pipelines;

public class SourceBuildRelease : SourceBuildPipelineDefinition
{
    public override TargetPathType TargetPathType => TargetPathType.RelativeToGitRoot;
    public override string TargetFile => "eng/source-build-release-official.yml";

    public override Pipeline Pipeline => new()
    {
        Name = "$(Date:yyyyMMdd)$(Rev:.r)",
        Trigger = Trigger.None,
        Pr = PrTrigger.None,

        Parameters =
        {
            StringParameter("dotnetMajorVersion","Major .NET version being released", allowedValues: new[] { "6.0", "7.0", "8.0" }),
            StringParameter("releaseName", "Release name (e.g. \".NET 8.0 Preview 1\")"),
            StringParameter("releaseBranchName", "Release branch name (e.g. release/8.0.1xx-preview1)"),
            BooleanParameter("isPreviewRelease", "Preview release", false),

            BooleanParameter("useCustomTag", "Use custom tag", false),
            StringParameter("customTag", "Custom release tag (e.g. v6.0.XYY-source-build)", " "),

            BooleanParameter("useSpecificPipelineRunIDs", "Use specific pipeline run IDs", false),
            StringParameter("dotnetDotnetRunID", "[⚠️ 8.0] Specific dotnet-dotnet run name", "202XXXXX.Y"),
            StringParameter("dotnetInstallerOfficialRunID", "[⚠️ 6.0 / 7.0] Specific dotnet-installer-official-ci run name", "202XXXXX.Y"),
            StringParameter("dotnetInstallerTarballBuildRunID", "[⚠️ 6.0 / 7.0] Specific dotnet-installer-source-build-tarball-build run name", "202XXXXX.Y"),
            BooleanParameter("verifyBuildSuccess", "Verify that associated pipeline runs succeeded", true),

            BooleanParameter("skipPackageMirroring", "Skip package mirroring", false),

            BooleanParameter("createReleaseAnnouncement", "Create release announcement", true),
            StringParameter("announcementGist", "Release announcement gist URL", " "),

            BooleanParameter("submitReleasePR", "Submit release PR", true),

            // Auto means that for dry run, we only create a draft release; full otherwise.
            StringParameter("createGitHubRelease", "[⚠️ 8.0] Create tag & release in dotnet/dotnet", "auto", new[] { "auto", "skip", "draft", "full" }),

            BooleanParameter("isDryRun", "Dry run", false),
        },

        Resources = new Resources()
        {
            Pipelines =
            {
                new PipelineResource("dotnet-staging-pipeline-resource")
                {
                    Source = "Stage-DotNet"
                },
            },

            Repositories =
            {
                new RepositoryResource("dotnet-dotnet")
                {
                    Type = RepositoryType.Git,
                    Name = "dotnet-dotnet",
                    Ref = "main",
                }
            },
        },

        Stages =
        {
            StageTemplate("templates/stages/pre-release.yml",
                passThroughParameters: new[]
                {
                    "dotnetMajorVersion",
                    "isPreviewRelease",
                    "releaseBranchName",
                    "releaseName",
                    "useSpecificPipelineRunIDs",
                    "dotnetDotnetRunID",
                    "dotnetInstallerOfficialRunID",
                    "dotnetInstallerTarballBuildRunID",
                    "verifyBuildSuccess",
                    "useCustomTag",
                    "isDryRun",
                },
                otherParameters: new()
                {
                    { "dotnetStagingPipelineResource", "dotnet-staging-pipeline-resource" },
                    { "customTag", "${{ replace(parameters.customTag, ' ', '') }}" },
                }),

            ApprovalStage(
                name: "MirrorApproval",
                environment: "Source Build Release - Mirror",
                dependsOn: new[] { "PreRelease" },
                hint: "Ready for dotnet-security-partners mirroring"),

            StageTemplate("templates/stages/mirror.yml",
                passThroughParameters: new[]
                {
                    "dotnetMajorVersion",
                    "isPreviewRelease",
                    "releaseBranchName",
                    "useCustomTag",
                    "skipPackageMirroring",
                    "isDryRun",
                },
                otherParameters: new()
                {
                    { "dotnetStagingPipelineResource", "dotnet-staging-pipeline-resource" },
                }),

            ApprovalStage(
                name: "NotificationApproval",
                environment: "Approval - Partner notification",
                dependsOn: new[] { "Mirror" },
                hint: "Confirm partner notification sent"),

            ApprovalStage(
                name: "ReleaseApproval",
                environment: "Source Build Release - Release",
                dependsOn: new[] { "NotificationApproval" },
                hint: "Confirm Microsoft build released"),

            StageTemplate("templates/stages/release.yml",
                passThroughParameters: new[]
                {
                    "dotnetMajorVersion",
                    "isPreviewRelease",
                    "releaseBranchName",
                    "releaseName",
                    "createReleaseAnnouncement",
                    "createGitHubRelease",
                    "submitReleasePR",
                    "isDryRun",
                },
                otherParameters: new()
                {
                    { "dotnetStagingPipelineResource", "dotnet-staging-pipeline-resource" },
                    { "announcementGist", "${{ replace(parameters.announcementGist, ' ', '') }}" },
                }),
        }
    };
}
