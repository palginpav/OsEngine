/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace OsEngine.Performance
{
    /// <summary>
    /// Async file operations for improved I/O performance during backtesting
    /// Асинхронные файловые операции для улучшения производительности I/O во время бэктестинга
    /// </summary>
    public static class AsyncFileOperations
    {
        /// <summary>
        /// Read all lines from a file asynchronously
        /// Асинхронно читает все строки из файла
        /// </summary>
        /// <param name="filePath">Path to the file / Путь к файлу</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>Array of lines from the file / Массив строк из файла</returns>
        public static async Task<string[]> ReadAllLinesAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return new string[0];
            }

            try
            {
                var lines = new List<string>();
                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        lines.Add(line);
                    }
                }
                return lines.ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read file {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read all lines from multiple files in parallel
        /// Параллельно читает все строки из нескольких файлов
        /// </summary>
        /// <param name="filePaths">Array of file paths / Массив путей к файлам</param>
        /// <param name="maxConcurrency">Maximum number of concurrent operations / Максимальное количество одновременных операций</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>Dictionary mapping file paths to their content / Словарь, сопоставляющий пути к файлам с их содержимым</returns>
        public static async Task<Dictionary<string, string[]>> ReadAllFilesAsync(
            string[] filePaths, 
            int maxConcurrency = 0, 
            CancellationToken cancellationToken = default)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                return new Dictionary<string, string[]>();
            }

            if (maxConcurrency <= 0)
            {
                maxConcurrency = Math.Min(Environment.ProcessorCount, filePaths.Length);
            }

            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task<KeyValuePair<string, string[]>>>();

            foreach (var filePath in filePaths)
            {
                tasks.Add(ReadSingleFileAsync(filePath, semaphore, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            var resultDict = new Dictionary<string, string[]>();

            foreach (var result in results)
            {
                if (result.Value != null)
                {
                    resultDict[result.Key] = result.Value;
                }
            }

            return resultDict;
        }

        /// <summary>
        /// Read a single file with semaphore control for concurrency
        /// Читает один файл с контролем семафора для параллелизма
        /// </summary>
        private static async Task<KeyValuePair<string, string[]>> ReadSingleFileAsync(
            string filePath, 
            SemaphoreSlim semaphore, 
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var content = await ReadAllLinesAsync(filePath, cancellationToken);
                return new KeyValuePair<string, string[]>(filePath, content);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Read file content in chunks for large files
        /// Читает содержимое файла частями для больших файлов
        /// </summary>
        /// <param name="filePath">Path to the file / Путь к файлу</param>
        /// <param name="chunkSize">Size of each chunk in lines / Размер каждой части в строках</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>Async enumerable of string arrays / Асинхронный перечислитель массивов строк</returns>
        public static async IAsyncEnumerable<string[]> ReadFileInChunksAsync(
            string filePath, 
            int chunkSize = 1000, 
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                yield break;
            }

            using (var reader = new StreamReader(filePath))
            {
                var chunk = new List<string>(chunkSize);
                string line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
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
        }

        /// <summary>
        /// Get file information asynchronously
        /// Асинхронно получает информацию о файле
        /// </summary>
        /// <param name="filePath">Path to the file / Путь к файлу</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>File information / Информация о файле</returns>
        public static async Task<FileInfo> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => new FileInfo(filePath), cancellationToken);
        }

        /// <summary>
        /// Check if file exists asynchronously
        /// Асинхронно проверяет существование файла
        /// </summary>
        /// <param name="filePath">Path to the file / Путь к файлу</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>True if file exists / True, если файл существует</returns>
        public static async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => File.Exists(filePath), cancellationToken);
        }

        /// <summary>
        /// Get directory files asynchronously
        /// Асинхронно получает файлы из директории
        /// </summary>
        /// <param name="directoryPath">Path to the directory / Путь к директории</param>
        /// <param name="searchPattern">Search pattern / Шаблон поиска</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>Array of file paths / Массив путей к файлам</returns>
        public static async Task<string[]> GetFilesAsync(
            string directoryPath, 
            string searchPattern = "*", 
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Directory.GetFiles(directoryPath, searchPattern), cancellationToken);
        }

        /// <summary>
        /// Get directory subdirectories asynchronously
        /// Асинхронно получает поддиректории
        /// </summary>
        /// <param name="directoryPath">Path to the directory / Путь к директории</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>Array of directory paths / Массив путей к директориям</returns>
        public static async Task<string[]> GetDirectoriesAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Directory.GetDirectories(directoryPath), cancellationToken);
        }

        /// <summary>
        /// Write lines to file asynchronously
        /// Асинхронно записывает строки в файл
        /// </summary>
        /// <param name="filePath">Path to the file / Путь к файлу</param>
        /// <param name="lines">Lines to write / Строки для записи</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        public static async Task WriteAllLinesAsync(
            string filePath, 
            string[] lines, 
            CancellationToken cancellationToken = default)
        {
            if (lines == null)
            {
                return;
            }

            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    foreach (var line in lines)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await writer.WriteLineAsync(line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to write file {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Copy file asynchronously
        /// Асинхронно копирует файл
        /// </summary>
        /// <param name="sourcePath">Source file path / Путь к исходному файлу</param>
        /// <param name="destinationPath">Destination file path / Путь к целевому файлу</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        public static async Task CopyFileAsync(
            string sourcePath, 
            string destinationPath, 
            CancellationToken cancellationToken = default)
        {
            const int bufferSize = 81920; // 80KB buffer for optimal performance

            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
            using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
            }
        }
    }
}
