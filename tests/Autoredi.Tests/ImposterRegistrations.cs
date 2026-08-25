using Imposter.Abstractions;
using Autoredi.Tests.Fixtures;

[assembly: GenerateImposter(typeof(ITestLogService))]
[assembly: GenerateImposter(typeof(ITestMessageSender))]
[assembly: GenerateImposter(typeof(ITestSessionService))]
[assembly: GenerateImposter(typeof(ITestExternalService))]
