namespace Samples.Common.Interfaces;

/// <summary>
/// Generic repository interface for data access demonstrations.
/// </summary>
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
    void Add(T entity);
}
