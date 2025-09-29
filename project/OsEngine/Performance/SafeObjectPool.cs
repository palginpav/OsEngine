/*
 * Your rights to use code governed by this license http://o-s-a.net/doc/license_simple_engine.pdf
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Candles;

namespace OsEngine.Performance
{
    /// <summary>
    /// Safe object pool for temporary objects that are created and destroyed quickly.
    /// Only use for objects that have a short, well-defined lifecycle.
    /// 
    /// Безопасный пул объектов для временных объектов, которые создаются и уничтожаются быстро.
    /// Используйте только для объектов с коротким, четко определенным жизненным циклом.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public class SafeObjectPool<T> where T : new()
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly Action<T> _resetAction;
        private readonly int _maxSize;
        private int _currentSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeObjectPool{T}"/> class.
        /// </summary>
        /// <param name="resetAction">An action to reset the object's state when it's returned to the pool.</param>
        /// <param name="initialSize">The initial number of objects to create in the pool.</param>
        /// <param name="maxSize">The maximum number of objects the pool can hold.</param>
        public SafeObjectPool(Action<T> resetAction = null, int initialSize = 0, int maxSize = 1000)
        {
            _objects = new ConcurrentQueue<T>();
            _resetAction = resetAction;
            _maxSize = maxSize;
            _currentSize = 0;

            for (int i = 0; i < initialSize; i++)
            {
                _objects.Enqueue(new T());
                _currentSize++;
            }
        }

        /// <summary>
        /// Retrieves an object from the pool. If the pool is empty, a new object is created.
        /// </summary>
        /// <returns>An object of type T.</returns>
        public T Get()
        {
            if (_objects.TryDequeue(out T item))
            {
                return item;
            }
            return new T();
        }

        /// <summary>
        /// Returns an object to the pool. The object's state is reset if a reset action was provided.
        /// IMPORTANT: Only return objects that are no longer referenced anywhere else!
        /// </summary>
        /// <param name="item">The object to return to the pool.</param>
        public void Return(T item)
        {
            if (item == null || _currentSize >= _maxSize)
            {
                return; // Don't pool null objects or if pool is full
            }

            _resetAction?.Invoke(item);
            _objects.Enqueue(item);
            _currentSize++;
        }

        /// <summary>
        /// Clears the pool and resets the current size.
        /// </summary>
        public void Clear()
        {
            while (_objects.TryDequeue(out _))
            {
                // Empty the queue
            }
            _currentSize = 0;
        }
    }

    /// <summary>
    /// Static class to hold various safe object pools for temporary objects only.
    /// 
    /// Статический класс для хранения различных безопасных пулов объектов только для временных объектов.
    /// </summary>
    public static class SafeObjectPools
    {
        /// <summary>
        /// Pool for temporary Trade objects used in data loading and processing.
        /// These objects are created, used briefly, and then discarded.
        /// 
        /// Пул для временных объектов Trade, используемых при загрузке и обработке данных.
        /// Эти объекты создаются, используются кратковременно, а затем отбрасываются.
        /// </summary>
        public static readonly SafeObjectPool<Entity.Trade> TemporaryTradePool = new SafeObjectPool<Entity.Trade>(
            resetAction: trade =>
            {
                // Reset trade properties to default values
                // Сбрасываем свойства сделки к значениям по умолчанию
                trade.SecurityNameCode = null;
                trade.Id = null;
                trade.IdInTester = 0;
                trade.Volume = 0;
                trade.Price = 0;
                trade.Time = DateTime.MinValue;
                trade.MicroSeconds = 0;
                trade.Side = Entity.Side.None;
                trade.OpenInterest = 0;
                trade.TimeFrameInTester = Entity.TimeFrame.Sec1;
                trade.Bid = 0;
                trade.Ask = 0;
                trade.BidsVolume = 0;
                trade.AsksVolume = 0;
            },
            maxSize: 2000
        );

        /// <summary>
        /// Pool for temporary Candle objects used in data loading and processing.
        /// These objects are created, used briefly, and then discarded.
        /// 
        /// Пул для временных объектов Candle, используемых при загрузке и обработке данных.
        /// Эти объекты создаются, используются кратковременно, а затем отбрасываются.
        /// </summary>
        public static readonly SafeObjectPool<Entity.Candle> TemporaryCandlePool = new SafeObjectPool<Entity.Candle>(
            resetAction: candle =>
            {
                // Reset candle properties to default values
                // Сбрасываем свойства свечи к значениям по умолчанию
                candle.TimeStart = DateTime.MinValue;
                candle.Open = 0;
                candle.High = 0;
                candle.Low = 0;
                candle.Close = 0;
                candle.Volume = 0;
                candle.OpenInterest = 0;
                candle.State = Entity.CandleState.None;
                candle.Trades?.Clear();
            },
            maxSize: 1000
        );

        /// <summary>
        /// Pool for temporary List&lt;Trade&gt; objects used in data processing.
        /// 
        /// Пул для временных объектов List&lt;Trade&gt;, используемых при обработке данных.
        /// </summary>
        public static readonly SafeObjectPool<List<Entity.Trade>> TemporaryTradeListPool = new SafeObjectPool<List<Entity.Trade>>(
            resetAction: list => list.Clear(),
            maxSize: 500
        );
    }
}
