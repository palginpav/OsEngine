/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Threading;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;

namespace OsEngine.OsOptimizer.Algorithms
{
    /// <summary>
    /// Interface for optimization algorithms that can be used in the OsEngine optimizer.
    /// Интерфейс для алгоритмов оптимизации, которые могут использоваться в оптимизаторе OsEngine.
    /// </summary>
    public interface IOptimizationAlgorithm
    {
        /// <summary>
        /// Name of the optimization algorithm.
        /// Название алгоритма оптимизации.
        /// </summary>
        string AlgorithmName { get; }

        /// <summary>
        /// Description of the optimization algorithm.
        /// Описание алгоритма оптимизации.
        /// </summary>
        string AlgorithmDescription { get; }

        /// <summary>
        /// Whether this algorithm supports multi-objective optimization.
        /// Поддерживает ли этот алгоритм многоцелевую оптимизацию.
        /// </summary>
        bool SupportsMultiObjective { get; }

        /// <summary>
        /// Optimize strategy parameters using the specific algorithm.
        /// Оптимизировать параметры стратегии с использованием конкретного алгоритма.
        /// </summary>
        /// <param name="parameters">List of all strategy parameters / Список всех параметров стратегии</param>
        /// <param name="parametersToOptimize">Which parameters to optimize / Какие параметры оптимизировать</param>
        /// <param name="faze">Optimization phase / Фаза оптимизации</param>
        /// <param name="maxIterations">Maximum number of iterations / Максимальное количество итераций</param>
        /// <param name="populationSize">Population size for population-based algorithms / Размер популяции для популяционных алгоритмов</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>List of optimization results / Список результатов оптимизации</returns>
        List<OptimizerReport> Optimize(
            List<IIStrategyParameter> parameters,
            List<bool> parametersToOptimize,
            OptimizerFaze faze,
            int maxIterations,
            int populationSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Get algorithm-specific parameters that can be configured.
        /// Получить специфичные для алгоритма параметры, которые можно настроить.
        /// </summary>
        /// <returns>Dictionary of parameter names and their default values / Словарь имен параметров и их значений по умолчанию</returns>
        Dictionary<string, object> GetAlgorithmParameters();

        /// <summary>
        /// Set algorithm-specific parameters.
        /// Установить специфичные для алгоритма параметры.
        /// </summary>
        /// <param name="parameters">Dictionary of parameter names and values / Словарь имен параметров и значений</param>
        void SetAlgorithmParameters(Dictionary<string, object> parameters);

        /// <summary>
        /// Event fired when optimization progress is updated.
        /// Событие, срабатывающее при обновлении прогресса оптимизации.
        /// </summary>
        event Action<int, double, string> ProgressUpdated;

        /// <summary>
        /// Event fired when optimization is completed.
        /// Событие, срабатывающее при завершении оптимизации.
        /// </summary>
        event Action<List<OptimizerReport>> OptimizationCompleted;
    }
}
