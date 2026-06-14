namespace StoneBridge.Domain.Enums;

/// <summary>
/// Stone material types supported by the platform.
/// Stored as lowercase VARCHAR in PostgreSQL: 'granite', 'marble', etc.
/// </summary>
public enum MaterialType
{
    Granite     = 1,
    Marble      = 2,
    Quartzite   = 3,
    Quartz      = 4,
    Porcelain   = 5,
    Dekton      = 6,
    Limestone   = 7,
    Travertine  = 8,
    Onyx        = 9,
    Slate       = 10,
    Soapstone   = 11,
    Other       = 12
}