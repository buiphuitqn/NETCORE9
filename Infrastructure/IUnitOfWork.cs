using System;

namespace CORE_BE.Infrastructure
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> GetRepository<T>()
            where T : class;
        int Complete();
        Task<int> CompleteAsync();
    }
}
