using ZMS.Core.Security;

namespace ZMS.Tests;

public class SecretRedactorTests
{
    [Theory]
    [InlineData("access_token=abc123")]
    [InlineData("refresh_token=abc123")]
    [InlineData("client_secret=abc123")]
    [InlineData("password=abc123")]
    [InlineData("Authorization: Bearer abc.def.ghi")]
    [InlineData("Server=tcp:example;User Id=app;Password=super-secret;")]
    [InlineData("https://example.blob.core.windows.net/a?sig=abc123&se=tomorrow")]
    public void Redact_RemovesSecretValues(string input)
    {
        var redacted = SecretRedactor.Redact(input);

        Assert.DoesNotContain("abc123", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[REDACTED]", redacted, StringComparison.Ordinal);
    }
}
