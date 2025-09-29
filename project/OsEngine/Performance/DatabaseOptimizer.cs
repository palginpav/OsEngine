/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using LiteDB;
using OsEngine.Logging;

namespace OsEngine.Performance
{
    /// <summary>
    /// Optimized database operations for improved performance during backtesting and trading
    /// Оптимизированные операции с базой данных для улучшения производительности во время бэктестинга и торговли
    /// </summary>
    public static class DatabaseOptimizer
    {
        private static readonly Dictionary<string, LiteDatabase> _connectionPool = new Dictionary<string, LiteDatabase>();
        private static readonly object _poolLock = new object();
        private const int MaxPoolSize = 10;
        private const int BatchSize = 1000;

        /// <summary>
        /// Gets or creates a database connection from the pool
        /// Получает или создает соединение с базой данных из пула
        /// </summary>
        /// <param name="databasePath">Path to the database file / Путь к файлу базы данных</param>
        /// <returns>LiteDatabase connection / Соединение LiteDatabase</returns>
        public static LiteDatabase GetConnection(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                throw new ArgumentException("Database path cannot be null or empty", nameof(databasePath));
            }

            lock (_poolLock)
            {
                if (_connectionPool.TryGetValue(databasePath, out LiteDatabase existingConnection))
                {
                    return existingConnection;
                }

                if (_connectionPool.Count >= MaxPoolSize)
                {
                    // Remove oldest connection if pool is full
                    // Удаляем самое старое соединение, если пул заполнен
                    var oldestKey = _connectionPool.Keys.First();
                    _connectionPool[oldestKey]?.Dispose();
                    _connectionPool.Remove(oldestKey);
                }

                // Ensure directory exists
                // Убеждаемся, что директория существует
                string directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var connection = new LiteDatabase(databasePath);
                _connectionPool[databasePath] = connection;
                return connection;
            }
        }

        /// <summary>
        /// Optimized bulk insert operation for orders
        /// Оптимизированная операция массовой вставки для ордеров
        /// </summary>
        /// <typeparam name="T">Type of objects to insert / Тип объектов для вставки</typeparam>
        /// <param name="databasePath">Path to the database / Путь к базе данных</param>
        /// <param name="collectionName">Name of the collection / Имя коллекции</param>
        /// <param name="items">Items to insert / Элементы для вставки</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        public static async Task BulkInsertAsync<T>(
            string databasePath, 
            string collectionName, 
            IEnumerable<T> items, 
            CancellationToken cancellationToken = default)
        {
            if (items == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                using (var db = GetConnection(databasePath))
                {
                    var collection = db.GetCollection<T>(collectionName);
                    
                    // Use bulk insert for better performance
                    // Используем массовую вставку для лучшей производительности
                    collection.InsertBulk(items);
                    
                    // Ensure index exists for better query performance
                    // Убеждаемся, что индекс существует для лучшей производительности запросов
                    var properties = typeof(T).GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.Name.Contains("Id") || prop.Name.Contains("Number"))
                        {
                            try
                            {
                                collection.EnsureIndex(prop.Name);
                            }
                            catch
                            {
                                // Index might already exist, ignore error
                                // Индекс может уже существовать, игнорируем ошибку
                            }
                        }
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Optimized bulk upsert operation (insert or update)
        /// Оптимизированная операция массового upsert (вставка или обновление)
        /// </summary>
        /// <typeparam name="T">Type of objects / Тип объектов</typeparam>
        /// <param name="databasePath">Path to the database / Путь к базе данных</param>
        /// <param name="collectionName">Name of the collection / Имя коллекции</param>
        /// <param name="items">Items to upsert / Элементы для upsert</param>
        /// <param name="keySelector">Function to select the key for comparison / Функция для выбора ключа для сравнения</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        public static async Task BulkUpsertAsync<T>(
            string databasePath, 
            string collectionName, 
            IEnumerable<T> items, 
            Func<T, object> keySelector,
            CancellationToken cancellationToken = default)
        {
            if (items == null || keySelector == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                using (var db = GetConnection(databasePath))
                {
                    var collection = db.GetCollection<T>(collectionName);
                    
                    // Process items in batches for better memory management
                    // Обрабатываем элементы пакетами для лучшего управления памятью
                    var batches = items.Batch(BatchSize);
                    
                    foreach (var batch in batches)
                    {
                        var batchList = batch.ToList();
                        
                        foreach (var item in batchList)
                        {
                            var key = keySelector(item);
                            var existing = collection.FindById(new BsonValue(key));
                            
                            if (existing != null)
                            {
                                collection.Update(item);
                            }
                            else
                            {
                                collection.Insert(item);
                            }
                        }
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Optimized query with pagination
        /// Оптимизированный запрос с пагинацией
        /// </summary>
        /// <typeparam name="T">Type of objects / Тип объектов</typeparam>
        /// <param name="databasePath">Path to the database / Путь к базе данных</param>
        /// <param name="collectionName">Name of the collection / Имя коллекции</param>
        /// <param name="query">Query to execute / Запрос для выполнения</param>
        /// <param name="skip">Number of items to skip / Количество элементов для пропуска</param>
        /// <param name="take">Number of items to take / Количество элементов для взятия</param>
        /// <returns>Paginated results / Результаты с пагинацией</returns>
        public static async Task<List<T>> QueryWithPaginationAsync<T>(
            string databasePath, 
            string collectionName, 
            Query query = null, 
            int skip = 0, 
            int take = 1000)
        {
            return await Task.Run(() =>
            {
                using (var db = GetConnection(databasePath))
                {
                    var collection = db.GetCollection<T>(collectionName);
                    
                    if (query == null)
                    {
                        return collection.Find(Query.All(), skip, take).ToList();
                    }
                    
                    return collection.Find(query, skip, take).ToList();
                }
            });
        }

        /// <summary>
        /// Optimized delete operation with batch processing
        /// Оптимизированная операция удаления с пакетной обработкой
        /// </summary>
        /// <param name="databasePath">Path to the database / Путь к базе данных</param>
        /// <param name="collectionName">Name of the collection / Имя коллекции</param>
        /// <param name="query">Query to identify items to delete / Запрос для идентификации элементов для удаления</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        public static async Task<int> DeleteBatchAsync(
            string databasePath, 
            string collectionName, 
            Query query,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                using (var db = GetConnection(databasePath))
                {
                    var collection = db.GetCollection(collectionName);
                    // Use DeleteAll for Query.All() or implement specific delete logic
                    // Используем DeleteAll для Query.All() или реализуем специфическую логику удаления
                    if (query == Query.All())
                    {
                        return collection.DeleteAll();
                    }
                    else
                    {
                        // For other queries, we'll need to find and delete individually
                        // Для других запросов, нам нужно найти и удалить по отдельности
                        var items = collection.Find(query);
                        int deletedCount = 0;
                        foreach (var item in items)
                        {
                            collection.Delete(item["_id"]);
                            deletedCount++;
                        }
                        return deletedCount;
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Optimized count operation
        /// Оптимизированная операция подсчета
        /// </summary>
        /// <param name="databasePath">Path to the database / Путь к базе данных</param>
        /// <param name="collectionName">Name of the collection / Имя коллекции</param>
        /// <param name="query">Query to count / Запрос для подсчета</param>
        /// <returns>Number of items / Количество элементов</returns>
        public static async Task<long> CountAsync(
            string databasePath, 
            string collectionName, 
            Query query = null)
        {
            return await Task.Run(() =>
            {
                using (var db = GetConnection(databasePath))
                {
                    var collection = db.GetCollection(collectionName);
                    
                    if (query == null)
                    {
                        return collection.Count();
                    }
                    
                    return collection.Count(query);
                }
            });
        }

        /// <summary>
        /// Closes all database connections in the pool
        /// Закрывает все соединения с базой данных в пуле
        /// </summary>
        public static void CloseAllConnections()
        {
            lock (_poolLock)
            {
                foreach (var connection in _connectionPool.Values)
                {
                    connection?.Dispose();
                }
                _connectionPool.Clear();
            }
        }

        /// <summary>
        /// Gets statistics about the connection pool
        /// Получает статистику о пуле соединений
        /// </summary>
        /// <returns>Pool statistics / Статистика пула</returns>
        public static (int ConnectionCount, List<string> DatabasePaths) GetPoolStats()
        {
            lock (_poolLock)
            {
                return (_connectionPool.Count, _connectionPool.Keys.ToList());
            }
        }
    }

    /// <summary>
    /// Extension methods for LINQ operations
    /// Методы расширения для операций LINQ
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Splits a collection into batches of specified size
        /// Разделяет коллекцию на пакеты указанного размера
        /// </summary>
        /// <typeparam name="T">Type of items / Тип элементов</typeparam>
        /// <param name="source">Source collection / Исходная коллекция</param>
        /// <param name="batchSize">Size of each batch / Размер каждого пакета</param>
        /// <returns>Batched collection / Коллекция пакетов</returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
            }

            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}
