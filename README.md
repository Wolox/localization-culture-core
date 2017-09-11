Localization Culture NetCore
============================

## Description

This project allows localization for NetCore using .json files instead of .resx.


## Configuration in your project

1. Add the package reference
```bash
    dotnet add package LocalizationCultureCore --version x.x.x
```

2. On Startup.cs
```bash
    services.AddJsonLocalization(options => options.ResourcesPath = "Resources");
```

3. Create a folder named "Resources" in the project's root directory. In this folder will be the resource json files: 'en-US.json', 'en.json', etc.


## Example 

To configure Localization Culture NetCore you must to add these lines in the ConfigureServices method on Startup.cs

```bash
    services.AddJsonLocalization(options => options.ResourcesPath = "Resources");
    services.AddMvc().AddViewLocalization();
```

The last line allows to use the localizer in the views.
To configure a default culture, you can do the following:

```bash
    CultureInfo.CurrentCulture = new CultureInfo("en-US");
```

You can see a full example [here](https://gist.github.com/gzamudio/4db43b93ea73d49e062654fd124dab26)


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


## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Push to the branch (`git push origin my-new-feature`)
5. Create new Pull Request

This project is based on [AspNet5Localization](https://github.com/rwwilden/AspNet5Localization/tree/develop)
