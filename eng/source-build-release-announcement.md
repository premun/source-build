<!-- This file is a template for a GitHub Discussion post. -->
<!-- The line prefixed by 'Title:' will be submitted as the title of the discussion, and the rest of the file will be submitted as the body. -->
Title: $RELEASE_NAME $RELEASE_DATE Update - .NET $RUNTIME_VERSION and SDK $SDK_VERSION

Please use the [$TAG tag]($TAG_URL) to source-build .NET version $RUNTIME_VERSION / $SDK_VERSION.

- Release Notes: $RELEASE_NOTES_URL
- Tag URL: $TAG_URL

@dotnet/distro-maintainers

```json
{
  "Channel": "$RELEASE_CHANNEL",
  "Runtime": "$RUNTIME_VERSION",
  "Sdk": "$SDK_VERSION",
  "TagUrl": "$TAG_URL"
}
```
