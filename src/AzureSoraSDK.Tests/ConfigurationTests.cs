using System;
using AzureSoraSDK.Configuration;
using FluentAssertions;
using Xunit;

namespace AzureSoraSDK.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void SoraClientOptions_DefaultValues_AreCorrect()
        {
            // Act
            var options = new SoraClientOptions();

            // Assert
            options.Endpoint.Should().BeEmpty();
            options.ApiKey.Should().BeEmpty();
            options.DeploymentName.Should().BeEmpty();
            options.ApiVersion.Should().Be("preview");
            options.HttpTimeout.Should().Be(TimeSpan.FromMinutes(5));
            options.MaxRetryAttempts.Should().Be(3);
            options.RetryDelay.Should().Be(TimeSpan.FromSeconds(2));
            options.DefaultPollingInterval.Should().Be(TimeSpan.FromSeconds(5));
            options.MaxWaitTime.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void PromptEnhancerOptions_DefaultValues_AreCorrect()
        {
            // Act
            var options = new PromptEnhancerOptions();

            // Assert
            options.Endpoint.Should().BeEmpty();
            options.ApiKey.Should().BeEmpty();
            options.DeploymentName.Should().BeEmpty();
            options.ApiVersion.Should().Be("preview");
            options.HttpTimeout.Should().Be(TimeSpan.FromMinutes(2));
            options.MaxRetryAttempts.Should().Be(3);
            options.RetryDelay.Should().Be(TimeSpan.FromSeconds(1));
            options.DefaultTemperature.Should().Be(0.7);
            options.DefaultTopP.Should().Be(0.9);
            options.MaxTokensPerRequest.Should().Be(1000);
        }

        [Fact]
        public void SoraClientOptions_Validate_WithValidOptions_DoesNotThrow()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithValidOptions_DoesNotThrow()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void SoraClientOptions_Validate_WithEmptyEndpoint_ThrowsException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithEmptyEndpoint_ThrowsArgumentException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Endpoint*");
        }

        [Fact]
        public void SoraClientOptions_Validate_WithInvalidEndpoint_ThrowsException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "not-a-url",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithInvalidEndpoint_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "not-a-url",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void SoraClientOptions_Validate_WithEmptyApiKey_ThrowsException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithEmptyApiKey_ThrowsArgumentException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "",
                DeploymentName = "test-deployment"
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*ApiKey*");
        }

        [Fact]
        public void SoraClientOptions_Validate_WithEmptyDeploymentName_ThrowsException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = ""
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithEmptyDeploymentName_ThrowsArgumentException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = ""
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*DeploymentName*");
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithEmptyApiVersion_ThrowsArgumentException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                ApiVersion = ""
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*ApiVersion*");
        }

        [Fact]
        public void SoraClientOptions_Validate_WithNegativeRetryAttempts_ThrowsException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                MaxRetryAttempts = -1
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithNegativeRetryAttempts_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                MaxRetryAttempts = -1
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void SoraClientOptions_Validate_WithExcessiveRetryAttempts_ThrowsException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                MaxRetryAttempts = 11
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithExcessiveRetryAttempts_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                MaxRetryAttempts = 11
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void SoraClientOptions_Validate_WithZeroHttpTimeout_ThrowsArgumentException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                HttpTimeout = TimeSpan.Zero
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*HttpTimeout*")
                .WithParameterName("HttpTimeout");
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithZeroHttpTimeout_ThrowsArgumentException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                HttpTimeout = TimeSpan.Zero
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*HttpTimeout*")
                .WithParameterName("HttpTimeout");
        }

        [Fact]
        public void SoraClientOptions_Validate_WithNegativeHttpTimeout_ThrowsArgumentException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                HttpTimeout = TimeSpan.FromSeconds(-1)
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*HttpTimeout*");
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithNegativeHttpTimeout_ThrowsArgumentException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                HttpTimeout = TimeSpan.FromSeconds(-1)
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*HttpTimeout*");
        }

        [Fact]
        public void SoraClientOptions_Validate_WithZeroRetryDelay_ThrowsArgumentException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                RetryDelay = TimeSpan.Zero
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*RetryDelay*");
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithZeroRetryDelay_ThrowsArgumentException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                RetryDelay = TimeSpan.Zero
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*RetryDelay*");
        }

        [Fact]
        public void SoraClientOptions_Validate_WithZeroPollingInterval_ThrowsArgumentException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                DefaultPollingInterval = TimeSpan.Zero
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*DefaultPollingInterval*");
        }

        [Fact]
        public void SoraClientOptions_Validate_WithZeroMaxWaitTime_ThrowsArgumentException()
        {
            // Arrange
            var options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                MaxWaitTime = TimeSpan.Zero
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*MaxWaitTime*");
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithInvalidTemperature_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                DefaultTemperature = -0.1
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithExcessiveTemperature_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                DefaultTemperature = 2.1
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithInvalidTopP_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                DefaultTopP = -0.1
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithExcessiveTopP_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                DefaultTopP = 1.1
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithInvalidMaxTokens_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                MaxTokensPerRequest = 0
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PromptEnhancerOptions_Validate_WithExcessiveMaxTokens_ThrowsException()
        {
            // Arrange
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                MaxTokensPerRequest = 5000
            };

            // Act & Assert
            var act = () => options.Validate();
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void SoraClientOptions_CanSetAllProperties()
        {
            // Arrange & Act
            var options = new SoraClientOptions
            {
                Endpoint = "https://custom.openai.azure.com",
                ApiKey = "custom-key",
                DeploymentName = "custom-deployment",
                ApiVersion = "2024-12-01",
                HttpTimeout = TimeSpan.FromMinutes(10),
                MaxRetryAttempts = 5,
                RetryDelay = TimeSpan.FromSeconds(5),
                DefaultPollingInterval = TimeSpan.FromSeconds(10),
                MaxWaitTime = TimeSpan.FromHours(2)
            };

            // Assert
            options.Endpoint.Should().Be("https://custom.openai.azure.com");
            options.ApiKey.Should().Be("custom-key");
            options.DeploymentName.Should().Be("custom-deployment");
            options.ApiVersion.Should().Be("2024-12-01");
            options.HttpTimeout.Should().Be(TimeSpan.FromMinutes(10));
            options.MaxRetryAttempts.Should().Be(5);
            options.RetryDelay.Should().Be(TimeSpan.FromSeconds(5));
            options.DefaultPollingInterval.Should().Be(TimeSpan.FromSeconds(10));
            options.MaxWaitTime.Should().Be(TimeSpan.FromHours(2));
        }

        [Fact]
        public void PromptEnhancerOptions_CanSetAllProperties()
        {
            // Arrange & Act
            var options = new PromptEnhancerOptions
            {
                Endpoint = "https://custom.openai.azure.com",
                ApiKey = "custom-key",
                DeploymentName = "custom-deployment",
                ApiVersion = "2024-03-01-preview",
                HttpTimeout = TimeSpan.FromMinutes(5),
                MaxRetryAttempts = 5,
                RetryDelay = TimeSpan.FromSeconds(3),
                DefaultTemperature = 0.5,
                DefaultTopP = 0.8,
                MaxTokensPerRequest = 2000
            };

            // Assert
            options.Endpoint.Should().Be("https://custom.openai.azure.com");
            options.ApiKey.Should().Be("custom-key");
            options.DeploymentName.Should().Be("custom-deployment");
            options.ApiVersion.Should().Be("2024-03-01-preview");
            options.HttpTimeout.Should().Be(TimeSpan.FromMinutes(5));
            options.MaxRetryAttempts.Should().Be(5);
            options.RetryDelay.Should().Be(TimeSpan.FromSeconds(3));
            options.DefaultTemperature.Should().Be(0.5);
            options.DefaultTopP.Should().Be(0.8);
            options.MaxTokensPerRequest.Should().Be(2000);
        }
    }
} 