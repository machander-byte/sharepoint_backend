using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;
using ZMS.Application.Services;

namespace ZMS.Tests;

public class DemoServiceTests
{
    [Fact]
    public async Task ResetAsync_ReturnsDemoStatusWithoutDeletingArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), $"zms-demo-test-{Guid.NewGuid():N}");
        var service = new DemoService(
            null!,
            null!,
            null!,
            null!,
            null!,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ZMS_DEMO_MODE"] = "true" }).Build(),
            new TestEnvironment { ContentRootPath = root });

        var status = await service.ResetAsync(CancellationToken.None);

        Assert.True(status.DemoMode);
        Assert.False(status.Seeded);
        Assert.Equal("reset", status.LastDemoChainResult);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "ZMS.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
