using System.Linq.Expressions;

namespace VendingMachine.Data.Repositories;

public interface IRepository<TEntity> 
    where TEntity : class
{
    IQueryable<TEntity> All();

    IQueryable<TEntity> AllAsNoTracking();

    void Add(TEntity entity);

    void Delete(TEntity entity);

    Task<int> SaveChangesAsync();

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate);
}
