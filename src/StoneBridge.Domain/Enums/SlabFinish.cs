namespace StoneBridge.Domain.Enums;

/// <summary>
/// Surface finish applied to a slab.
/// Stored as lowercase VARCHAR: 'polished', 'honed', etc.
/// </summary>
public enum SlabFinish
{
    Polished    = 1,
    Honed       = 2,
    Leathered   = 3,
    Brushed     = 4,
    Sandblasted = 5,
    Flamed      = 6,
    Natural     = 7
}