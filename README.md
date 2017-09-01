Localization Culture NetCore
============================

## Steps to create and upload a NuGet Package

1. Build the application on release mode.

```bash
    dotnet build -c release
```

2. Pack the application.
```bash
    dotnet pack /p:PackageVersion=x.x.x -c release.
```

3. The NuGet Package is now in /bin/release/LocalizationCultureCore.x.x.x.nupkg.

4. Login at [NuGet](http://www.nuget.org) and upload the .nupkg file in [Upload](https://www.nuget.org/packages/manage/upload) section.

## Configuration in your project

1. Add the package reference
```bash
    dotnet add package LocalizationCultureCore --version x.x.x
```

2. On Startup.cs
```bash
    services.AddJsonLocalization(options => options.ResourcesPath = "Resources");
```

3. Create a folder named "Resources" in project's root directory. In this folder will be the resource json files: 'en-US.json', 'en.json', etc.
