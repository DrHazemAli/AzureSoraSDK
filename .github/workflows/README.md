# GitHub Workflows for NuGet Publishing

This directory contains a GitHub Actions workflow to automatically publish the AzureSoraSDK package to NuGet using secure GitHub secrets.

## Available Workflow

### `publish-nuget.yml` (Secure)
- Uses GitHub secrets for secure API key storage
- Requires one-time setup of repository secrets
- **✅ Secure by default** - no hardcoded API keys

## Setup Instructions

### 1. Set up GitHub Secret (Required):
- Go to your repository on GitHub
- Navigate to Settings → Secrets and variables → Actions
- Click "New repository secret"
- Name: `NUGET_API_KEY`
- Value: `your-actual-nuget-api-key-here`
- Click "Add secret"

### 2. The workflow will automatically trigger on:
- Pushing version tags (e.g., `v1.0.1`, `v1.1.0`)
- Creating GitHub releases
- Manual triggering from Actions tab

## Triggering a Release

### Method 1: Create a Git Tag
```bash
# Update version in AzureSoraSDK.csproj first
git add .
git commit -m "Release v1.0.1"
git tag v1.0.1
git push origin v1.0.1
```

### Method 2: Create a GitHub Release
1. Go to your repository on GitHub
2. Click "Releases" → "Create a new release"
3. Choose or create a tag (e.g., `v1.0.1`)
4. Fill in release notes
5. Click "Publish release"

### Method 3: Manual Trigger
1. Go to Actions tab in your repository
2. Select the "Publish to NuGet" workflow
3. Click "Run workflow"
4. Choose the branch and click "Run workflow"

## What the Workflow Does

1. **Checkout**: Downloads the repository code
2. **Setup .NET**: Installs .NET 6.0 SDK
3. **Restore**: Downloads NuGet dependencies
4. **Build**: Compiles the solution in Release mode
5. **Test**: Runs unit tests to ensure quality
6. **Pack**: Creates the NuGet package
7. **Publish**: Uploads the package to NuGet.org using secure API key
8. **Archive**: Saves the package as a GitHub artifact

## Version Management

- Update the version in `src/AzureSoraSDK/AzureSoraSDK.csproj`
- The workflow uses `--skip-duplicate` to avoid errors if the version already exists
- Consider using semantic versioning (Major.Minor.Patch)

## Security Features

- ✅ **API Key Protection**: Uses GitHub secrets instead of hardcoded values
- ✅ **Environment Variables**: Proper environment variable handling
- ✅ **No Sensitive Data**: No API keys visible in workflow files or logs

## Troubleshooting

- **Build Failures**: Check the Actions tab for detailed logs
- **Test Failures**: The workflow will stop if tests fail
- **Duplicate Package**: Use `--skip-duplicate` flag (already included)
- **API Key Issues**: Verify the secret `NUGET_API_KEY` is set correctly
- **Missing Secret**: The workflow will fail if `NUGET_API_KEY` secret is not configured 