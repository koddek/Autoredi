using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Common.Interfaces;
using Samples.Common.Models;

namespace Samples.Complete.Services;

/// <summary>
/// User repository implementation as a scoped service.
/// </summary>
[Autoredi(ServiceLifetime.Scoped, typeof(IRepository<User>))]
public class UserRepository : IRepository<User>
{
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "Alice", Email = "alice@example.com" },
        new User { Id = 2, Name = "Bob", Email = "bob@example.com" },
        new User { Id = 3, Name = "Charlie", Email = "charlie@example.com" }
    };

    public User? GetById(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public IEnumerable<User> GetAll()
    {
        return _users.AsReadOnly();
    }

    public void Add(User entity)
    {
        _users.Add(entity);
    }
}
