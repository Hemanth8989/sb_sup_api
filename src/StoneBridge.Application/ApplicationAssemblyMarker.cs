namespace StoneBridge.Application;

/// <summary>
/// Marker class used for assembly scanning.
/// Pass typeof(ApplicationAssemblyMarker).Assembly to MediatR and FluentValidation registration.
/// Never instantiated — exists solely as an assembly reference point.
/// </summary>
public sealed class ApplicationAssemblyMarker;