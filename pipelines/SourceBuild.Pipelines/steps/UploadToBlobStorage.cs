// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Sharpliner;
using Sharpliner.AzureDevOps;
using Sharpliner.AzureDevOps.ConditionedExpressions;

namespace SourceBuild.Pipelines;

public class UploadToBlobStorage : StepTemplateDefinition
{
    public override TargetPathType TargetPathType => TargetPathType.RelativeToGitRoot;
    public override string TargetFile => "eng/templates/steps/upload-to-blob-storage.yml";

    public override List<Parameter> Parameters => new()
    {
        StringParameter("file"),
        StringParameter("accountName"),
        StringParameter("containerName"),
        StringParameter("uploadPath"),
        StringParameter("azureStorageKey"),
    };

    public override ConditionedList<Step> Definition => new()
    {
        Script.Inline(
            """
            set -euxo pipefail
            az config set extension.use_dynamic_install=yes_without_prompt

            filename="$(basename ${{ parameters.file }})"
            full_filepath="$(realpath ${{ parameters.file }})"

            # Check if the file is on disk
            if [ ! -f "${full_filepath}" ]; then
              echo "##vso[task.logissue type=error]File ${full_filepath} not found on disk. It might not have been downloaded. Exiting..."
            fi

            # Check if the file already exists in blob storage
            file_blob_list=$(az storage blob list --account-name "${{ parameters.accountName }}" --container-name "${{ parameters.containerName }}" --prefix "${{ parameters.uploadPath }}/${filename}")
            number_of_blobs=$(echo $file_blob_list | jq -r 'length')
            if [ $number_of_blobs -gt 0 ]; then
              echo "##vso[task.logissue type=warning]There is already a blob named ${filename} found in blob storage. Skipping upload..."
              echo "##vso[task.complete result=SucceededWithIssues;]DONE"
              exit 0
            fi

            az storage blob upload --account-name "${{ parameters.accountName }}" --container-name "${{ parameters.containerName }}" --file "${full_filepath}" --name "${{ parameters.uploadPath }}/${filename}"

            # Check if the uploaded file is reachable in the blob storage account
            file_blob_list=$(az storage blob list --account-name "${{ parameters.accountName }}" --container-name "${{ parameters.containerName }}" --prefix "${{ parameters.uploadPath }}/${filename}")
            number_of_blobs=$(echo $file_blob_list | jq -r 'length')
            if [ $number_of_blobs -eq 0 ]; then
              echo "##vso[task.logissue type=error]File ${filename} not found in blob storage container after upload. It might not have been downloaded earlier, or might not have been uploaded to ${{ parameters.containerName }}. Exiting..."
              exit 1
            elif [ $number_of_blobs -gt 1 ]; then
              echo "##vso[task.logissue type=error]More than one blob named ${filename} found in blob storage. Exiting..."
              exit 1
            fi
            """) with
        {
            DisplayName = "Upload to blob storage",
            Env = new()
            {
                { "AZURE_STORAGE_KEY", parameters["azureStorageKey"] }
            }
        }
    };
}
