using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StoneBridge.Application.Catalog.Queries.SearchCatalogSlabs;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Tests.Common;
using Xunit;

namespace StoneBridge.Application.Tests.Catalog;

/// <summary>
/// Unit tests for SearchCatalogSlabsQueryHandler.
/// Uses Moq for ICatalogRepository and ICurrentTenant.
/// Verifies: authorisation rules, delegation to repository, return value passthrough.
/// </summary>
public sealed class SearchCatalogSlabsQueryHandlerTests : TestBase
{
    private readonly Mock<ICatalogRepository>             _mockRepo;
    private readonly Mock<ILogger<SearchCatalogSlabsQueryHandler>> _mockLogger;
    private readonly SearchCatalogSlabsQueryHandler       _handler;

    public SearchCatalogSlabsQueryHandlerTests()
    {
        _mockRepo   = new Mock<ICatalogRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<SearchCatalogSlabsQueryHandler>>();

        _handler = new SearchCatalogSlabsQueryHandler(
            _mockRepo.Object,
            MockCurrentTenant.Object,
            _mockLogger.Object);
    }

    // ── Authorization tests ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCallerIsFabricator_DelegatesToRepository()
    {
        // Arrange
        var fabricatorId = Guid.NewGuid();
        SetupAsFabricator(fabricatorId);

        var expectedResult = FakeData.PagedCatalogSlabs(itemCount: 3, totalCount: 10);
        var searchParams   = FakeData.DefaultSearchParams();

        _mockRepo
            .Setup(r => r.SearchAsync(
                fabricatorId,
                searchParams,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new SearchCatalogSlabsQuery(searchParams);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockRepo.Verify(
            r => r.SearchAsync(fabricatorId, searchParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCallerIsSupplier_ThrowsForbiddenException()
    {
        // Arrange
        SetupAsSupplier();
        var query = new SearchCatalogSlabsQuery(FakeData.DefaultSearchParams());

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*fabricator*");

        _mockRepo.Verify(
            r => r.SearchAsync(
                It.IsAny<Guid>(),
                It.IsAny<StoneBridge.Application.Catalog.DTOs.CatalogSearchParams>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Repository delegation tests ────────────────────────────────────────────

    [Fact]
    public async Task Handle_PassesFabricatorIdFromCurrentTenantToRepository()
    {
        // Arrange
        var fabricatorId = Guid.NewGuid();
        SetupAsFabricator(fabricatorId);

        var searchParams = FakeData.DefaultSearchParams();

        _mockRepo
            .Setup(r => r.SearchAsync(
                fabricatorId,          // ← must receive the exact fabricator ID
                searchParams,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeData.PagedCatalogSlabs());

        // Act
        await _handler.Handle(new SearchCatalogSlabsQuery(searchParams), CancellationToken.None);

        // Assert — verifies the exact fabricatorId was passed, not a different tenant ID
        _mockRepo.Verify(
            r => r.SearchAsync(
                fabricatorId,
                searchParams,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsExactPagedResultFromRepository()
    {
        // Arrange
        SetupAsFabricator();

        var slabs    = FakeData.CatalogSlabs(5);
        var expected = PagedResult<StoneBridge.Application.Catalog.DTOs.CatalogSlabDto>.Create(
            slabs, totalCount: 47, page: 2, perPage: 5);

        _mockRepo
            .Setup(r => r.SearchAsync(
                It.IsAny<Guid>(),
                It.IsAny<StoneBridge.Application.Catalog.DTOs.CatalogSearchParams>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(
            new SearchCatalogSlabsQuery(FakeData.DefaultSearchParams()),
            CancellationToken.None);

        // Assert — the handler does not mutate the result
        result.TotalCount.Should().Be(47);
        result.Page.Should().Be(2);
        result.PerPage.Should().Be(5);
        result.Items.Should().HaveCount(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmptyPage_ReturnsEmptyPagedResult()
    {
        // Arrange
        SetupAsFabricator();
        var empty = PagedResult<StoneBridge.Application.Catalog.DTOs.CatalogSlabDto>.Empty();

        _mockRepo
            .Setup(r => r.SearchAsync(
                It.IsAny<Guid>(),
                It.IsAny<StoneBridge.Application.Catalog.DTOs.CatalogSearchParams>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        // Act
        var result = await _handler.Handle(
            new SearchCatalogSlabsQuery(FakeData.DefaultSearchParams()),
            CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        SetupAsFabricator();

        _mockRepo
            .Setup(r => r.SearchAsync(
                It.IsAny<Guid>(),
                It.IsAny<StoneBridge.Application.Catalog.DTOs.CatalogSearchParams>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed."));

        var act = async () => await _handler.Handle(
            new SearchCatalogSlabsQuery(FakeData.DefaultSearchParams()),
            CancellationToken.None);

        // Assert — handler must not swallow exceptions
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed.");
    }

    [Fact]
    public async Task Handle_PassesSearchParamsUnmodifiedToRepository()
    {
        // Arrange
        SetupAsFabricator();

        var searchParams = new StoneBridge.Application.Catalog.DTOs.CatalogSearchParams
        {
            SearchQuery    = "Calacatta",
            MaterialTypes  = new[] { "marble" }.ToList().AsReadOnly(),
            ColorFamilies  = new[] { "white" }.ToList().AsReadOnly(),
            ThicknessMinCm = 2.5m,
            PriceMax       = 500m,
            IsRemnant      = false,
            SortBy         = "listPrice",
            SortDir        = "ASC",
            Page           = 3,
            PerPage        = 12,
        };

        _mockRepo
            .Setup(r => r.SearchAsync(
                It.IsAny<Guid>(),
                It.Is<StoneBridge.Application.Catalog.DTOs.CatalogSearchParams>(p =>
                    p.SearchQuery    == "Calacatta"  &&
                    p.ThicknessMinCm == 2.5m         &&
                    p.PriceMax       == 500m          &&
                    p.Page           == 3             &&
                    p.PerPage        == 12            &&
                    p.SortBy         == "listPrice"   &&
                    p.SortDir        == "ASC"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeData.PagedCatalogSlabs());

        // Act
        await _handler.Handle(
            new SearchCatalogSlabsQuery(searchParams),
            CancellationToken.None);

        // Assert — the exact same params object was forwarded to the repository
        _mockRepo.Verify(
            r => r.SearchAsync(
                It.IsAny<Guid>(),
                It.Is<StoneBridge.Application.Catalog.DTOs.CatalogSearchParams>(p =>
                    p.SearchQuery    == "Calacatta" &&
                    p.ThicknessMinCm == 2.5m        &&
                    p.Page           == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}