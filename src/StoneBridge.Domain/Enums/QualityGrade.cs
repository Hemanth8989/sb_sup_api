namespace StoneBridge.Domain.Enums;

/// <summary>
/// Quality grading for slabs.
/// A = premium, no visible defects.
/// B = commercial, minor variations acceptable.
/// C = builder grade, visible variations acceptable.
/// Stored as single CHAR: 'A', 'B', 'C'.
/// </summary>
public enum QualityGrade
{
    A = 1,
    B = 2,
    C = 3
}