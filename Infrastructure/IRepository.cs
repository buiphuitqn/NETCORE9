using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CORE_BE.Infrastructure
{
    public interface IRepository<T>
        where T : class
    {
        T Add(T entity);
        void AddRange(IEnumerable<T> entities);

        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);

        void Delete(params object[] id);
        void DeleteRange(List<object> lst);
        void RemoveRange(IEnumerable<T> entities);

        T GetById(object id);

        T GetSingle(
            Expression<Func<T, bool>> whereCondition = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string[] includes = null
        );

        ICollection<T> GetAll(
            Expression<Func<T, bool>> whereCondition = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string[] includes = null
        );

        ICollection<T> GetAllPaging(
            int page,
            int pageSize,
            out int totalRow,
            out int totalPage,
            Expression<Func<T, bool>> whereCondition = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string[] includes = null
        );

        int Count(Expression<Func<T, bool>> whereCondition = null);
        bool Exists(Expression<Func<T, bool>> whereCondition = null);

        ICollection<T> ExecWithStoreProcedure(string query, params object[] parameters);
    }
}
