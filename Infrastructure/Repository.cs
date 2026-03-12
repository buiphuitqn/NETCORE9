using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CORE_BE.Infrastructure
{
    public class Repository<T> : IRepository<T>
        where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public T Add(T entity)
        {
            _dbSet.Add(entity);
            return entity;
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _dbSet.AddRange(entities);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        // public void Delete(object id)
        // {
        //     var entity = _dbSet.Find(id);
        //     if (entity != null)
        //         _dbSet.Remove(entity);
        // }
        public void Delete(params object[] ids)
        {
            var entity = _dbSet.Find(ids);
            if (entity != null)
                _dbSet.Remove(entity);
        }

        public void DeleteRange(List<object> lst)
        {
            foreach (var id in lst)
                Delete(id);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public T GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public T GetSingle(
            Expression<Func<T, bool>> whereCondition = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string[] includes = null
        )
        {
            IQueryable<T> query = _dbSet;

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            if (whereCondition != null)
                query = query.Where(whereCondition);

            return orderBy != null ? orderBy(query).FirstOrDefault() : query.FirstOrDefault();
        }

        public ICollection<T> GetAll(
            Expression<Func<T, bool>> whereCondition = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string[] includes = null
        )
        {
            IQueryable<T> query = _dbSet;

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            if (whereCondition != null)
                query = query.Where(whereCondition);

            return orderBy != null ? orderBy(query).ToList() : query.ToList();
        }

        public ICollection<T> GetAllPaging(
            int page,
            int pageSize,
            out int totalRow,
            out int totalPage,
            Expression<Func<T, bool>> whereCondition = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string[] includes = null
        )
        {
            IQueryable<T> query = _dbSet;

            // Include BEFORE filtering and paging for correct query generation
            if (includes != null && includes.Length > 0)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            if (whereCondition != null)
                query = query.Where(whereCondition);

            if (orderBy != null)
                query = orderBy(query);

            totalRow = query.Count();
            totalPage = (int)Math.Ceiling((double)totalRow / pageSize);

            query = query.Skip((page - 1) * pageSize).Take(pageSize);
            return query.AsNoTracking().ToList();
        }

        public int Count(Expression<Func<T, bool>> whereCondition = null)
        {
            return whereCondition == null ? _dbSet.Count() : _dbSet.Count(whereCondition);
        }

        public bool Exists(Expression<Func<T, bool>> whereCondition = null)
        {
            return whereCondition == null ? _dbSet.Any() : _dbSet.Any(whereCondition);
        }

        public ICollection<T> ExecWithStoreProcedure(string query, params object[] parameters)
        {
            return _dbSet.FromSqlRaw(query, parameters).ToList();
        }
    }
}
