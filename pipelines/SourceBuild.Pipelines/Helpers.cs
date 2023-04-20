// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner.AzureDevOps;

namespace SourceBuild.Pipelines;

public static class Helpers
{
    public static TemplateParameters PassThroughParameters(this TemplateParameters templateParameters, Parameter[] passThroughParameters)
    {
        var newParameters = new TemplateParameters();

        foreach (var parameter in passThroughParameters)
        {
            newParameters.Add(parameter.Name, "${{ parameters." + parameter.Name + " }}");
        }

        foreach (var parameter in templateParameters)
        {
            newParameters.Add(parameter.Key, parameter.Value);
        }

        return newParameters;
    }

    public static string RemoveSpace(this Parameter parameter) => "${{ replace(parameters." + parameter.Name + ", ' ', '') }}";
}
