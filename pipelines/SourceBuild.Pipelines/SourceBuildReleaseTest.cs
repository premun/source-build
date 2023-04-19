// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SourceBuild.Pipelines;

public class SourceBuildReleaseTest : SourceBuildReleasePipeline
{
    public override string TargetFile => "eng/source-build-release-test.yml";

    public SourceBuildReleaseTest() : base(isTestPipeline: true)
    {
    }
}
