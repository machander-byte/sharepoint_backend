using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZMS.Application.Discovery;
using ZMS.Core.Options;

namespace ZMS.Tests;

public class LiveGraphDiscoveryScannerTests
{
    [Fact]
    public async Task ScanAsync_WhenCredentialsMissing_ReturnsPartialFallback()
    {
        var scanner = new LiveSharePointDiscoveryScanner(
            new HttpClient(new FakeGraphHandler()),
            Options.Create(new DiscoveryOptions()),
            new ConfigurationBuilder().Build(),
            NullLogger<LiveSharePointDiscoveryScanner>.Instance);

        var result = await scanner.ScanAsync(
            Guid.NewGuid().ToString("D"),
            new DiscoveryScanRequest
            {
                Mode = "live",
                SiteUrls = ["https://contoso.sharepoint.com/sites/hr"]
            },
            (_, _) => Task.CompletedTask);

        Assert.True(result.IsPartial);
        Assert.NotEmpty(result.Warnings);
        Assert.Single(result.SiteCollections);
    }

    [Fact]
    public async Task ScanAsync_RetriesAfterGraphThrottle_AndPersistsPartialTelemetry()
    {
        var handler = new FakeGraphHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCOVERY_TENANT_ID"] = "tenant",
                ["DISCOVERY_CLIENT_ID"] = "client",
                ["DISCOVERY_CLIENT_SECRET"] = "secret",
                ["DISCOVERY_MAX_ITEMS"] = "10"
            })
            .Build();
        var scanner = new LiveSharePointDiscoveryScanner(
            new HttpClient(handler),
            Options.Create(new DiscoveryOptions()),
            configuration,
            NullLogger<LiveSharePointDiscoveryScanner>.Instance);

        var result = await scanner.ScanAsync(
            Guid.NewGuid().ToString("D"),
            new DiscoveryScanRequest
            {
                Mode = "live",
                SiteUrls = ["https://contoso.sharepoint.com/sites/hr"],
                IncludePermissions = false
            },
            (_, _) => Task.CompletedTask);

        Assert.Equal(1, result.ThrottleCount);
        Assert.Single(result.SiteCollections);
        Assert.Single(result.SiteCollections[0].Libraries);
        Assert.Single(result.SiteCollections[0].Libraries[0].Files);
    }

    private sealed class FakeGraphHandler : HttpMessageHandler
    {
        private int _driveChildrenAttempts;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/oauth2/v2.0/token", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"access_token":"token","expires_in":3600}""");
            }

            if (uri.Contains("/sites/contoso.sharepoint.com:/sites/hr", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"id":"site-1","displayName":"HR","webUrl":"https://contoso.sharepoint.com/sites/hr"}""");
            }

            if (uri.Contains("/sites/site-1/lists", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"value":[{"id":"list-1","displayName":"Announcements","webUrl":"https://contoso.sharepoint.com/sites/hr/Lists/Announcements","list":{"template":"genericList"}}]}""");
            }

            if (uri.Contains("/sites/site-1/drives", StringComparison.OrdinalIgnoreCase))
            {
                return Json("""{"value":[{"id":"drive-1","name":"Documents","webUrl":"https://contoso.sharepoint.com/sites/hr/Documents","driveType":"documentLibrary"}]}""");
            }

            if (uri.Contains("/drives/drive-1/root/children", StringComparison.OrdinalIgnoreCase))
            {
                _driveChildrenAttempts++;
                if (_driveChildrenAttempts == 1)
                {
                    var throttled = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    throttled.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
                    return Task.FromResult(throttled);
                }

                return Json("""{"value":[{"id":"file-1","name":"policy.docx","size":1024,"webUrl":"https://contoso.sharepoint.com/sites/hr/Documents/policy.docx","file":{},"lastModifiedDateTime":"2026-05-01T00:00:00Z"}]}""");
            }

            return Json("""{"value":[]}""");
        }

        private static Task<HttpResponseMessage> Json(string json)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
