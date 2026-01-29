using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Autoredi.Tests.Fixtures;

// --- Firebase Group ---

[Autoredi(ServiceLifetime.Singleton, group: "Firebase", priority: 100)]
public class FirebaseConfig;

[Autoredi(ServiceLifetime.Transient, group: "Firebase", priority: 10)]
public class FirebaseRepo;

[Autoredi(ServiceLifetime.Singleton, group: "Firebase")] // Default priority 0
public class FirebaseLogger;


// --- Account Group ---

[Autoredi(ServiceLifetime.Scoped, group: "Account")]
public class AccountService;


// --- Mixed / No Group ---

[Autoredi(ServiceLifetime.Transient)]
public class DefaultService;
