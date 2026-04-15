# Web API Template

A `dotnet new` project template for an ASP.NET Core Web API.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

## Install the template

From the root of this repository (the folder containing `.template.config`), run:

```bash
dotnet new install .\
```

Verify the template is registered:

```bash
dotnet new list
```

You should see `Web API Template` with short name `api-template` in the list.

## Create a new project

```bash
dotnet new api-template -n MyProject.API
```

This produces a new solution and project with all `ProjectName` occurrences replaced by `MyProject`:

```
MyProject.API/
├── MyProject.API/
│   ├── MyProject.API.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Properties/
│       └── launchSettings.json
└── MyProject API.slnx
```

## Uninstall the template

From the root of this repository, run:

```bash
dotnet new uninstall .\
```
