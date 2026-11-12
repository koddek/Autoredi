using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Tests.Fixtures;

/// <summary>
/// Open generic service contracts and implementations used to exercise
/// typeof(IFoo&lt;&gt;) registration against the generated extension class.
/// </summary>
public interface IOpenRepo<T> { }

[Autoredi(ServiceLifetime.Scoped, typeof(IOpenRepo<>))]
public sealed class OpenRepo<T> : IOpenRepo<T> { }

[Autoredi(ServiceLifetime.Scoped, typeof(IOpenRepo<>))]
public sealed class AuditedOpenRepo<T> : IOpenRepo<T> { }

[Autoredi(ServiceLifetime.Scoped, typeof(IOpenRepo<>), ServiceKeys.Email)]
public sealed class KeyedOpenRepo<T> : IOpenRepo<T> { }
