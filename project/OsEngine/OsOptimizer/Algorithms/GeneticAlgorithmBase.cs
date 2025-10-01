/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;

namespace OsEngine.OsOptimizer.Algorithms
{
    /// <summary>
    /// Base class for genetic algorithm implementations.
    /// Базовый класс для реализаций генетических алгоритмов.
    /// </summary>
    public abstract class GeneticAlgorithmBase : IOptimizationAlgorithm
    {
        /// <summary>
        /// Fitness evaluator for multi-objective optimization.
        /// Оценщик пригодности для многоцелевой оптимизации.
        /// </summary>
        protected FitnessEvaluator FitnessEvaluator { get; set; }

        /// <summary>
        /// Current population being evolved.
        /// Текущая популяция, которая эволюционирует.
        /// </summary>
        protected Population CurrentPopulation { get; set; }

        /// <summary>
        /// Best individuals found across all generations.
        /// Лучшие особи, найденные во всех поколениях.
        /// </summary>
        protected List<Individual> BestIndividuals { get; set; }

        /// <summary>
        /// Algorithm-specific parameters.
        /// Специфичные для алгоритма параметры.
        /// </summary>
        protected Dictionary<string, object> AlgorithmParameters { get; set; }

        /// <summary>
        /// Random number generator.
        /// Генератор случайных чисел.
        /// </summary>
        protected Random Random { get; set; }

        /// <summary>
        /// Cancellation token for stopping optimization.
        /// Токен отмены для остановки оптимизации.
        /// </summary>
        protected CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Initialize the genetic algorithm base.
        /// Инициализировать базовый генетический алгоритм.
        /// </summary>
        protected GeneticAlgorithmBase()
        {
            FitnessEvaluator = FitnessEvaluator.CreateBalanced();
            BestIndividuals = new List<Individual>();
            AlgorithmParameters = new Dictionary<string, object>();
            Random = new Random();
            
            // Set default parameters
            SetDefaultParameters();
        }

        /// <summary>
        /// Set default algorithm parameters.
        /// Установить параметры алгоритма по умолчанию.
        /// </summary>
        protected virtual void SetDefaultParameters()
        {
            AlgorithmParameters["PopulationSize"] = 50;
            AlgorithmParameters["MaxGenerations"] = 100;
            AlgorithmParameters["EliteCount"] = 5;
            AlgorithmParameters["CrossoverRate"] = 0.8;
            AlgorithmParameters["MutationRate"] = 0.1;
            AlgorithmParameters["MutationStrength"] = 0.1;
            AlgorithmParameters["SelectionMethod"] = SelectionMethod.Tournament;
            AlgorithmParameters["TournamentSize"] = 3;
            AlgorithmParameters["FitnessWeights"] = new FitnessWeights();
        }

        /// <summary>
        /// Main optimization method.
        /// Основной метод оптимизации.
        /// </summary>
        public virtual List<OptimizerReport> Optimize(
            List<IIStrategyParameter> parameters,
            List<bool> parametersToOptimize,
            OptimizerFaze faze,
            int maxIterations,
            int populationSize,
            CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            
            // Update parameters
            AlgorithmParameters["MaxGenerations"] = maxIterations;
            AlgorithmParameters["PopulationSize"] = populationSize;

            // Initialize population
            CurrentPopulation = new Population(populationSize, parameters, parametersToOptimize, 0);
            BestIndividuals.Clear();

            // Run optimization
            var results = RunOptimization(parameters, parametersToOptimize, faze);

            // Return best results
            return results;
        }

        /// <summary>
        /// Run the genetic algorithm optimization.
        /// Запустить оптимизацию генетическим алгоритмом.
        /// </summary>
        protected virtual List<OptimizerReport> RunOptimization(
            List<IIStrategyParameter> parameters,
            List<bool> parametersToOptimize,
            OptimizerFaze faze)
        {
            int maxGenerations = (int)AlgorithmParameters["MaxGenerations"];
            int eliteCount = (int)AlgorithmParameters["EliteCount"];
            double crossoverRate = (double)AlgorithmParameters["CrossoverRate"];
            double mutationRate = (double)AlgorithmParameters["MutationRate"];
            double mutationStrength = (double)AlgorithmParameters["MutationStrength"];

            var allResults = new List<OptimizerReport>();

            for (int generation = 0; generation < maxGenerations; generation++)
            {
                if (CancellationToken.IsCancellationRequested)
                    break;

                // Evaluate current population
                EvaluatePopulation();

                // Update best individuals
                UpdateBestIndividuals();

                // Report progress
                ReportProgress(generation, maxGenerations);

                // Check for convergence
                if (HasConverged())
                {
                    OnProgressUpdated?.Invoke(generation, CurrentPopulation.AverageFitness, $"Converged at generation {generation}");
                    break;
                }

                // Create next generation
                if (generation < maxGenerations - 1)
                {
                    CurrentPopulation = CurrentPopulation.CreateNextGeneration(
                        eliteCount, crossoverRate, mutationRate, mutationStrength);
                }
            }

            // Final evaluation
            EvaluatePopulation();
            UpdateBestIndividuals();

            // Convert best individuals to reports
            foreach (var individual in BestIndividuals.OrderByDescending(i => i.Fitness))
            {
                if (individual.Report != null)
                {
                    allResults.Add(individual.Report);
                }
            }

            OnOptimizationCompleted?.Invoke(allResults);
            return allResults;
        }

        /// <summary>
        /// Evaluate all individuals in the current population.
        /// Оценить всех особей в текущей популяции.
        /// </summary>
        protected virtual void EvaluatePopulation()
        {
            // This method should be implemented by derived classes
            // to integrate with the OsEngine optimization system
            throw new NotImplementedException("EvaluatePopulation must be implemented by derived classes");
        }

        /// <summary>
        /// Update the list of best individuals found so far.
        /// Обновить список лучших особей, найденных до сих пор.
        /// </summary>
        protected virtual void UpdateBestIndividuals()
        {
            var currentBest = CurrentPopulation.BestIndividual;
            if (currentBest != null && currentBest.IsEvaluated)
            {
                // Add to best individuals if it's better than existing ones
                var existingWorse = BestIndividuals.FirstOrDefault(i => i.Fitness < currentBest.Fitness);
                if (existingWorse != null)
                {
                    BestIndividuals.Remove(existingWorse);
                    BestIndividuals.Add(currentBest.Clone());
                }
                else if (BestIndividuals.Count < 10) // Keep top 10
                {
                    BestIndividuals.Add(currentBest.Clone());
                }

                // Sort by fitness
                BestIndividuals = BestIndividuals.OrderByDescending(i => i.Fitness).ToList();
            }
        }

        /// <summary>
        /// Check if the algorithm has converged.
        /// Проверить, сошелся ли алгоритм.
        /// </summary>
        protected virtual bool HasConverged()
        {
            if (CurrentPopulation == null || CurrentPopulation.Individuals.Count == 0)
                return false;

            // Check if fitness standard deviation is very low
            double stdDev = CurrentPopulation.FitnessStandardDeviation;
            double threshold = 0.001; // Very low diversity threshold

            return stdDev < threshold;
        }

        /// <summary>
        /// Report optimization progress.
        /// Сообщить о прогрессе оптимизации.
        /// </summary>
        protected virtual void ReportProgress(int currentGeneration, int maxGenerations)
        {
            double progress = (double)currentGeneration / maxGenerations * 100;
            string message = $"Generation {currentGeneration}/{maxGenerations} - " +
                           $"Best Fitness: {CurrentPopulation.BestIndividual?.Fitness:F4}, " +
                           $"Avg Fitness: {CurrentPopulation.AverageFitness:F4}, " +
                           $"Diversity: {CurrentPopulation.Diversity:F4}";

            OnProgressUpdated?.Invoke(currentGeneration, CurrentPopulation.AverageFitness, message);
        }

        /// <summary>
        /// Get algorithm-specific parameters.
        /// Получить специфичные для алгоритма параметры.
        /// </summary>
        public virtual Dictionary<string, object> GetAlgorithmParameters()
        {
            return new Dictionary<string, object>(AlgorithmParameters);
        }

        /// <summary>
        /// Set algorithm-specific parameters.
        /// Установить специфичные для алгоритма параметры.
        /// </summary>
        public virtual void SetAlgorithmParameters(Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (AlgorithmParameters.ContainsKey(kvp.Key))
                    {
                        AlgorithmParameters[kvp.Key] = kvp.Value;
                    }
                }

                // Update fitness evaluator if weights changed
                if (parameters.ContainsKey("FitnessWeights") && parameters["FitnessWeights"] is FitnessWeights weights)
                {
                    FitnessEvaluator = new FitnessEvaluator(weights);
                }
            }
        }

        /// <summary>
        /// Create a random individual for the given parameters.
        /// Создать случайную особь для заданных параметров.
        /// </summary>
        protected virtual Individual CreateRandomIndividual(List<IIStrategyParameter> parameters, List<bool> parametersToOptimize)
        {
            var individualParams = new List<IIStrategyParameter>();

            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                var shouldOptimize = i < parametersToOptimize.Count && parametersToOptimize[i];

                if (shouldOptimize)
                {
                    individualParams.Add(CreateRandomParameter(param));
                }
                else
                {
                    individualParams.Add(Individual.CopyParameter(param));
                }
            }

            return new Individual(individualParams);
        }

        /// <summary>
        /// Create a random value for a parameter.
        /// Создать случайное значение для параметра.
        /// </summary>
        protected virtual IIStrategyParameter CreateRandomParameter(IIStrategyParameter parameter)
        {
            switch (parameter.Type)
            {
                case StrategyParameterType.Bool:
                    return new StrategyParameterBool(parameter.Name, Random.NextDouble() < 0.5);

                case StrategyParameterType.Int:
                    var intParam = (StrategyParameterInt)parameter;
                    var intValue = Random.Next(intParam.ValueIntStart, intParam.ValueIntStop + 1);
                    var newIntParam = new StrategyParameterInt(parameter.Name,
                        intParam.ValueIntDefolt, intParam.ValueIntStart, intParam.ValueIntStop, intParam.ValueIntStep);
                    newIntParam.ValueInt = intValue;
                    return newIntParam;

                case StrategyParameterType.Decimal:
                    var decimalParam = (StrategyParameterDecimal)parameter;
                    var decimalValue = (decimal)(Random.NextDouble() * (double)(decimalParam.ValueDecimalStop - decimalParam.ValueDecimalStart) + (double)decimalParam.ValueDecimalStart);
                    var newDecimalParam = new StrategyParameterDecimal(parameter.Name,
                        decimalParam.ValueDecimalDefolt, decimalParam.ValueDecimalStart, decimalParam.ValueDecimalStop, decimalParam.ValueDecimalStep);
                    newDecimalParam.ValueDecimal = decimalValue;
                    return newDecimalParam;

                case StrategyParameterType.String:
                    var stringParam = (StrategyParameterString)parameter;
                    if (stringParam.ValuesString != null && stringParam.ValuesString.Count > 0)
                    {
                        var randomIndex = Random.Next(stringParam.ValuesString.Count);
                        return new StrategyParameterString(parameter.Name, stringParam.ValuesString[randomIndex], stringParam.ValuesString);
                    }
                    return new StrategyParameterString(parameter.Name, stringParam.ValueString, stringParam.ValuesString);

                case StrategyParameterType.TimeOfDay:
                    var timeParam = (StrategyParameterTimeOfDay)parameter;
                    var randomMinutes = Random.Next(0, 1440); // 0 to 1439 minutes in a day
                    var randomHour = randomMinutes / 60;
                    var randomMinute = randomMinutes % 60;
                    return new StrategyParameterTimeOfDay(parameter.Name, randomHour, randomMinute, 0, 0);

                case StrategyParameterType.CheckBox:
                    var randomCheckState = Random.NextDouble() < 0.5;
                    return new StrategyParameterCheckBox(parameter.Name, randomCheckState);

                case StrategyParameterType.DecimalCheckBox:
                    var decimalCheckParam = (StrategyParameterDecimalCheckBox)parameter;
                    var randomDecimalValue = (decimal)(Random.NextDouble() * (double)(decimalCheckParam.ValueDecimalStop - decimalCheckParam.ValueDecimalStart) + (double)decimalCheckParam.ValueDecimalStart);
                    var randomCheckState2 = Random.NextDouble() < 0.5;
                    var newDecimalCheckParam = new StrategyParameterDecimalCheckBox(parameter.Name,
                        decimalCheckParam.ValueDecimalDefolt, decimalCheckParam.ValueDecimalStart, decimalCheckParam.ValueDecimalStop, decimalCheckParam.ValueDecimalStep,
                        randomCheckState2);
                    newDecimalCheckParam.ValueDecimal = randomDecimalValue;
                    return newDecimalCheckParam;

                default:
                    return Individual.CopyParameter(parameter);
            }
        }

        /// <summary>
        /// Calculate fitness for an individual.
        /// Вычислить пригодность для особи.
        /// </summary>
        protected virtual double CalculateFitness(Individual individual)
        {
            if (individual.Report == null)
                return 0.0;

            return FitnessEvaluator.CalculateFitness(individual.Report);
        }

        /// <summary>
        /// Abstract properties that must be implemented by derived classes.
        /// Абстрактные свойства, которые должны быть реализованы производными классами.
        /// </summary>
        public abstract string AlgorithmName { get; }
        public abstract string AlgorithmDescription { get; }
        public abstract bool SupportsMultiObjective { get; }

        /// <summary>
        /// Events for progress reporting.
        /// События для отчетности о прогрессе.
        /// </summary>
        public event Action<int, double, string> OnProgressUpdated;
        public event Action<List<OptimizerReport>> OnOptimizationCompleted;

        // Implement interface events
        public event Action<int, double, string> ProgressUpdated
        {
            add { OnProgressUpdated += value; }
            remove { OnProgressUpdated -= value; }
        }

        public event Action<List<OptimizerReport>> OptimizationCompleted
        {
            add { OnOptimizationCompleted += value; }
            remove { OnOptimizationCompleted -= value; }
        }
    }
}
