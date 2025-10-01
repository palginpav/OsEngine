/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace OsEngine.OsOptimizer.Algorithms
{
    /// <summary>
    /// Weights for different fitness components in multi-objective optimization.
    /// Веса для различных компонентов пригодности в многоцелевой оптимизации.
    /// </summary>
    public class FitnessWeights
    {
        /// <summary>
        /// Weight for total profit component.
        /// Вес для компонента общей прибыли.
        /// </summary>
        public double ProfitWeight { get; set; } = 0.4;

        /// <summary>
        /// Weight for maximum drawdown component (inverted - lower is better).
        /// Вес для компонента максимальной просадки (инвертированный - меньше лучше).
        /// </summary>
        public double DrawdownWeight { get; set; } = 0.2;

        /// <summary>
        /// Weight for Sharpe ratio component.
        /// Вес для компонента коэффициента Шарпа.
        /// </summary>
        public double SharpWeight { get; set; } = 0.2;

        /// <summary>
        /// Weight for profit factor component.
        /// Вес для компонента фактора прибыли.
        /// </summary>
        public double FactorWeight { get; set; } = 0.2;

        /// <summary>
        /// Normalize weights to sum to 1.0.
        /// Нормализовать веса так, чтобы их сумма равнялась 1.0.
        /// </summary>
        public void Normalize()
        {
            double total = ProfitWeight + DrawdownWeight + SharpWeight + FactorWeight;
            if (total > 0)
            {
                ProfitWeight /= total;
                DrawdownWeight /= total;
                SharpWeight /= total;
                FactorWeight /= total;
            }
        }
    }

    /// <summary>
    /// Evaluates fitness of optimization results using multiple objectives.
    /// Оценивает пригодность результатов оптимизации с использованием нескольких целей.
    /// </summary>
    public class FitnessEvaluator
    {
        private readonly FitnessWeights _weights;
        private readonly Dictionary<string, (double min, double max)> _normalizationRanges;

        /// <summary>
        /// Initialize fitness evaluator with weights.
        /// Инициализировать оценщик пригодности с весами.
        /// </summary>
        /// <param name="weights">Fitness component weights / Веса компонентов пригодности</param>
        public FitnessEvaluator(FitnessWeights weights = null)
        {
            _weights = weights ?? new FitnessWeights();
            _weights.Normalize();
            _normalizationRanges = new Dictionary<string, (double min, double max)>();
        }

        /// <summary>
        /// Calculate fitness score for an optimization report.
        /// Вычислить оценку пригодности для отчета об оптимизации.
        /// </summary>
        /// <param name="report">Optimization report / Отчет об оптимизации</param>
        /// <returns>Fitness score (higher is better) / Оценка пригодности (выше лучше)</returns>
        public double CalculateFitness(OptimizerReport report)
        {
            if (report == null)
                return 0.0;

            double profitScore = NormalizeProfit(report.TotalProfit);
            double drawdownScore = NormalizeDrawdown(report.MaxDrawDawn);
            double sharpScore = NormalizeSharp(report.SharpRatio);
            double factorScore = NormalizeFactor(report.ProfitFactor);

            return _weights.ProfitWeight * profitScore +
                   _weights.DrawdownWeight * drawdownScore +
                   _weights.SharpWeight * sharpScore +
                   _weights.FactorWeight * factorScore;
        }

        /// <summary>
        /// Update normalization ranges based on a population of reports.
        /// Обновить диапазоны нормализации на основе популяции отчетов.
        /// </summary>
        /// <param name="reports">List of optimization reports / Список отчетов об оптимизации</param>
        public void UpdateNormalizationRanges(List<OptimizerReport> reports)
        {
            if (reports == null || reports.Count == 0)
                return;

            var profits = reports.Select(r => (double)r.TotalProfit).ToList();
            var drawdowns = reports.Select(r => (double)r.MaxDrawDawn).ToList();
            var sharps = reports.Select(r => (double)r.SharpRatio).ToList();
            var factors = reports.Select(r => (double)r.ProfitFactor).ToList();

            _normalizationRanges["profit"] = (profits.Min(), profits.Max());
            _normalizationRanges["drawdown"] = (drawdowns.Min(), drawdowns.Max());
            _normalizationRanges["sharp"] = (sharps.Min(), sharps.Max());
            _normalizationRanges["factor"] = (factors.Min(), factors.Max());
        }

        /// <summary>
        /// Normalize profit value to 0-1 range.
        /// Нормализовать значение прибыли в диапазон 0-1.
        /// </summary>
        private double NormalizeProfit(decimal profit)
        {
            if (!_normalizationRanges.ContainsKey("profit"))
                return Math.Max(0, Math.Min(1, (double)profit / 100000)); // Default normalization

            var range = _normalizationRanges["profit"];
            if (range.max == range.min)
                return 0.5;

            return Math.Max(0, Math.Min(1, ((double)profit - range.min) / (range.max - range.min)));
        }

        /// <summary>
        /// Normalize drawdown value to 0-1 range (inverted - lower drawdown is better).
        /// Нормализовать значение просадки в диапазон 0-1 (инвертированно - меньшая просадка лучше).
        /// </summary>
        private double NormalizeDrawdown(decimal drawdown)
        {
            if (!_normalizationRanges.ContainsKey("drawdown"))
                return Math.Max(0, Math.Min(1, 1.0 - (double)drawdown / 100)); // Default normalization

            var range = _normalizationRanges["drawdown"];
            if (range.max == range.min)
                return 0.5;

            // Invert so lower drawdown gets higher score
            return Math.Max(0, Math.Min(1, 1.0 - ((double)drawdown - range.min) / (range.max - range.min)));
        }

        /// <summary>
        /// Normalize Sharpe ratio value to 0-1 range.
        /// Нормализовать значение коэффициента Шарпа в диапазон 0-1.
        /// </summary>
        private double NormalizeSharp(decimal sharp)
        {
            if (!_normalizationRanges.ContainsKey("sharp"))
                return Math.Max(0, Math.Min(1, ((double)sharp + 2) / 4)); // Default normalization (-2 to 2)

            var range = _normalizationRanges["sharp"];
            if (range.max == range.min)
                return 0.5;

            return Math.Max(0, Math.Min(1, ((double)sharp - range.min) / (range.max - range.min)));
        }

        /// <summary>
        /// Normalize profit factor value to 0-1 range.
        /// Нормализовать значение фактора прибыли в диапазон 0-1.
        /// </summary>
        private double NormalizeFactor(decimal factor)
        {
            if (!_normalizationRanges.ContainsKey("factor"))
                return Math.Max(0, Math.Min(1, (double)factor / 3)); // Default normalization (0 to 3)

            var range = _normalizationRanges["factor"];
            if (range.max == range.min)
                return 0.5;

            return Math.Max(0, Math.Min(1, ((double)factor - range.min) / (range.max - range.min)));
        }

        /// <summary>
        /// Get current fitness weights.
        /// Получить текущие веса пригодности.
        /// </summary>
        public FitnessWeights GetWeights() => _weights;

        /// <summary>
        /// Create a fitness evaluator optimized for profit maximization.
        /// Создать оценщик пригодности, оптимизированный для максимизации прибыли.
        /// </summary>
        public static FitnessEvaluator CreateProfitOptimized()
        {
            return new FitnessEvaluator(new FitnessWeights
            {
                ProfitWeight = 0.6,
                DrawdownWeight = 0.2,
                SharpWeight = 0.1,
                FactorWeight = 0.1
            });
        }

        /// <summary>
        /// Create a fitness evaluator optimized for risk-adjusted returns.
        /// Создать оценщик пригодности, оптимизированный для доходности с учетом риска.
        /// </summary>
        public static FitnessEvaluator CreateRiskAdjusted()
        {
            return new FitnessEvaluator(new FitnessWeights
            {
                ProfitWeight = 0.3,
                DrawdownWeight = 0.3,
                SharpWeight = 0.3,
                FactorWeight = 0.1
            });
        }

        /// <summary>
        /// Create a fitness evaluator with balanced objectives.
        /// Создать оценщик пригодности со сбалансированными целями.
        /// </summary>
        public static FitnessEvaluator CreateBalanced()
        {
            return new FitnessEvaluator(new FitnessWeights
            {
                ProfitWeight = 0.4,
                DrawdownWeight = 0.2,
                SharpWeight = 0.2,
                FactorWeight = 0.2
            });
        }
    }
}
