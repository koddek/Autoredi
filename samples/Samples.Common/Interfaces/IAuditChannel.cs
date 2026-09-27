namespace Samples.Common.Interfaces;

/// <summary>
/// Contract shared by several assemblies so each one can contribute its own
/// Autoredi registration. Used to prove that <c>AddAutorediServicesAll</c> keeps
/// every implementation instead of dropping the ones from other assemblies.
/// </summary>
public interface IAuditChannel
{
    string Name { get; }
}
