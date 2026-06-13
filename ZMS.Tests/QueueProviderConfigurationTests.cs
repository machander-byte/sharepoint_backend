using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZMS.Core.Interfaces;
using ZMS.MigrationEngine.DependencyInjection;

namespace ZMS.Tests;

public class QueueProviderConfigurationTests
{
    [Fact]
    public void AddZmsMigrationEngine_DefaultsToLocalQueue()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddZmsMigrationEngine(configuration);
        using var provider = services.BuildServiceProvider();

        var diagnostics = provider.GetRequiredService<IQueueDiagnostics>();

        Assert.Equal("Local", diagnostics.Provider);
        Assert.True(diagnostics.IsConfigured);
    }

    [Fact]
    public void AddZmsMigrationEngine_AzureServiceBusWithoutConnectionStringIsNotConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QueueProvider"] = "AzureServiceBus"
            })
            .Build();

        services.AddZmsMigrationEngine(configuration);
        using var provider = services.BuildServiceProvider();

        var diagnostics = provider.GetRequiredService<IQueueDiagnostics>();

        Assert.Equal("AzureServiceBus", diagnostics.Provider);
        Assert.False(diagnostics.IsConfigured);
        Assert.Contains("ConnectionString", diagnostics.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }
}
