using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.Common.Models;
using Samples.Complete.Services;

namespace Samples.Complete.Controllers;

/// <summary>
/// User service demonstrating dependency injection with multiple services.
/// </summary>
[Autoredi]
public class UserService
{
    private readonly IRepository<User> _userRepository;
    private readonly ILogger _logger;
    private readonly AppSettings _settings;

    public UserService(
        IRepository<User> userRepository,
        ILogger logger,
        AppSettings settings)
    {
        _userRepository = userRepository;
        _logger = logger;
        _settings = settings;
    }

    public User? GetUser(int id)
    {
        _logger.Log($"Getting user with ID: {id}");
        return _userRepository.GetById(id);
    }

    public IEnumerable<User> GetAllUsers()
    {
        _logger.Log("Getting all users");
        return _userRepository.GetAll();
    }

    public void DisplayAppInfo()
    {
        Console.WriteLine($"\nApplication: {_settings.ApplicationName}");
        Console.WriteLine($"Version: {_settings.Version}");
        Console.WriteLine($"Debug Mode: {_settings.IsDebugMode}");
    }
}
