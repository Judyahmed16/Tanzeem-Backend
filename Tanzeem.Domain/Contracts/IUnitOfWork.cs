using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Contracts {
    public interface IUnitOfWork {
    
        IGenericRepository<Entity> GetRepository<Entity>() where Entity : class;

        Task<int> SaveChangesAsync();

        Task<IDbContextTransaction> BeginTransactionAsync();


    }
}
