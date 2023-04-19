// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner;
using Sharpliner.AzureDevOps;
using Sharpliner.AzureDevOps.ConditionedExpressions;

namespace SourceBuild.Pipelines;

/// <summary>
/// Hardcoded information about already existing AzDO pipelines that is expected to not change.
/// </summary>
public class Pipelines : VariableTemplateDefinition
{
    public override TargetPathType TargetPathType => TargetPathType.RelativeToGitRoot;
    public override string TargetFile => "eng/templates/variables/pipelines.yml";

    public override ConditionedList<VariableBase> Definition => new()
    {
        Variable("AZDO_PROJECT", "internal"),
        Variable("AZDO_ORG", "https://dev.azure.com/dnceng/"),
        Variable("INSTALLER_OFFICIAL_CI_PIPELINE_ID", 286),
        Variable("INSTALLER_TARBALL_BUILD_CI_PIPELINE_ID", 1011),
        Variable("DOTNET_DOTNET_CI_PIPELINE_ID", 1219),
    };
}
