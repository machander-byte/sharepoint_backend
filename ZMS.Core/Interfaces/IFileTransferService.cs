using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IFileTransferService
{
    Task<string> TransferAsync(
        ConnectionProfile targetConnection,
        MigrationJob job,
        MigrationItem item,
        Stream content,
        CancellationToken cancellationToken);
}
