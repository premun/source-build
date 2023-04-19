// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner.AzureDevOps;

namespace SourceBuild.Pipelines;

public abstract class SourceBuildPipelineDefinition : PipelineDefinition
{
    protected static Stage ApprovalStage(string name, string environment, string[] dependsOn, string hint) =>
        new(name, hint)
        {
            DependsOn = dependsOn,
            Jobs =
            {
                new DeploymentJob(name, hint)
                {
                    Environment = new Sharpliner.AzureDevOps.Environment(environment),
                    Pool = new ServerPool(),
                }
            }
        };

    protected static Template<Stage> StageTemplate(string path, string[] passThroughParameters, TemplateParameters otherParameters)
    {
        var jointParameters = new TemplateParameters();

        foreach (var parameter in passThroughParameters)
        {
            jointParameters.Add(parameter, parameters[parameter]);
        }

        foreach (var parameter in otherParameters)
        {
            jointParameters.Add(parameter.Key, parameter.Value);
        }

        return StageTemplate(path, jointParameters);
    }
}
