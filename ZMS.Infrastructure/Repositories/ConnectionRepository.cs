using Microsoft.EntityFrameworkCore;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Infrastructure.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly ZmsDbContext _dbContext;

    public ConnectionRepository(ZmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ConnectionProfile>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Connections
            .AsNoTracking()
            .OrderBy(connection => connection.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<ConnectionProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(connection => connection.Id == id, cancellationToken);
    }

    public async Task AddAsync(ConnectionProfile connection, CancellationToken cancellationToken)
    {
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ConnectionProfile connection, CancellationToken cancellationToken)
    {
        _dbContext.Connections.Update(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
