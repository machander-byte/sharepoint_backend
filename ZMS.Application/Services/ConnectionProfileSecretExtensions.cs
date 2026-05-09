using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Application.Services;

public static class ConnectionProfileSecretExtensions
{
    public static ConnectionProfile WithUnprotectedSecrets(
        this ConnectionProfile connection,
        ISecretProtector secretProtector)
    {
        return new ConnectionProfile
        {
            Id = connection.Id,
            Name = connection.Name,
            Type = connection.Type,
            Url = connection.Url,
            Username = connection.Username,
            Password = secretProtector.Unprotect(connection.Password),
            ClientId = connection.ClientId,
            ClientSecret = secretProtector.Unprotect(connection.ClientSecret),
            TenantId = connection.TenantId,
            RootPath = connection.RootPath,
            AdditionalSettings = new Dictionary<string, string>(connection.AdditionalSettings, StringComparer.OrdinalIgnoreCase),
            IsEnabled = connection.IsEnabled,
            CreatedUtc = connection.CreatedUtc,
            UpdatedUtc = connection.UpdatedUtc
        };
    }
}
