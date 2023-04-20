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
                            .If.Equal(parameters[ReleaseParameters.IsDryRun.Name])
                                    .Variable("blobContainerUploadBaseFilePath", "Dev")
                                .Else
                                    .Variable("blobContainerUploadBaseFilePath", "release")
                    },

                    Steps =
                    {
                        StepTemplate("../steps/initialize-release-info.yml",
                            new Parameter[]
                            {
                                ReleaseParameters.DotnetMajorVersion,
                                ReleaseParameters.DotnetStagingPipelineResource,
                                ReleaseParameters.ReleaseBranchName,
                                ReleaseParameters.UseSpecificPipelineRunIDs,
                                ReleaseParameters.DotnetDotnetRunID,
                                ReleaseParameters.DotnetInstallerOfficialRunID,
                                ReleaseParameters.DotnetInstallerTarballBuildRunID,
                                ReleaseParameters.VerifyBuildSuccess,
                                ReleaseParameters.UseCustomTag,
                                ReleaseParameters.CustomTag,
                                ReleaseParameters.IsDryRun,
                            }.AddParameters(new())),

                        Script.Inline(
                            """
                            prerelease=''
                            if [ ${{ parameters.isPreviewRelease }} = True ]; then
                              prerelease='--prerelease'
                            fi

                            "$(Build.SourcesDirectory)/eng/create-announcement-draft.sh" \
                              --template "$(Build.SourcesDirectory)/eng/source-build-release-announcement.md" \
                              --release-name '${{ parameters.releaseName }}'             \
                              --channel "$(ReadReleaseInfo.ReleaseChannel)"              \
                              $prerelease                                                \
                              --release "$(ReadReleaseInfo.Release)"                     \
                              --sdk-version "$(ReadReleaseInfo.SdkVersion)"              \
                              --runtime-version "$(ReadReleaseInfo.RuntimeVersion)"      \
                              --tag "$(ReadReleaseInfo.ReleaseTag)"
                            """) with
                        {
                            DisplayName = "Create announcement draft"
                        },

                        If.Or(Equal(parameters[ReleaseParameters.DotnetMajorVersion.Name], "6.0"), Equal(parameters[ReleaseParameters.DotnetMajorVersion.Name], "7.0"))
                            .Step(Download.SpecificBuild(
                                "$(AZDO_PROJECT)",
                                0, // TODO "$(INSTALLER_OFFICIAL_CI_PIPELINE_ID)",
                                0, // TODO "$(AssociatedPipelineRuns.InstallerOfficialRunId)",
                                "BlobArtifacts",
                                patterns: new[] { "BlobArtifacts/dotnet-sdk-source-$(ReadReleaseInfo.SdkVersion).tar.gz" }) with
                                {
                                    DisplayName = "Download Source Tarball"
                                })

                            .StepTemplate("../steps/upload-to-blob-storage.yml", new()
                            {
                                { "file", "$(PIPELINE.WORKSPACE)/dotnet-sdk-source-$(ReadReleaseInfo.SdkVersion).tar.gz" },
                                { "accountName", "$(storageAccountName)" },
                                { "containerName", "$(blobContainerName)" },
                                { "uploadPath", "$(blobContainerUploadBaseFilePath)/$(ReadReleaseInfo.ReleaseChannel)/$(ReadReleaseInfo.RuntimeVersion)-$(ReadReleaseInfo.SdkVersion)" },
                                { "azureStorageKey", "$(dotnetclimsrc-access-key)" },
                            })

                            .Step(Script.Inline(
                                """
                                set -euo pipefail

                                upstream_with_pat=$(echo $(vmrUpstreamUrl) | sed "s,https://.*@,https://dn-bot:${AZDO_PAT}@,g")

                                args=()
                                args+=(--releaseChannel $(ReadReleaseInfo.ReleaseChannel))
                                args+=(--sdkVersion $(ReadReleaseInfo.SdkVersion))
                                args+=(--upstream ${upstream_with_pat})
                                args+=(--tarball $(Pipeline.Workspace)/dotnet-sdk-source-$(ReadReleaseInfo.SdkVersion).tar.gz)

                                if [ '${{ parameters.isDryRun }}' = True ]; then
                                  args+=(--isDryRun)
                                fi

                                $(Build.SourcesDirectory)/eng/push-tarball.sh "${args[@]}"
                                """) with
                                {
                                    DisplayName = "Update security-partners-dotnet",
                                    Env = new() { { "AZDO_PAT", "$(dn-bot-all-orgs-build-rw-code-rw)" } }
                                })
                    }
                }
            }
        }
    };
}
