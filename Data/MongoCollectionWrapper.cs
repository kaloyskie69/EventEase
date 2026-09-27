using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace EventEase.Data
{
    /// <summary>
    /// Native MongoDB implementation of INoSqlCollection delegating to MongoDB.Driver.IMongoCollection.
    /// </summary>
    public class MongoCollectionWrapper<T> : INoSqlCollection<T>
    {
        private readonly IMongoCollection<T> _collection;

        public MongoCollectionWrapper(IMongoCollection<T> collection)
        {
            _collection = collection;
        }

        public async Task<List<T>> FindAllAsync()
        {
            return await _collection.Find(FilterDefinition<T>.Empty).ToListAsync();
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter)
        {
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<T?> FindOneAsync(Expression<Func<T, bool>> filter)
        {
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<int> GetMaxIdAsync(Expression<Func<T, int>> idSelector)
        {
            var sort = Builders<T>.Sort.Descending(new ExpressionFieldDefinition<T, int>(idSelector));
            var doc = await _collection.Find(FilterDefinition<T>.Empty).Sort(sort).Limit(1).FirstOrDefaultAsync();
            if (doc == null) return 0;
            var compiled = idSelector.Compile();
            return compiled(doc);
        }

        public async Task InsertOneAsync(T document)
        {
            await _collection.InsertOneAsync(document);
        }

        public async Task InsertManyAsync(IEnumerable<T> documents)
        {
            if (documents != null && documents.Any())
            {
                await _collection.InsertManyAsync(documents);
            }
        }

        public async Task ReplaceOneAsync(Expression<Func<T, bool>> filter, T document)
        {
            await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });
        }

        public async Task DeleteOneAsync(Expression<Func<T, bool>> filter)
        {
            await _collection.DeleteOneAsync(filter);
        }

        public async Task DeleteManyAsync(Expression<Func<T, bool>> filter)
        {
            await _collection.DeleteManyAsync(filter);
        }

        public async Task<long> CountDocumentsAsync(Expression<Func<T, bool>> filter)
        {
            return await _collection.CountDocumentsAsync(filter);
        }
    }
}
