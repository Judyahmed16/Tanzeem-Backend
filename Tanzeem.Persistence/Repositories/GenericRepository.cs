using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Tanzeem.Domain.Contracts;
using Tanzeem.Persistence.Data.DbContexts;

namespace Tanzeem.Persistence.Repositories {
    public class GenericRepository<Entity>(TanzeemDbContext _context) : IGenericRepository<Entity> where Entity : class {

        public async Task<Entity?> GetByIdAsync(int id) {
            return await _context.Set<Entity>().FindAsync(id);
        }

        public async Task<IEnumerable<Entity>> GetAllAsync() {
            return await _context.Set<Entity>().ToListAsync();
        }

        public async Task AddAsync(Entity entity) {
            await _context.AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<Entity> entities) {
            await _context.AddRangeAsync(entities);
        }

        public void UpdateAsync(Entity entity) {
            _context.Update(entity);
        }

        public void DeleteAsync(Entity entity) {
            _context.Remove(entity);
        }
        public void DeleteRangeAsync(IEnumerable<Entity> entities)
        {
            _context.RemoveRange(entities);
        }

        public async Task<Entity?> GetAsync(Expression<Func<Entity, bool>> predicate,
            params Expression<Func<Entity, object>>[] includes) {

            IQueryable<Entity> query = _context.Set<Entity>();

            if (includes != null) {
                foreach (var include in includes) {
                    query = query.Include(include);
                }
            }

            return await query.FirstOrDefaultAsync(predicate);
        }



        public IQueryable<Entity> GetAllAsIQueryable() {
            //we dont need to return task, we will need it when convert to list
            return _context.Set<Entity>().AsNoTracking();
        }

        public IQueryable<Entity> GetByIdAsQueryable(int id) {
            //extract PK
            var keyName = _context.Model.FindEntityType(typeof(Entity))!
                                  .FindPrimaryKey()!
                                  .Properties // if it is composite key
                                  .Select(x => x.Name) //pk name as string
                                  .Single();

            return _context.Set<Entity>() //int for cast
                           .Where(e => EF.Property<int>(e, keyName) == id);
        }

    }
}
