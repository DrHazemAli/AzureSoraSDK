# Contributing

Thank you for your interest in contributing to AzureSoraSDK! This guide will help you get started.

## Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct:

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Accept feedback gracefully

## How to Contribute

### Reporting Issues

1. **Search existing issues** to avoid duplicates
2. **Create a new issue** with:
   - Clear, descriptive title
   - Steps to reproduce
   - Expected vs actual behavior
   - SDK version and .NET version
   - Code samples (without sensitive data)

### Feature Requests

1. **Check existing requests** first
2. **Open a discussion** to gauge interest
3. **Create an issue** with:
   - Use case description
   - Proposed API design
   - Examples of how it would be used

### Submitting Changes

#### 1. Fork and Clone

```bash
# Fork on GitHub, then:
git clone https://github.com/your-username/AzureSoraSDK.git
cd AzureSoraSDK
git remote add upstream https://github.com/DrHazemAli/AzureSoraSDK.git
```

#### 2. Create a Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-description
```

#### 3. Make Changes

Follow our coding standards:
- Use meaningful variable and method names
- Add XML documentation comments
- Follow C# naming conventions
- Keep methods small and focused

#### 4. Write Tests

```csharp
[Fact]
public async Task YourMethod_WithCondition_ShouldBehaveExpected()
{
    // Arrange
    var client = CreateTestClient();
    
    // Act
    var result = await client.YourMethod();
    
    // Assert
    result.Should().NotBeNull();
}
```

#### 5. Run Tests

```bash
dotnet test
```

#### 6. Commit Changes

```bash
git add .
git commit -m "feat: add support for custom video styles"
# or
git commit -m "fix: handle null prompt gracefully"
```

Follow conventional commits:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `test:` Test additions/changes
- `refactor:` Code refactoring
- `chore:` Build/tooling changes

#### 7. Push and Create PR

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub.

## Development Setup

### Prerequisites

- .NET 6.0 SDK or higher
- Visual Studio 2022 or VS Code
- Git

### Building

```bash
dotnet build
```

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific tests
dotnet test --filter "FullyQualifiedName~SoraClientTests"
```

### Code Style

We use .editorconfig for consistent styling. Most IDEs will automatically apply these rules.

Key conventions:
- 4 spaces for indentation
- Opening braces on new line
- `var` for obvious types
- Explicit types for ambiguous cases

Example:
```csharp
public class VideoService
{
    private readonly ISoraClient _client;
    
    public VideoService(ISoraClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }
    
    public async Task<string> GenerateVideoAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }
        
        var jobId = await _client.SubmitVideoJobAsync(
            prompt, 
            width: 1920, 
            height: 1080, 
            durationInSeconds: 10);
            
        return jobId;
    }
}
```

## Pull Request Guidelines

### PR Title

Use conventional commit format:
- `feat: add batch video generation support`
- `fix: correct URL format for API endpoints`
- `docs: update configuration examples`

### PR Description

Include:
- **What** changed
- **Why** it changed
- **How** to test it
- Related issue numbers

Template:
```markdown
## Description
Brief description of changes

## Motivation
Why these changes are needed

## Testing
How to test these changes

Fixes #123
```

### PR Checklist

- [ ] Tests pass locally
- [ ] New tests added for new features
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] No breaking changes (or documented)

## Documentation

### Code Documentation

```csharp
/// <summary>
/// Submits a video generation job to Azure OpenAI.
/// </summary>
/// <param name="prompt">The text prompt describing the video</param>
/// <param name="width">Video width in pixels (must be divisible by 8)</param>
/// <returns>The job ID for tracking the generation progress</returns>
/// <exception cref="SoraValidationException">Thrown when parameters are invalid</exception>
public async Task<string> SubmitVideoJobAsync(
    string prompt, 
    int width, 
    int height, 
    int durationInSeconds)
{
    // Implementation
}
```

### Wiki Updates

When adding features, update relevant wiki pages:
- API Reference for new methods
- Configuration for new options
- Examples for usage scenarios
- Troubleshooting for common issues

## Release Process

1. Update version in `.csproj` files
2. Update CHANGELOG.md
3. Create a release tag
4. Package will be automatically published to NuGet

## Getting Help

- **Discord**: [Join our community](https://discord.gg/azuresorasdk)
- **Discussions**: Use GitHub Discussions
- **Email**: hazem@azuresorasdk.com

## Recognition

Contributors will be:
- Listed in CONTRIBUTORS.md
- Mentioned in release notes
- Given credit in documentation

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Thank You!

Every contribution helps make AzureSoraSDK better. We appreciate your time and effort!

### Quick Links

- [Report a Bug](https://github.com/DrHazemAli/AzureSoraSDK/issues/new?labels=bug)
- [Request a Feature](https://github.com/DrHazemAli/AzureSoraSDK/issues/new?labels=enhancement)
- [Ask a Question](https://github.com/DrHazemAli/AzureSoraSDK/discussions/new) 