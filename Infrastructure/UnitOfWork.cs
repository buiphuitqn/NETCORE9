using System;
using System.Collections.Generic;
using CORE_BE.Data;
using Microsoft.EntityFrameworkCore;

namespace CORE_BE.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MyDbContext _context;
        private Dictionary<Type, object> _repositories;

        public UnitOfWork(MyDbContext context)
        {
            _context = context;
        }

        public IRepository<T> GetRepository<T>()
            where T : class
        {
            _repositories ??= new Dictionary<Type, object>();

            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                var repo = new Repository<T>(_context);
                _repositories.Add(type, repo);
            }

            return (IRepository<T>)_repositories[type];
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
