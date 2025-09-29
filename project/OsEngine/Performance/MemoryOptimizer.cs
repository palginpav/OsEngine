/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OsEngine.Entity;

namespace OsEngine.Performance
{
    /// <summary>
    /// Memory optimization utilities for reducing garbage collection pressure
    /// Утилиты оптимизации памяти для снижения давления сборки мусора
    /// </summary>
    public static class MemoryOptimizer
    {
        /// <summary>
        /// Pre-allocated string builders for temporary string operations
        /// Предварительно выделенные построители строк для временных операций со строками
        /// </summary>
        private static readonly ConcurrentQueue<System.Text.StringBuilder> _stringBuilderPool = new();

        /// <summary>
        /// Pre-allocated lists for temporary collections
        /// Предварительно выделенные списки для временных коллекций
        /// </summary>
        private static readonly ConcurrentQueue<List<object>> _listPool = new();

        /// <summary>
        /// Pre-allocated dictionaries for temporary key-value operations
        /// Предварительно выделенные словари для временных операций ключ-значение
        /// </summary>
        private static readonly ConcurrentQueue<Dictionary<string, object>> _dictionaryPool = new();

        /// <summary>
        /// Get a reusable StringBuilder from the pool
        /// Получить переиспользуемый StringBuilder из пула
        /// </summary>
        public static System.Text.StringBuilder GetStringBuilder()
        {
            if (_stringBuilderPool.TryDequeue(out var sb))
            {
                sb.Clear();
                return sb;
            }
            return new System.Text.StringBuilder(256); // Pre-allocate reasonable capacity
        }

        /// <summary>
        /// Return a StringBuilder to the pool for reuse
        /// Вернуть StringBuilder в пул для переиспользования
        /// </summary>
        public static void ReturnStringBuilder(System.Text.StringBuilder sb)
        {
            if (sb != null && _stringBuilderPool.Count < 50) // Limit pool size
            {
                _stringBuilderPool.Enqueue(sb);
            }
        }

        /// <summary>
        /// Get a reusable List from the pool
        /// Получить переиспользуемый List из пула
        /// </summary>
        public static List<T> GetList<T>()
        {
            if (_listPool.TryDequeue(out var list))
            {
                list.Clear();
                return (List<T>)(object)list;
            }
            return new List<T>(32); // Pre-allocate reasonable capacity
        }

        /// <summary>
        /// Return a List to the pool for reuse
        /// Вернуть List в пул для переиспользования
        /// </summary>
        public static void ReturnList<T>(List<T> list)
        {
            if (list != null && _listPool.Count < 50) // Limit pool size
            {
                list.Clear();
                _listPool.Enqueue((List<object>)(object)list);
            }
        }

        /// <summary>
        /// Get a reusable Dictionary from the pool
        /// Получить переиспользуемый Dictionary из пула
        /// </summary>
        public static Dictionary<string, T> GetDictionary<T>()
        {
            if (_dictionaryPool.TryDequeue(out var dict))
            {
                dict.Clear();
                return (Dictionary<string, T>)(object)dict;
            }
            return new Dictionary<string, T>(16); // Pre-allocate reasonable capacity
        }

        /// <summary>
        /// Return a Dictionary to the pool for reuse
        /// Вернуть Dictionary в пул для переиспользования
        /// </summary>
        public static void ReturnDictionary<T>(Dictionary<string, T> dict)
        {
            if (dict != null && _dictionaryPool.Count < 50) // Limit pool size
            {
                dict.Clear();
                _dictionaryPool.Enqueue((Dictionary<string, object>)(object)dict);
            }
        }

        /// <summary>
        /// Optimize string concatenation using pooled StringBuilder
        /// Оптимизировать конкатенацию строк с использованием пула StringBuilder
        /// </summary>
        public static string ConcatStrings(params string[] strings)
        {
            if (strings == null || strings.Length == 0)
                return string.Empty;

            var sb = GetStringBuilder();
            try
            {
                foreach (var str in strings)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        sb.Append(str);
                    }
                }
                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        /// <summary>
        /// Optimize string formatting using pooled StringBuilder
        /// Оптимизировать форматирование строк с использованием пула StringBuilder
        /// </summary>
        public static string FormatString(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
                return string.Empty;

            var sb = GetStringBuilder();
            try
            {
                sb.AppendFormat(format, args);
                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        /// <summary>
        /// Clear all pools to free memory
        /// Очистить все пулы для освобождения памяти
        /// </summary>
        public static void ClearPools()
        {
            while (_stringBuilderPool.TryDequeue(out _)) { }
            while (_listPool.TryDequeue(out _)) { }
            while (_dictionaryPool.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Get pool statistics for monitoring
        /// Получить статистику пулов для мониторинга
        /// </summary>
        public static (int StringBuilderCount, int ListCount, int DictionaryCount) GetPoolStats()
        {
            return (_stringBuilderPool.Count, _listPool.Count, _dictionaryPool.Count);
        }
    }

    /// <summary>
    /// Optimized collection operations to reduce allocations
    /// Оптимизированные операции с коллекциями для уменьшения выделений памяти
    /// </summary>
    public static class CollectionOptimizer
    {
        /// <summary>
        /// Efficiently add multiple items to a list without multiple reallocations
        /// Эффективно добавить несколько элементов в список без множественных перераспределений
        /// </summary>
        public static void AddRangeOptimized<T>(List<T> list, IEnumerable<T> items)
        {
            if (list == null || items == null)
                return;

            // If items is a collection, we can optimize capacity
            if (items is ICollection<T> collection)
            {
                if (list.Capacity < list.Count + collection.Count)
                {
                    list.Capacity = list.Count + collection.Count;
                }
            }

            list.AddRange(items);
        }

        /// <summary>
        /// Efficiently create a list with pre-allocated capacity
        /// Эффективно создать список с предварительно выделенной емкостью
        /// </summary>
        public static List<T> CreateListWithCapacity<T>(int expectedSize)
        {
            return new List<T>(Math.Max(expectedSize, 16));
        }

        /// <summary>
        /// Efficiently create a dictionary with pre-allocated capacity
        /// Эффективно создать словарь с предварительно выделенной емкостью
        /// </summary>
        public static Dictionary<TKey, TValue> CreateDictionaryWithCapacity<TKey, TValue>(int expectedSize)
        {
            return new Dictionary<TKey, TValue>(Math.Max(expectedSize, 16));
        }
    }

    /// <summary>
    /// Optimized string operations to reduce allocations
    /// Оптимизированные строковые операции для уменьшения выделений памяти
    /// </summary>
    public static class StringOptimizer
    {
        /// <summary>
        /// Check if string is null or empty without creating new string
        /// Проверить, является ли строка null или пустой, не создавая новую строку
        /// </summary>
        public static bool IsNullOrEmpty(string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Check if string is null or whitespace without creating new string
        /// Проверить, является ли строка null или пробелом, не создавая новую строку
        /// </summary>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Safe string comparison that handles null values
        /// Безопасное сравнение строк, которое обрабатывает null значения
        /// </summary>
        public static bool SafeEquals(string a, string b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        /// <summary>
        /// Safe string comparison with case insensitivity
        /// Безопасное сравнение строк без учета регистра
        /// </summary>
        public static bool SafeEqualsIgnoreCase(string a, string b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
