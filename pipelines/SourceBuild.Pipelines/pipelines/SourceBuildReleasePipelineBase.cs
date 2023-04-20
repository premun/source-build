// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner;
using Sharpliner.AzureDevOps;

namespace SourceBuild.Pipelines;

public abstract class SourceBuildReleasePipelineBase : PipelineDefinition
{
    public override TargetPathType TargetPathType => TargetPathType.RelativeToGitRoot;

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
                }
            },

            Repositories =
            {
                new RepositoryResource("dotnet-dotnet")
                {
                    Type = RepositoryType.Git,
                    Name = "dotnet-dotnet",
                    Ref = "main",
                }
            }
        },

        Parameters =
        {
            ReleaseParameters.DotnetMajorVersion,
            ReleaseParameters.ReleaseName,
            ReleaseParameters.ReleaseBranchName,
            ReleaseParameters.IsPreviewRelease with { Default = false },

            ReleaseParameters.UseCustomTag with { Default = false },
            ReleaseParameters.CustomTag  with { Default = " " },

            ReleaseParameters.UseSpecificPipelineRunIDs with { Default = false },
            ReleaseParameters.DotnetDotnetRunID with { Default = "202XXXXX.Y" },
            ReleaseParameters.DotnetInstallerOfficialRunID with { Default = "202XXXXX.Y" },
            ReleaseParameters.DotnetInstallerTarballBuildRunID with { Default = "202XXXXX.Y" },
            ReleaseParameters.VerifyBuildSuccess with { Default = true },

            ReleaseParameters.CreateReleaseAnnouncement with { Default = true },
            ReleaseParameters.AnnouncementGist with { Default = " " },

            ReleaseParameters.SkipPackageMirroring with { Default = false },
            ReleaseParameters.SubmitReleasePR with { Default = true },
            ReleaseParameters.CreateGitHubRelease with { Default = "auto" },
        },

        Stages =
        {
            StageTemplate("templates/stages/pre-release.yml",
                new TemplateParameters()
                {
                    { ReleaseParameters.DotnetStagingPipelineResource.Name, ReleaseParameters.StagingPipelineName },
                    { ReleaseParameters.CustomTag.Name, Helpers.RemoveSpace(ReleaseParameters.CustomTag) },
                    { ReleaseParameters.IsDryRun.Name, _isTestPipeline }
                }
                .PassThroughParameters(new Parameter[]
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
                })),

            ApprovalStage(
                name: "MirrorApproval",
                environment: "Source Build Release - Mirror",
                dependsOn: new[] { "PreRelease" },
                hint: "Ready for dotnet-security-partners mirroring"),

            StageTemplate("templates/stages/mirror.yml",
                new TemplateParameters()
                {
                    { ReleaseParameters.DotnetStagingPipelineResource.Name, ReleaseParameters.StagingPipelineName },
                    { ReleaseParameters.IsDryRun.Name, _isTestPipeline },
                }
                .PassThroughParameters(new Parameter[]
                {
                    ReleaseParameters.DotnetMajorVersion,
                    ReleaseParameters.IsPreviewRelease,
                    ReleaseParameters.ReleaseBranchName,
                    ReleaseParameters.UseCustomTag,
                    ReleaseParameters.SkipPackageMirroring,
                })),

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
                new TemplateParameters()
                {
                    { ReleaseParameters.DotnetStagingPipelineResource.Name, ReleaseParameters.StagingPipelineName },
                    { ReleaseParameters.AnnouncementGist.Name, Helpers.RemoveSpace(ReleaseParameters.AnnouncementGist) },
                    { ReleaseParameters.IsDryRun.Name, _isTestPipeline },
                }
                .PassThroughParameters(new Parameter[]
                {
                    ReleaseParameters.DotnetMajorVersion,
                    ReleaseParameters.IsPreviewRelease,
                    ReleaseParameters.ReleaseBranchName,
                    ReleaseParameters.ReleaseName,
                    ReleaseParameters.CreateReleaseAnnouncement,
                    ReleaseParameters.CreateGitHubRelease,
                    ReleaseParameters.SubmitReleasePR,
                })),
        }
    };

    #region Helper methods

    private readonly bool _isTestPipeline;

    protected SourceBuildReleasePipelineBase(bool isTestPipeline)
    {
        _isTestPipeline = isTestPipeline;
    }

    protected Stage ApprovalStage(string name, string environment, string[] dependsOn, string hint) =>
        new(name, hint)
        {
            DependsOn = dependsOn,
            Jobs =
            {
                new DeploymentJob(name, hint)
                {
                    // The PR environment doesn't need an actual approval
                    Environment = _isTestPipeline ? new Sharpliner.AzureDevOps.Environment("pr") : new Sharpliner.AzureDevOps.Environment(environment),
                    Pool = new ServerPool(),
                }
            }
        };

    #endregion
}
