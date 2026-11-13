using System.Data;

namespace Domain.IRepositories
{
    public interface IUnitOfWork
    {
        Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default);
    }
}
