using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EventEase.Data
{
    /// <summary>
    /// Generic NoSQL Document Collection interface providing CRUD operations on documents.
    /// </summary>
    /// <typeparam name="T">Document entity type</typeparam>
    public interface INoSqlCollection<T>
    {
        Task<List<T>> FindAllAsync();
        Task<List<T>> FindAsync(Expression<Func<T, bool>> filter);
        Task<T?> FindOneAsync(Expression<Func<T, bool>> filter);
        Task<int> GetMaxIdAsync(Expression<Func<T, int>> idSelector);
        Task InsertOneAsync(T document);
        Task InsertManyAsync(IEnumerable<T> documents);
        Task ReplaceOneAsync(Expression<Func<T, bool>> filter, T document);
        Task DeleteOneAsync(Expression<Func<T, bool>> filter);
        Task DeleteManyAsync(Expression<Func<T, bool>> filter);
        Task<long> CountDocumentsAsync(Expression<Func<T, bool>> filter);
    }
}
