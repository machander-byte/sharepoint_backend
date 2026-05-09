using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using ZMS.Core.Interfaces;

namespace ZMS.Application.Services;

public class SecretProtector : ISecretProtector
{
    private const string ProtectedPrefix = "dp:";
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("ZettalogixMigrationSuite.ConnectionSecrets.v1");
    }

    public string? Protect(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        return $"{ProtectedPrefix}{_protector.Protect(secret.Trim())}";
    }

    public string? Unprotect(string? protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
        {
            return null;
        }

        if (!protectedSecret.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
        {
            return protectedSecret;
        }

        try
        {
            return _protector.Unprotect(protectedSecret[ProtectedPrefix.Length..]);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException(
                "A saved connection secret could not be decrypted. Configure a persistent DataProtection:KeyRingPath on every API/worker host and recreate the affected connection if the key ring was lost.",
                exception);
        }
    }
}
