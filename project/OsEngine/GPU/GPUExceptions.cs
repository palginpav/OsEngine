/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;

namespace OsEngine.GPU
{
    /// <summary>
    /// Exception thrown when GPU initialization fails.
    /// Исключение выбрасываемое при неудачной инициализации GPU.
    /// </summary>
    public class GPUInitializationException : Exception
    {
        /// <summary>
        /// Constructor for GPU initialization exception.
        /// Конструктор исключения инициализации GPU.
        /// </summary>
        /// <param name="message">Error message / Сообщение об ошибке</param>
        public GPUInitializationException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Constructor for GPU initialization exception with inner exception.
        /// Конструктор исключения инициализации GPU с внутренним исключением.
        /// </summary>
        /// <param name="message">Error message / Сообщение об ошибке</param>
        /// <param name="innerException">Inner exception / Внутреннее исключение</param>
        public GPUInitializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    /// <summary>
    /// Exception thrown when GPU calculation fails.
    /// Исключение выбрасываемое при неудачном вычислении GPU.
    /// </summary>
    public class GPUCalculationException : Exception
    {
        /// <summary>
        /// Constructor for GPU calculation exception.
        /// Конструктор исключения вычисления GPU.
        /// </summary>
        /// <param name="message">Error message / Сообщение об ошибке</param>
        public GPUCalculationException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Constructor for GPU calculation exception with inner exception.
        /// Конструктор исключения вычисления GPU с внутренним исключением.
        /// </summary>
        /// <param name="message">Error message / Сообщение об ошибке</param>
        /// <param name="innerException">Inner exception / Внутреннее исключение</param>
        public GPUCalculationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    /// <summary>
    /// Exception thrown when GPU memory allocation fails.
    /// Исключение выбрасываемое при неудачном выделении памяти GPU.
    /// </summary>
    public class GPUMemoryException : Exception
    {
        /// <summary>
        /// Constructor for GPU memory exception.
        /// Конструктор исключения памяти GPU.
        /// </summary>
        /// <param name="message">Error message / Сообщение об ошибке</param>
        public GPUMemoryException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Constructor for GPU memory exception with inner exception.
        /// Конструктор исключения памяти GPU с внутренним исключением.
        /// </summary>
        /// <param name="message">Error message / Сообщение об ошибке</param>
        /// <param name="innerException">Inner exception / Внутреннее исключение</param>
        public GPUMemoryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
