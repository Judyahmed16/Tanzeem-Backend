using System.Linq.Expressions;


namespace Tanzeem.Domain.Contracts {
    public interface IGenericRepository<Entity> where Entity : class {

        Task<IEnumerable<Entity>> GetAllAsync();

        Task<Entity?> GetByIdAsync(int id);

        Task AddAsync(Entity entity);
        Task AddRangeAsync(IEnumerable<Entity> entities);

        void UpdateAsync(Entity entity);

        void DeleteAsync(Entity entity);
        void DeleteRangeAsync(IEnumerable<Entity> entities);
        #region new methods
        IQueryable<Entity> GetAllAsIQueryable();
        IQueryable<Entity> GetByIdAsQueryable(int id);

        Task<Entity?> GetAsync(Expression<Func<Entity, bool>> predicate, params Expression<Func<Entity, object>>[] includes);

        #endregion

    }
}
