using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VendingMachine.Data.Context;

namespace VendingMachine.Data.Repositories;

public class Repository<TEntity> 
    : IRepository<TEntity> 
    where TEntity : class
{
    private readonly VendingMachineDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;
    public Repository(VendingMachineDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = _dbContext.Set<TEntity>();
    }

    public void Add(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    public IQueryable<TEntity> All()
    {
        return _dbSet;
    }

    public IQueryable<TEntity> AllAsNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    public void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}
