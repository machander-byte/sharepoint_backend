using System.Text.RegularExpressions;

namespace ZMS.Core.Security;

public static partial class SecretRedactor
{
    private const string Redacted = "[REDACTED]";

    private static readonly string[] SecretKeyFragments =
    [
        "access_token",
        "refresh_token",
        "client_secret",
        "clientsecret",
        "password",
        "authorization",
        "sharedaccesssignature",
        "sig",
        "apikey",
        "api_key",
        "private_key",
        "connectionstring"
    ];

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var redacted = AuthorizationHeaderRegex().Replace(value, $"Authorization: Bearer {Redacted}");
        redacted = KeyValueSecretRegex().Replace(redacted, match =>
        {
            var key = match.Groups["key"].Value;
            return IsSecretKey(key) ? $"{key}{match.Groups["sep"].Value}{QuoteIfPresent(match.Groups["quote"].Value)}{Redacted}{QuoteIfPresent(match.Groups["quote"].Value)}" : match.Value;
        });
        redacted = JsonSecretRegex().Replace(redacted, match =>
        {
            var key = match.Groups["key"].Value;
            return IsSecretKey(key) ? $"\"{key}\"{match.Groups["sep"].Value}\"{Redacted}\"" : match.Value;
        });
        redacted = SasTokenRegex().Replace(redacted, match => $"{match.Groups["prefix"].Value}{Redacted}");
        redacted = JwtRegex().Replace(redacted, Redacted);

        return redacted;
    }

    public static object? RedactObject(object? value)
    {
        return value switch
        {
            null => null,
            string text => Redact(text),
            IDictionary<string, string> dictionary => dictionary.ToDictionary(
                pair => pair.Key,
                pair => IsSecretKey(pair.Key) ? Redacted : Redact(pair.Value),
                StringComparer.OrdinalIgnoreCase),
            IDictionary<string, object?> dictionary => dictionary.ToDictionary(
                pair => pair.Key,
                pair => IsSecretKey(pair.Key) ? Redacted : RedactObject(pair.Value),
                StringComparer.OrdinalIgnoreCase),
            _ => value
        };
    }

    private static bool IsSecretKey(string key)
    {
        var normalized = key.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return SecretKeyFragments.Any(fragment =>
            normalized.Contains(
                fragment.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant(),
                StringComparison.OrdinalIgnoreCase));
    }

    private static string QuoteIfPresent(string quote) => string.IsNullOrEmpty(quote) ? string.Empty : quote;

    [GeneratedRegex(@"Authorization\s*:\s*Bearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase)]
    private static partial Regex AuthorizationHeaderRegex();

    [GeneratedRegex(@"(?<key>[A-Za-z0-9_.:-]*(?:token|secret|password|authorization|apikey|api_key|private_key|connectionstring|sig)[A-Za-z0-9_.:-]*)(?<sep>\s*[=:]\s*)(?<quote>[""']?)[^""'\s;&,}]+(?<quote2>[""']?)", RegexOptions.IgnoreCase)]
    private static partial Regex KeyValueSecretRegex();

    [GeneratedRegex(@"""(?<key>[^""]*(?:token|secret|password|authorization|apikey|api_key|private_key|connectionstring|sig)[^""]*)""(?<sep>\s*:\s*)""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex JsonSecretRegex();

    [GeneratedRegex(@"(?<prefix>[?&](?:sig|token|se|sp|sv|spr|skoid|sktid|skt|ske|sks|skv)=)[^&\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex SasTokenRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b")]
    private static partial Regex JwtRegex();
}
