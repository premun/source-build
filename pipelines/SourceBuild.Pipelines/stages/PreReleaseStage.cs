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
        ReleaseParameters.DotnetStagingPipelineResource,
        ReleaseParameters.DotnetMajorVersion,
        ReleaseParameters.IsPreviewRelease,
        ReleaseParameters.ReleaseName,
        ReleaseParameters.ReleaseBranchName,
        ReleaseParameters.UseSpecificPipelineRunIDs,
        ReleaseParameters.DotnetInstallerOfficialRunID with { Default = string.Empty },
        ReleaseParameters.DotnetInstallerTarballBuildRunID with { Default = string.Empty },
        ReleaseParameters.DotnetDotnetRunID with { Default = string.Empty },
        ReleaseParameters.VerifyBuildSuccess,
        ReleaseParameters.UseCustomTag,
        ReleaseParameters.CustomTag,
        ReleaseParameters.IsDryRun,
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
                        If.Or(Equal(parameters[ReleaseParameters.DotnetMajorVersion.Name], "6.0"), Equal(parameters[ReleaseParameters.DotnetMajorVersion.Name], "7.0"))
                            .Group("DotNet-MSRC-Storage")
                            .Group("DotNet-Source-Build-All-Orgs-Source-Access")
                            .Variable("storageAccountName", "dotnetclimsrc")
                            .Variable("blobContainerName", "source-build")
                            .Variable("vmrUpstreamUrl", "https://dnceng@dev.azure.com/dnceng/internal/_git/security-partners-dotnet")
                            .If.True(parameters[ReleaseParameters.IsDryRun.Name])
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
