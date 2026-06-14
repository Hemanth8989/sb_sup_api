using Bogus;
using Moq;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Tests.Common;

/// <summary>
/// Base class for all Application layer unit tests.
/// Provides pre-configured Moq mocks for the cross-cutting interfaces
/// that every handler and query depends on.
/// Inherit from this class, then create feature-specific mocks in the subclass constructor.
/// </summary>
public abstract class TestBase
{
    // ── Shared mocks — available to all test classes ───────────────────────────
    protected readonly Mock<ICurrentTenant>   MockCurrentTenant  = new(MockBehavior.Strict);
    protected readonly Mock<ICacheService>    MockCacheService   = new(MockBehavior.Loose);
    protected readonly Faker                  Faker              = new("en");

    // ── Setup helpers ──────────────────────────────────────────────────────────

    /// <summary>Configure MockCurrentTenant as a supplier tenant.</summary>
    protected void SetupAsSupplier(Guid? tenantId = null)
    {
        var id = tenantId ?? Guid.NewGuid();
        MockCurrentTenant.Setup(t => t.TenantId).Returns(id);
        MockCurrentTenant.Setup(t => t.TenantType).Returns("supplier");
        MockCurrentTenant.Setup(t => t.UserId).Returns(Guid.NewGuid());
        MockCurrentTenant.Setup(t => t.Role).Returns("owner");
        MockCurrentTenant.Setup(t => t.IsSupplier).Returns(true);
        MockCurrentTenant.Setup(t => t.IsFabricator).Returns(false);
        MockCurrentTenant.Setup(t => t.IsAdminOrOwner).Returns(true);
    }

    /// <summary>Configure MockCurrentTenant as a fabricator tenant.</summary>
    protected void SetupAsFabricator(Guid? tenantId = null)
    {
        var id = tenantId ?? Guid.NewGuid();
        MockCurrentTenant.Setup(t => t.TenantId).Returns(id);
        MockCurrentTenant.Setup(t => t.TenantType).Returns("fabricator");
        MockCurrentTenant.Setup(t => t.UserId).Returns(Guid.NewGuid());
        MockCurrentTenant.Setup(t => t.Role).Returns("owner");
        MockCurrentTenant.Setup(t => t.IsSupplier).Returns(false);
        MockCurrentTenant.Setup(t => t.IsFabricator).Returns(true);
        MockCurrentTenant.Setup(t => t.IsAdminOrOwner).Returns(true);
    }
}