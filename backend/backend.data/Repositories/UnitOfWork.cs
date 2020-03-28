﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using System.Threading.Tasks;

namespace backend.data.Repositories
{
    public class UnitOfWork<TDbContext> : IUnitOfWork<TDbContext> where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;

        private IDbContextTransaction _transaction;

        public UnitOfWork(TDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        public async Task StartTransactionAsync()
        {
            _transaction = await _dbContext?.Database.BeginTransactionAsync();
        }

        public async Task RollBackAsync()
        {
            await _transaction?.RollbackAsync();
        }

        public async Task CommitAsync()
        {
            await _transaction?.CommitAsync();
        }
    }
}
