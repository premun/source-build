﻿// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner;
using Sharpliner.AzureDevOps;
using Sharpliner.AzureDevOps.ConditionedExpressions;

namespace SourceBuild.Pipelines;

public class InitializeReleaseInfo : StepTemplateDefinition
{
    public override TargetPathType TargetPathType => TargetPathType.RelativeToGitRoot;
    public override string TargetFile => "eng/templates/steps/initialize-release-info.yml";

    public override List<Parameter> Parameters => new()
    {
        PipelineParameters.DotnetStagingPipelineResource,
        PipelineParameters.DotnetMajorVersion,
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

    public override ConditionedList<Step> Definition => new()
    {
        Download.FromPipelineResource(
            parameters[PipelineParameters.DotnetStagingPipelineResource.Name],
            "manifests",
            new[] { "manifest.json" }) with
        {
            DisplayName = "Download release manifest"
        },

        Download.FromPipelineResource(
            parameters[PipelineParameters.DotnetStagingPipelineResource.Name],
            "drop",
            new[] { "config.json" }) with
        {
            DisplayName = "Download release config"
        },

        Script.Inline(
            """
            set -euo pipefail

            manifest_path=$(PIPELINE.WORKSPACE)/dotnet-staging-pipeline-resource/manifests/manifest.json
            config_path=$(PIPELINE.WORKSPACE)/dotnet-staging-pipeline-resource/drop/config.json

            runtime_version="$(jq -r '.Runtime' "$config_path")"
            release_channel="$(jq -r '.Channel' "$config_path")"
            release="$(jq -r '.Release' "$config_path")"

            # Source-build only supports the lowest available feature band.
            # Sort the SDK releases by number and pick the lowest value.
            # We also need to remove the preview label from the version
            sdk_version="$(jq -r '.Sdks | sort_by(. | split(".") | map(sub("-[a-z0-9]+"; "")) | map(tonumber)) | .[0]' $config_path)"

            branch_name="${{ parameters.releaseBranchName }}"
            if [ -z "$branch_name" ]; then
              # For non-preview releases, the branch name can be determined from the SDK version
              # Replace the last two characters in sdk_version with xx
              branch_version=$(echo $sdk_version | sed 's/..$/xx/')
              branch_name="refs/heads/internal/release/${branch_version}"
            fi

            if [[ ! "$branch_name" =~ ^refs/heads/ ]]; then
              branch_name="refs/heads/$branch_name"
            fi

            commit=$(jq -r --arg BRANCH "$branch_name" '.builds[] | select(.repo == "https://dev.azure.com/dnceng/internal/_git/dotnet-installer" or .repo == "https://github.com/dotnet/installer") | select(.branch == $BRANCH) | .commit' $manifest_path)

            if [[ ! $commit ]]; then
              echo "##vso[task.logissue type=error]Installer build on a commit for branch $branch_name not found. Exiting..."
              exit 1
            fi

            if [ "${{ parameters.useCustomTag }}" = "True" ] ; then
              tag="${{ parameters.customTag }}"
              echo "Using custom tag $tag"
            else
              if [[ "${{ parameters.dotnetMajorVersion }}" == '6.0' || "${{ parameters.dotnetMajorVersion }}" == '7.0' ]]; then
                tag="v$sdk_version"
              else
                tag="v$runtime_version"
              fi
            fi

            echo "Release: $release"
            echo "Release channel: $release_channel"
            echo "Runtime version: $runtime_version"
            echo "Release tag: $tag"
            echo "SDK version: $sdk_version"
            echo "Installer commit: $commit"

            echo "##vso[task.setvariable variable=Release;isOutput=true]$release"
            echo "##vso[task.setvariable variable=SdkVersion;isOutput=true]$sdk_version"
            echo "##vso[task.setvariable variable=RuntimeVersion;isOutput=true]$runtime_version"
            echo "##vso[task.setvariable variable=ReleaseChannel;isOutput=true]$release_channel"
            echo "##vso[task.setvariable variable=ReleaseTag;isOutput=true]$tag"
            echo "##vso[task.setvariable variable=InstallerCommit;isOutput=true]$commit"

            build_name="$(resources.pipeline.dotnet-staging-pipeline-resource.runName)"
            if [ ${{ parameters.isDryRun }} = True ]; then
              build_name="dryrun-${build_name}"
            fi

            original_build_name='$(Build.BuildNumber)'
            revision="${original_build_name:9}"

            # Tag & name the release build
            echo "##vso[build.updatebuildnumber]$build_name-$revision"
            echo "##vso[build.addbuildtag]$release"
            if [ '${{ parameters.isDryRun }}' = True ]; then
              echo "##vso[build.addbuildtag]dry-run"
            fi
            """) with
        {
            Name = "ReadReleaseInfo",
            DisplayName = "Read release info",
        },

        Script.Inline(
            """
            set -euo pipefail
            source eng/get-build-info.sh

            if [[ "$(ReadReleaseInfo.ReleaseChannel)" == "6.0" || "$(ReadReleaseInfo.ReleaseChannel)" == "7.0" ]]; then
              if [[ "${{ parameters.useSpecificPipelineRunIDs }}" == "True" ]]; then
                search_by=name
                query1="${{ parameters.dotnetInstallerOfficialRunID }}"
                query2="${{ parameters.dotnetInstallerTarballBuildRunID }}"
                echo "Searching for associated builds $query1 and $query2"
              else
                search_by=sourceVersion
                query1="$(ReadReleaseInfo.InstallerCommit)"
                query2="$(ReadReleaseInfo.InstallerCommit)"
              fi

              get_build_info \
                "$(AZDO_ORG)" \
                "$(AZDO_PROJECT)" \
                "$(INSTALLER_OFFICIAL_CI_PIPELINE_ID)" \
                dotnet-installer-official-ci \
                InstallerOfficialRunId \
                InstallerCommit \
                "${{ parameters.verifyBuildSuccess }}" \
                $search_by \
                "$query1"

              get_build_info \
                "$(AZDO_ORG)" \
                "$(AZDO_PROJECT)" \
                "$(INSTALLER_TARBALL_BUILD_CI_PIPELINE_ID)" \
                dotnet-installer-source-build-tarball-build \
                InstallerTarballBuildRunId \
                InstallerCommit \
                "${{ parameters.verifyBuildSuccess }}" \
                $search_by \
                "$query2"
            else
              if [[ "${{ parameters.useSpecificPipelineRunIDs }}" == "True" ]]; then
                search_by=name
                query="${{ parameters.dotnetDotnetRunID }}"
                echo "Searching for associated build $query"
              else
                search_by=tag
                query="installer-$(ReadReleaseInfo.InstallerCommit)"
              fi

              get_build_info \
                "$(AZDO_ORG)" \
                "$(AZDO_PROJECT)" \
                "$(DOTNET_DOTNET_CI_PIPELINE_ID)" \
                dotnet-dotnet \
                DotnetDotnetRunId \
                DotnetDotnetCommit \
                "${{ parameters.verifyBuildSuccess }}" \
                $search_by \
                "$query"
            fi
            """) with
        {
            Name = "AssociatedPipelineRuns",
            DisplayName = "Get associated pipeline runs",
            Env = new()
            {
                { "AZURE_DEVOPS_EXT_PAT", "$(System.AccessToken)" }
            }
        }
    };
}
