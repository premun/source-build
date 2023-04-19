// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner;
using Sharpliner.AzureDevOps;
using Sharpliner.AzureDevOps.ConditionedExpressions;

namespace SourceBuild.Pipelines;

public class PreReleaseStage : StageTemplateDefinition
{
    public override TargetPathType TargetPathType => TargetPathType.RelativeToGitRoot;
    public override string TargetFile => "eng/templates/stages/pre-release.yml";

    public override List<Parameter> Parameters => new()
    {
        PipelineParameters.DotnetStagingPipelineResource,
        PipelineParameters.DotnetMajorVersion,
        PipelineParameters.IsPreviewRelease,
        PipelineParameters.ReleaseName,
        PipelineParameters.ReleaseBranchName,
        PipelineParameters.UseSpecificPipelineRunIDs,
        PipelineParameters.DotnetInstallerOfficialRunID with { Default = string.Empty },
        PipelineParameters.DotnetInstallerTarballBuildRunID with { Default = string.Empty },
        PipelineParameters.DotnetDotnetRunID with { Default = string.Empty },
        PipelineParameters.VerifyBuildSuccess,
        PipelineParameters.UseCustomTag,
        PipelineParameters.CustomTag,
        PipelineParameters.IsDryRun,
    };

    public override ConditionedList<Stage> Definition => new()
    {
        new Stage("PreRelease", "Pre-Release")
        {
            Jobs =
            {
                new Job("PreRelease", "Initialize release info")
                {
                    Variables =
                    {
                        VariableTemplate("../variables/pipelines.yml"),
                        If.Or(Equal(parameters[PipelineParameters.DotnetMajorVersion.Name], "6.0"), Equal(parameters[PipelineParameters.DotnetMajorVersion.Name], "7.0"))
                            .Group("DotNet-MSRC-Storage")
                            .Group("DotNet-Source-Build-All-Orgs-Source-Access")
                            .Variable("storageAccountName", "dotnetclimsrc")
                            .Variable("blobContainerName", "source-build")
                            .Variable("vmrUpstreamUrl", "https://dnceng@dev.azure.com/dnceng/internal/_git/security-partners-dotnet")
                            .If.True(parameters[PipelineParameters.IsDryRun.Name])
                                    .Variable("blobContainerUploadBaseFilePath", "Dev")
                                .Else
                                    .Variable("blobContainerUploadBaseFilePath", "release")
                    },

                    Steps =
                    {
                        StepTemplate("../steps/initialize-release-info.yml",
                            passThroughParameters: new Parameter[]
                            {
                            },
                            otherParameters: new()
                            {
                            }),
                    }
                }
            }
        }
    };
}
