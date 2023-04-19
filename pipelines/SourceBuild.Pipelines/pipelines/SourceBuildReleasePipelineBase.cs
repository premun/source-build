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
                new PipelineResource(PipelineParameters.StagingPipelineName)
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
            PipelineParameters.DotnetMajorVersion,
            PipelineParameters.ReleaseName,
            PipelineParameters.ReleaseBranchName,
            PipelineParameters.IsPreviewRelease with { Default = false },

            PipelineParameters.UseCustomTag with { Default = false },
            PipelineParameters.CustomTag  with { Default = " " },

            PipelineParameters.UseSpecificPipelineRunIDs with { Default = false },
            PipelineParameters.DotnetDotnetRunID with { Default = "202XXXXX.Y" },
            PipelineParameters.DotnetInstallerOfficialRunID with { Default = "202XXXXX.Y" },
            PipelineParameters.DotnetInstallerTarballBuildRunID with { Default = "202XXXXX.Y" },
            PipelineParameters.VerifyBuildSuccess with { Default = true },

            PipelineParameters.CreateReleaseAnnouncement with { Default = true },
            PipelineParameters.AnnouncementGist with { Default = " " },

            PipelineParameters.SkipPackageMirroring with { Default = false },
            PipelineParameters.SubmitReleasePR with { Default = true },
            PipelineParameters.CreateGitHubRelease with { Default = "auto" },
        },

        Stages =
        {
            StageTemplate("templates/stages/pre-release.yml",
                passThroughParameters: new Parameter[]
                {
                    PipelineParameters.DotnetMajorVersion,
                    PipelineParameters.IsPreviewRelease,
                    PipelineParameters.ReleaseBranchName,
                    PipelineParameters.ReleaseName,
                    PipelineParameters.UseSpecificPipelineRunIDs,
                    PipelineParameters.DotnetDotnetRunID,
                    PipelineParameters.DotnetInstallerOfficialRunID,
                    PipelineParameters.DotnetInstallerTarballBuildRunID,
                    PipelineParameters.VerifyBuildSuccess,
                    PipelineParameters.UseCustomTag,
                },
                otherParameters: new()
                {
                    { PipelineParameters.DotnetStagingPipelineResource.Name, PipelineParameters.StagingPipelineName },
                    { PipelineParameters.CustomTag.Name, "${{ replace(parameters.customTag, ' ', '') }}" },
                    { PipelineParameters.IsDryRun.Name, _isTestPipeline },
                }),

            ApprovalStage(
                name: "MirrorApproval",
                environment: "Source Build Release - Mirror",
                dependsOn: new[] { "PreRelease" },
                hint: "Ready for dotnet-security-partners mirroring"),

            StageTemplate("templates/stages/mirror.yml",
                passThroughParameters: new Parameter[]
                {
                    PipelineParameters.DotnetMajorVersion,
                    PipelineParameters.IsPreviewRelease,
                    PipelineParameters.ReleaseBranchName,
                    PipelineParameters.UseCustomTag,
                    PipelineParameters.SkipPackageMirroring,
                },
                otherParameters: new()
                {
                    { PipelineParameters.DotnetStagingPipelineResource.Name, PipelineParameters.StagingPipelineName },
                    { PipelineParameters.IsDryRun.Name, _isTestPipeline },
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
                passThroughParameters: new Parameter[]
                {
                    PipelineParameters.DotnetMajorVersion,
                    PipelineParameters.IsPreviewRelease,
                    PipelineParameters.ReleaseBranchName,
                    PipelineParameters.ReleaseName,
                    PipelineParameters.CreateReleaseAnnouncement,
                    PipelineParameters.CreateGitHubRelease,
                    PipelineParameters.SubmitReleasePR,
                },
                otherParameters: new()
                {
                    { PipelineParameters.DotnetStagingPipelineResource.Name, PipelineParameters.StagingPipelineName },
                    { PipelineParameters.AnnouncementGist.Name, "${{ replace(parameters.announcementGist, ' ', '') }}" },
                    { PipelineParameters.IsDryRun.Name, _isTestPipeline },
                }),
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

    protected static Template<Stage> StageTemplate(string path, Parameter[] passThroughParameters, TemplateParameters otherParameters)
    {
        var jointParameters = new TemplateParameters();

        foreach (var parameter in passThroughParameters)
        {
            jointParameters.Add(parameter.Name, parameters[parameter.Name]);
        }

        foreach (var parameter in otherParameters)
        {
            jointParameters.Add(parameter.Key, parameter.Value);
        }

        return StageTemplate(path, jointParameters);
    }

    #endregion
}
