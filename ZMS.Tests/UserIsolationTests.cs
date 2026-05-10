using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ZMS.Core.Enums;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;
using ZMS.Infrastructure.Repositories;

namespace ZMS.Tests;

public class UserIsolationTests
{
    [Fact]
    public async Task ConnectionRepository_ListAsync_ReturnsOnlyEnabledConnectionsForRequestedUser()
    {
        await using var database = await CreateOpenDatabaseAsync();
        await using var context = CreateContext(database);
        await context.Database.EnsureCreatedAsync();

        var userId = "user123";
        var otherUserId = "user456";
        var visibleConnection = new ConnectionProfile
        {
            Name = "User Connection",
            UserId = userId,
            Type = ConnectionType.GoogleDrive,
            Url = "https://drive.google.com/drive/folders/source-folder"
        };

        context.Connections.AddRange(
            visibleConnection,
            new ConnectionProfile
            {
                Name = "Other User Connection",
                UserId = otherUserId,
                Type = ConnectionType.GoogleDrive,
                Url = "https://drive.google.com/drive/folders/other-folder"
            },
            new ConnectionProfile
            {
                Name = "Deleted User Connection",
                UserId = userId,
                Type = ConnectionType.GoogleDrive,
                Url = "https://drive.google.com/drive/folders/deleted-folder",
                IsEnabled = false
            });
        await context.SaveChangesAsync();

        var repository = new ConnectionRepository(context);

        var result = await repository.ListAsync(userId, CancellationToken.None);

        var connection = Assert.Single(result);
        Assert.Equal(visibleConnection.Id, connection.Id);
        Assert.Equal(userId, connection.UserId);
        Assert.True(connection.IsEnabled);
    }

    [Fact]
    public async Task ConnectionRepository_GetByIdAsync_RequiresOwnerAndEnabledConnection()
    {
        await using var database = await CreateOpenDatabaseAsync();
        await using var context = CreateContext(database);
        await context.Database.EnsureCreatedAsync();

        var userId = "user123";
        var otherUserId = "user456";
        var connection = new ConnectionProfile
        {
            Name = "User Connection",
            UserId = userId,
            Type = ConnectionType.GoogleDrive,
            Url = "https://drive.google.com/drive/folders/source-folder"
        };
        var disabledConnection = new ConnectionProfile
        {
            Name = "Deleted User Connection",
            UserId = userId,
            Type = ConnectionType.GoogleDrive,
            Url = "https://drive.google.com/drive/folders/deleted-folder",
            IsEnabled = false
        };

        context.Connections.AddRange(connection, disabledConnection);
        await context.SaveChangesAsync();

        var repository = new ConnectionRepository(context);

        var ownerResult = await repository.GetByIdAsync(connection.Id, userId, CancellationToken.None);
        var otherUserResult = await repository.GetByIdAsync(connection.Id, otherUserId, CancellationToken.None);
        var disabledResult = await repository.GetByIdAsync(disabledConnection.Id, userId, CancellationToken.None);

        Assert.NotNull(ownerResult);
        Assert.Equal(userId, ownerResult!.UserId);
        Assert.Null(otherUserResult);
        Assert.Null(disabledResult);
    }

    [Fact]
    public async Task ConnectionRepository_DeleteAsync_DisablesOnlyOwnedConnection()
    {
        await using var database = await CreateOpenDatabaseAsync();
        await using var context = CreateContext(database);
        await context.Database.EnsureCreatedAsync();

        var userId = "user123";
        var otherUserId = "user456";
        var connection = new ConnectionProfile
        {
            Name = "User Connection",
            UserId = userId,
            Type = ConnectionType.GoogleDrive,
            Url = "https://drive.google.com/drive/folders/source-folder"
        };

        context.Connections.Add(connection);
        await context.SaveChangesAsync();

        var repository = new ConnectionRepository(context);

        Assert.False(await repository.DeleteAsync(connection.Id, otherUserId, CancellationToken.None));
        Assert.True((await context.Connections.FindAsync(new object?[] { connection.Id }, CancellationToken.None))!.IsEnabled);

        Assert.True(await repository.DeleteAsync(connection.Id, userId, CancellationToken.None));

        var listedConnections = await repository.ListAsync(userId, CancellationToken.None);
        var storedConnection = await context.Connections.FindAsync(new object?[] { connection.Id }, CancellationToken.None);

        Assert.Empty(listedConnections);
        Assert.NotNull(storedConnection);
        Assert.False(storedConnection!.IsEnabled);
    }

    private static async Task<SqliteConnection> CreateOpenDatabaseAsync()
    {
        var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        return database;
    }

    private static ZmsDbContext CreateContext(SqliteConnection database)
    {
        var options = new DbContextOptionsBuilder<ZmsDbContext>()
            .UseSqlite(database)
            .Options;

        return new ZmsDbContext(options);
    }
}
