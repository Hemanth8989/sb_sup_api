namespace StoneBridge.Domain.Enums;

/// <summary>
/// The lifecycle status of a physical slab.
/// Stored in PostgreSQL as lowercase VARCHAR: 'available', 'reserved', etc.
/// Transitions enforced by Slab domain entity methods (added in later phases).
/// </summary>
public enum SlabStatus
{
    /// <summary>Visible to connected fabricators. Can be added to a purchase order.</summary>
    Available  = 1,

    /// <summary>Held against a pending PO. Not orderable by other fabricators.</summary>
    Reserved   = 2,

    /// <summary>PO confirmed by supplier. Being prepared for shipment.</summary>
    Allocated  = 3,

    /// <summary>Physically dispatched to the fabricator.</summary>
    Shipped    = 4,

    /// <summary>Temporarily unavailable. Hidden from fabricator catalog.</summary>
    Hold       = 5,

    /// <summary>Terminal state. Delivered and confirmed received.</summary>
    Sold       = 6
}