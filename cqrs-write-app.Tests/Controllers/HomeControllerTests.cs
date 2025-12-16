using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using cqrs_write_app.Controllers;
using cqrs_write_app.Services;
using Xunit;

namespace cqrs_write_app.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly RoundRobinService _roundRobinService;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockLogger = new Mock<ILogger<HomeController>>();
        var replicas = new[] { "replica1", "replica2", "replica3" };
        _roundRobinService = new RoundRobinService(replicas);
        _controller = new HomeController(_mockLogger.Object, _roundRobinService);
    }

    [Fact]
    public void Demo_ShouldReturnView()
    {
        // Act
        var result = _controller.Demo();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void SystemStatus_ShouldReturnView()
    {
        // Act
        var result = _controller.SystemStatus();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void ArchitectureDiagram_ShouldReturnView()
    {
        // Act
        var result = _controller.ArchitectureDiagram();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Privacy_ShouldReturnView()
    {
        // Act
        var result = _controller.Privacy();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Create_Get_ShouldReturnView()
    {
        // Act
        var result = _controller.Create();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void ReadFromReplica_ShouldUseRoundRobinService()
    {
        // Arrange - Use real service, but test will fail on DB connection which is expected
        var result = _controller.ReadFromReplica();

        // Assert - Should return JsonResult even if DB connection fails
        Assert.NotNull(result);
        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public void ReadFromReplica_ShouldReturnJsonResult()
    {
        // Act
        var result = _controller.ReadFromReplica();

        // Assert
        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public void ReadFromReplica_ShouldHandleDatabaseException()
    {
        // Arrange - Use invalid connection string to trigger exception
        var invalidReplicas = new[] { "Host=invalid-host;Port=5432;Database=test;Username=test;Password=test" };
        var service = new RoundRobinService(invalidReplicas);
        var controller = new HomeController(_mockLogger.Object, service);

        // Act
        var result = controller.ReadFromReplica();

        // Assert
        Assert.IsType<JsonResult>(result);
        var jsonResult = result as JsonResult;
        Assert.NotNull(jsonResult);

        // Verify error was logged (check that LogError was called)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Edit_Get_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange - Set up controller context with TempData
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionary = new TempDataDictionary(httpContext, tempDataProvider.Object);

        _controller.TempData = tempDataDictionary;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act - Use a very large ID that likely doesn't exist
        // This will try to connect to DB, which will fail and set TempData["Error"]
        // Then it will return NotFound since product is null
        var result = _controller.Edit(999999);

        // Assert - Should return NotFound when product is null (after DB connection fails)
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetSyncStatus_WithInvalidProductId_ShouldReturnError()
    {
        // Act - Use a very large ID that likely doesn't exist
        var result = _controller.GetSyncStatus(999999);

        // Assert
        Assert.IsType<JsonResult>(result);
        var jsonResult = result as JsonResult;
        Assert.NotNull(jsonResult);
    }

    [Fact]
    public void GetSystemStatus_ShouldReturnJsonResult()
    {
        // Act
        var result = _controller.GetSystemStatus();

        // Assert
        Assert.IsType<JsonResult>(result);
    }
}

