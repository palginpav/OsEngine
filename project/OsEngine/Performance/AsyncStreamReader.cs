/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OsEngine.Performance
{
    /// <summary>
    /// High-performance async stream reader with buffering for backtesting data
    /// Высокопроизводительный асинхронный читатель потоков с буферизацией для данных бэктестинга
    /// </summary>
    public class AsyncStreamReader : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly string _filePath;
        private readonly int _bufferSize;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the AsyncStreamReader
        /// Инициализирует новый экземпляр AsyncStreamReader
        /// </summary>
        /// <param name="filePath">Path to the file to read / Путь к файлу для чтения</param>
        /// <param name="bufferSize">Buffer size for reading / Размер буфера для чтения</param>
        public AsyncStreamReader(string filePath, int bufferSize = 65536) // 64KB buffer
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _bufferSize = bufferSize;
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            _reader = new StreamReader(filePath, System.Text.Encoding.UTF8, true, _bufferSize);
        }

        /// <summary>
        /// Reads a line asynchronously
        /// Асинхронно читает строку
        /// </summary>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>The next line from the stream, or null if end of stream / Следующая строка из потока или null, если конец потока</returns>
        public async Task<string> ReadLineAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncStreamReader));
            }

            try
            {
                return await _reader.ReadLineAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read line from {_filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads all remaining lines asynchronously
        /// Асинхронно читает все оставшиеся строки
        /// </summary>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>Array of all remaining lines / Массив всех оставшихся строк</returns>
        public async Task<string[]> ReadAllLinesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncStreamReader));
            }

            var lines = new System.Collections.Generic.List<string>();
            string line;

            while ((line = await ReadLineAsync(cancellationToken)) != null)
            {
                lines.Add(line);
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Reads lines in chunks for better memory management
        /// Читает строки частями для лучшего управления памятью
        /// </summary>
        /// <param name="chunkSize">Number of lines per chunk / Количество строк в части</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>Async enumerable of string arrays / Асинхронный перечислитель массивов строк</returns>
        public async System.Collections.Generic.IAsyncEnumerable<string[]> ReadLinesInChunksAsync(
            int chunkSize = 1000,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncStreamReader));
            }

            var chunk = new System.Collections.Generic.List<string>(chunkSize);
            string line;

            while ((line = await ReadLineAsync(cancellationToken)) != null)
            {
                chunk.Add(line);

                if (chunk.Count >= chunkSize)
                {
                    yield return chunk.ToArray();
                    chunk.Clear();
                }
            }

            // Yield remaining lines if any
            if (chunk.Count > 0)
            {
                yield return chunk.ToArray();
            }
        }

        /// <summary>
        /// Checks if the end of stream has been reached
        /// Проверяет, достигнут ли конец потока
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(AsyncStreamReader));
                }
                return _reader.EndOfStream;
            }
        }

        /// <summary>
        /// Gets the current position in the stream
        /// Получает текущую позицию в потоке
        /// </summary>
        public long Position
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(AsyncStreamReader));
                }
                return _reader.BaseStream.Position;
            }
        }

        /// <summary>
        /// Gets the length of the stream
        /// Получает длину потока
        /// </summary>
        public long Length
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(AsyncStreamReader));
                }
                return _reader.BaseStream.Length;
            }
        }

        /// <summary>
        /// Resets the reader to the beginning of the file
        /// Сбрасывает читатель в начало файла
        /// </summary>
        public void Reset()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncStreamReader));
            }

            _reader.BaseStream.Position = 0;
            _reader.DiscardBufferedData();
        }

        /// <summary>
        /// Seeks to a specific position in the stream
        /// Переходит к определенной позиции в потоке
        /// </summary>
        /// <param name="position">Position to seek to / Позиция для перехода</param>
        public void Seek(long position)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncStreamReader));
            }

            _reader.BaseStream.Position = position;
            _reader.DiscardBufferedData();
        }

        /// <summary>
        /// Disposes the reader and releases resources
        /// Освобождает читатель и ресурсы
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// Защищенный метод освобождения
        /// </summary>
        /// <param name="disposing">Whether disposing managed resources / Освобождать ли управляемые ресурсы</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reader?.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// Финализатор
        /// </summary>
        ~AsyncStreamReader()
        {
            Dispose(false);
        }
    }
}
