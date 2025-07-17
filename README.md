New repository for remaking the 7DTD (7 days to die) legit mod.
It is made for educational purposes only and cannot be used in multiplayer.

## Build
To compile without optional UnityExplorer support use the `Release` configuration:

```
dotnet build -c Release
```

The `DEBUG` and `RELEASE_UE` configurations require the `UnityExplorer.STANDALONE.Mono.dll`
library to be present in the path referenced by `SevenDTDMono.csproj`.
