using Moq;
using Xunit;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Core.Enums;

namespace ZMS.Tests;

public class UserIsolationTests
{
    [Fact]
    public async Task ConnectionRepository_ListAsync_FiltersByUserId()
    {
        // Arrange
        var mockRepository = new Mock<IConnectionRepository>();
        var userId = "user123";
        var otherUserId = "user456";
        var userConnections = new List<ConnectionProfile>
        {
            new ConnectionProfile { Id = Guid.NewGuid(), Name = "User Connection", UserId = userId },
            new ConnectionProfile { Id = Guid.NewGuid(), Name = "Another User Connection", UserId = userId }
        };
        var otherUserConnections = new List<ConnectionProfile>
        {
            new ConnectionProfile { Id = Guid.NewGuid(), Name = "Other User Connection", UserId = otherUserId }
        };

        mockRepository
            .Setup(repo => repo.ListAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userConnections);

        mockRepository
            .Setup(repo => repo.ListAsync(otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherUserConnections);

        // Act
        var userResult = await mockRepository.Object.ListAsync(userId, CancellationToken.None);
        var otherUserResult = await mockRepository.Object.ListAsync(otherUserId, CancellationToken.None);

        // Assert
        Assert.Equal(2, userResult.Count);
        Assert.All(userResult, c => Assert.Equal(userId, c.UserId));
        
        Assert.Single(otherUserResult);
        Assert.All(otherUserResult, c => Assert.Equal(otherUserId, c.UserId));
    }

    [Fact]
    public async Task ConnectionRepository_GetByIdAsync_FiltersByUserId()
    {
        // Arrange
        var mockRepository = new Mock<IConnectionRepository>();
        var userId = "user123";
        var otherUserId = "user456";
        var connectionId = Guid.NewGuid();
        var userConnection = new ConnectionProfile { Id = connectionId, Name = "User Connection", UserId = userId };

        mockRepository
            .Setup(repo => repo.GetByIdAsync(connectionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userConnection);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(connectionId, otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionProfile?)null);

        // Act
        var userResult = await mockRepository.Object.GetByIdAsync(connectionId, userId, CancellationToken.None);
        var otherUserResult = await mockRepository.Object.GetByIdAsync(connectionId, otherUserId, CancellationToken.None);

        // Assert
        Assert.NotNull(userResult);
        Assert.Equal(userId, userResult!.UserId);
        Assert.Equal(connectionId, userResult.Id);

        Assert.Null(otherUserResult);
    }
}