# Installation

This guide covers how to install and set up the AzureSoraSDK in your .NET project.

## Prerequisites

Before installing the SDK, ensure you have:

- **.NET 6.0 or higher** installed ([Download .NET](https://dotnet.microsoft.com/download))
- **Azure OpenAI resource** with Sora model deployment
- **Valid API key** with appropriate permissions

## Installation Methods

### 1. Package Manager Console

```powershell
Install-Package AzureSoraSDK
```

### 2. .NET CLI

```bash
dotnet add package AzureSoraSDK
```

### 3. PackageReference

Add the following to your `.csproj` file:

```xml
<PackageReference Include="AzureSoraSDK" Version="1.0.0" />
```

### 4. Package Manager UI

1. Right-click on your project in Visual Studio
2. Select "Manage NuGet Packages"
3. Search for "AzureSoraSDK"
4. Click "Install"

## Verify Installation

After installation, you can verify by adding the using statement:

```csharp
using AzureSoraSDK;
```

## Dependencies

The SDK automatically includes the following dependencies:

- **Microsoft.Extensions.DependencyInjection** (>= 6.0.0)
- **Microsoft.Extensions.Http** (>= 6.0.0)
- **Microsoft.Extensions.Logging.Abstractions** (>= 6.0.0)
- **Microsoft.Extensions.Options** (>= 6.0.0)
- **Microsoft.Extensions.Options.DataAnnotations** (>= 6.0.0)
- **Polly.Extensions.Http** (>= 3.0.0)
- **System.Text.Json** (>= 6.0.0)

## Next Steps

- [Getting Started](Getting-Started) - Learn how to use the SDK
- [Configuration](Configuration) - Set up your Azure OpenAI credentials
- [Examples](Examples) - See code examples

## Troubleshooting Installation

### Common Issues

1. **Package not found**
   - Ensure you have a stable internet connection
   - Check if NuGet.org is accessible
   - Try clearing NuGet cache: `dotnet nuget locals all --clear`

2. **Version conflicts**
   - Check for conflicting package versions in your project
   - Consider updating to the latest versions of dependencies

3. **Build errors after installation**
   - Ensure your project targets .NET 6.0 or higher
   - Rebuild the solution: `dotnet clean && dotnet build`

For more help, see the [Troubleshooting](Troubleshooting) guide. 