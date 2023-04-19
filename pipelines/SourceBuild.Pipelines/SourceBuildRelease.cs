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
        AppendCommitMessageToRunName = false,
        Trigger = Trigger.None,
        Pr = PrTrigger.None,
        Pool = new HostedPool()
        {
            Name = "NetCore1ESPool-Svc-Internal",
            Demands = new[] { "ImageOverride -equals 1es-ubuntu-2004" },
        },
        Resources = new Resources()
        {
            Pipelines =
            {
                new PipelineResource(ReleaseParameters.StagingPipelineName)
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

        Parameters =
        {
            ReleaseParameters.DotnetMajorVersion,
            ReleaseParameters.ReleaseName,
            ReleaseParameters.ReleaseBranchName,
            ReleaseParameters.IsPreviewRelease,

            ReleaseParameters.UseCustomTag,
            ReleaseParameters.CustomTag,

            ReleaseParameters.UseSpecificPipelineRunIDs,
            ReleaseParameters.DotnetDotnetRunID,
            ReleaseParameters.DotnetInstallerOfficialRunID,
            ReleaseParameters.DotnetInstallerTarballBuildRunID,
            ReleaseParameters.VerifyBuildSuccess,

            ReleaseParameters.CreateReleaseAnnouncement,
            ReleaseParameters.AnnouncementGist,

            ReleaseParameters.SubmitReleasePR,
            ReleaseParameters.CreateGitHubRelease,
            ReleaseParameters.SkipPackageMirroring,

            ReleaseParameters.IsDryRun,
        },

        Stages =
        {
            StageTemplate("templates/stages/pre-release.yml",
                passThroughParameters: new[]
                {
                    ReleaseParameters.DotnetMajorVersion,
                    ReleaseParameters.IsPreviewRelease,
                    ReleaseParameters.ReleaseBranchName,
                    ReleaseParameters.ReleaseName,
                    ReleaseParameters.UseSpecificPipelineRunIDs,
                    ReleaseParameters.DotnetDotnetRunID,
                    ReleaseParameters.DotnetInstallerOfficialRunID,
                    ReleaseParameters.DotnetInstallerTarballBuildRunID,
                    ReleaseParameters.VerifyBuildSuccess,
                    ReleaseParameters.UseCustomTag,
                    ReleaseParameters.IsDryRun,
                },
                otherParameters: new()
                {
                    { ReleaseParameters.StagingPipelineResource, ReleaseParameters.StagingPipelineName },
                    { ReleaseParameters.CustomTag.Name, "${{ replace(parameters.customTag, ' ', '') }}" },
                }),

            ApprovalStage(
                name: "MirrorApproval",
                environment: "Source Build Release - Mirror",
                dependsOn: new[] { "PreRelease" },
                hint: "Ready for dotnet-security-partners mirroring"),

            StageTemplate("templates/stages/mirror.yml",
                passThroughParameters: new[]
                {
                    ReleaseParameters.DotnetMajorVersion,
                    ReleaseParameters.IsPreviewRelease,
                    ReleaseParameters.ReleaseBranchName,
                    ReleaseParameters.UseCustomTag,
                    ReleaseParameters.SkipPackageMirroring,
                    ReleaseParameters.IsDryRun,
                },
                otherParameters: new()
                {
                    { ReleaseParameters.StagingPipelineResource, ReleaseParameters.StagingPipelineName },
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
                    ReleaseParameters.DotnetMajorVersion,
                    ReleaseParameters.IsPreviewRelease,
                    ReleaseParameters.ReleaseBranchName,
                    ReleaseParameters.ReleaseName,
                    ReleaseParameters.CreateReleaseAnnouncement,
                    ReleaseParameters.CreateGitHubRelease,
                    ReleaseParameters.SubmitReleasePR,
                    ReleaseParameters.IsDryRun,
                },
                otherParameters: new()
                {
                    { ReleaseParameters.StagingPipelineResource, ReleaseParameters.StagingPipelineName },
                    { ReleaseParameters.AnnouncementGist.Name, "${{ replace(parameters.announcementGist, ' ', '') }}" },
                }),
        }
    };
}
