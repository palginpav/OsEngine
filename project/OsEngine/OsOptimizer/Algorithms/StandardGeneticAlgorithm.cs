/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.Logging;

namespace OsEngine.OsOptimizer.Algorithms
{
    /// <summary>
    /// Standard Genetic Algorithm implementation for OsEngine optimizer.
    /// Стандартная реализация генетического алгоритма для оптимизатора OsEngine.
    /// </summary>
    public class StandardGeneticAlgorithm : GeneticAlgorithmBase
    {
        /// <summary>
        /// Reference to the optimizer executor for running strategy tests.
        /// Ссылка на исполнитель оптимизатора для запуска тестов стратегий.
        /// </summary>
        private OptimizerExecutor _optimizerExecutor;

        /// <summary>
        /// Reference to the optimizer master for accessing configuration.
        /// Ссылка на мастер оптимизатора для доступа к конфигурации.
        /// </summary>
        private OptimizerMaster _optimizerMaster;
        
        // Failure tracking for diagnostics
        private int _totalEvaluations = 0;
        private int _successfulEvaluations = 0;
        private int _timeoutFailures = 0;
        private int _parameterFailures = 0;
        private int _connectionFailures = 0;
        private int _otherFailures = 0;

        /// <summary>
        /// Current optimization phase.
        /// Текущая фаза оптимизации.
        /// </summary>
        private OptimizerFaze _currentFaze;

        /// <summary>
        /// Strategy parameters being optimized.
        /// Параметры стратегии, которые оптимизируются.
        /// </summary>
        private List<IIStrategyParameter> _parameters;

        /// <summary>
        /// Which parameters to optimize.
        /// Какие параметры оптимизировать.
        /// </summary>
        private List<bool> _parametersToOptimize;

        /// <summary>
        /// Initialize the standard genetic algorithm.
        /// Инициализировать стандартный генетический алгоритм.
        /// </summary>
        public StandardGeneticAlgorithm()
        {
            // Set algorithm-specific default parameters
            AlgorithmParameters["SelectionMethod"] = SelectionMethod.Tournament;
            AlgorithmParameters["TournamentSize"] = 3;
            AlgorithmParameters["EliteCount"] = 5;
            AlgorithmParameters["CrossoverRate"] = 0.8;
            AlgorithmParameters["MutationRate"] = 0.1;
            AlgorithmParameters["MutationStrength"] = 0.1;
            AlgorithmParameters["ConvergenceThreshold"] = 0.001;
            AlgorithmParameters["MaxStagnationGenerations"] = 20;
        }

        /// <summary>
        /// Algorithm name.
        /// Название алгоритма.
        /// </summary>
        public override string AlgorithmName => "Standard Genetic Algorithm";

        /// <summary>
        /// Algorithm description.
        /// Описание алгоритма.
        /// </summary>
        public override string AlgorithmDescription => 
            "Standard genetic algorithm with tournament selection, crossover, and mutation. " +
            "Suitable for most parameter optimization tasks with good convergence properties.";

        /// <summary>
        /// Whether this algorithm supports multi-objective optimization.
        /// Поддерживает ли этот алгоритм многоцелевую оптимизацию.
        /// </summary>
        public override bool SupportsMultiObjective => true;

        /// <summary>
        /// Set the optimizer executor and master for running strategy tests.
        /// Установить исполнитель и мастер оптимизатора для запуска тестов стратегий.
        /// </summary>
        /// <param name="executor">Optimizer executor / Исполнитель оптимизатора</param>
        /// <param name="master">Optimizer master / Мастер оптимизатора</param>
        public void SetOptimizerExecutor(OptimizerExecutor executor, OptimizerMaster master)
        {
            _optimizerExecutor = executor;
            _optimizerMaster = master;
        }

        /// <summary>
        /// Run the genetic algorithm optimization.
        /// Запустить оптимизацию генетическим алгоритмом.
        /// </summary>
        protected override List<OptimizerReport> RunOptimization(
            List<IIStrategyParameter> parameters,
            List<bool> parametersToOptimize,
            OptimizerFaze faze)
        {
            _parameters = parameters;
            _parametersToOptimize = parametersToOptimize;
            _currentFaze = faze;

            if (_optimizerExecutor == null)
            {
                throw new InvalidOperationException("OptimizerExecutor must be set before running optimization");
            }

            _optimizerMaster?.SendLogMessage("RunOptimization: Starting Genetic Algorithm optimization", LogMessageType.System);
            
            // Initialize the OptimizerExecutor properly like the brute-force optimizer does
            _optimizerMaster?.SendLogMessage("RunOptimization: Starting OptimizerExecutor", LogMessageType.System);
            bool startResult = _optimizerExecutor.Start(parametersToOptimize, parameters);
            if (!startResult)
            {
                _optimizerMaster?.SendLogMessage("RunOptimization: Failed to start OptimizerExecutor", LogMessageType.Error);
                return new List<OptimizerReport>();
            }
            _optimizerMaster?.SendLogMessage("RunOptimization: OptimizerExecutor started successfully", LogMessageType.System);
            
            var results = new List<OptimizerReport>();
            
            try
            {
                // Run the genetic algorithm evolution using the existing infrastructure
                results = RunGeneticEvolutionWithExecutor(parameters, parametersToOptimize, faze);
                
                _optimizerMaster?.SendLogMessage($"RunOptimization: Genetic Algorithm completed with {results.Count} results", LogMessageType.System);
            }
            catch (Exception ex)
            {
                _optimizerMaster?.SendLogMessage($"RunOptimization: Error in genetic algorithm: {ex.Message}", LogMessageType.Error);
                _optimizerMaster?.SendLogMessage($"RunOptimization: Stack trace: {ex.StackTrace}", LogMessageType.Error);
            }

            return results;
        }

        /// <summary>
        /// Run the genetic algorithm evolution process using the existing OptimizerExecutor infrastructure.
        /// Запустить процесс эволюции генетического алгоритма используя существующую инфраструктуру OptimizerExecutor.
        /// </summary>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="parametersToOptimize">Parameters to optimize / Параметры для оптимизации</param>
        /// <param name="faze">Optimization phase / Фаза оптимизации</param>
        /// <returns>List of optimization reports / Список отчетов об оптимизации</returns>
        private List<OptimizerReport> RunGeneticEvolutionWithExecutor(
            List<IIStrategyParameter> parameters,
            List<bool> parametersToOptimize,
            OptimizerFaze faze)
        {
            _optimizerMaster?.SendLogMessage("RunGeneticEvolutionWithExecutor: Starting genetic evolution", LogMessageType.System);
            
            var populationSize = (int)AlgorithmParameters.GetValueOrDefault("PopulationSize", 50);
            var maxGenerations = (int)AlgorithmParameters.GetValueOrDefault("MaxGenerations", 100);
            var mutationRate = (double)AlgorithmParameters.GetValueOrDefault("MutationRate", 0.1);
            var crossoverRate = (double)AlgorithmParameters.GetValueOrDefault("CrossoverRate", 0.8);
            
            _optimizerMaster?.SendLogMessage($"RunGeneticEvolutionWithExecutor: Population={populationSize}, Generations={maxGenerations}, Mutation={mutationRate}, Crossover={crossoverRate}", LogMessageType.System);
            
            // Initialize population
            var population = new Population(populationSize, parameters, parametersToOptimize, 0);
            var allResults = new List<OptimizerReport>();
            
            for (int generation = 0; generation < maxGenerations; generation++)
            {
                _optimizerMaster?.SendLogMessage($"RunGeneticEvolutionWithExecutor: Generation {generation + 1}/{maxGenerations}", LogMessageType.System);
                
                // Evaluate population using the existing OptimizerExecutor infrastructure
                EvaluatePopulationWithExecutor(population, faze);
                
                // Collect results from this generation
                var generationResults = population.Individuals
                    .Where(i => i.IsEvaluated && i.Report != null)
                    .Select(i => i.Report)
                    .ToList();
                
                allResults.AddRange(generationResults);
                
               // Track best individual
               var bestIndividual = population.Individuals.OrderByDescending(i => i.Fitness).FirstOrDefault();
               if (bestIndividual?.Report != null)
               {
                   _optimizerMaster?.SendLogMessage($"RunGeneticEvolutionWithExecutor: Generation {generation + 1} best fitness: {bestIndividual.Fitness:F2}", LogMessageType.System);
               }
               
               // Log failure statistics for this generation
               LogFailureStatistics(generation + 1);
                
                // Check for convergence or early stopping
                if (HasConverged(population))
                {
                    _optimizerMaster?.SendLogMessage($"RunGeneticEvolutionWithExecutor: Converged at generation {generation + 1}", LogMessageType.System);
                    break;
                }
                
                // Create next generation
                if (generation < maxGenerations - 1) // Don't create next generation for the last iteration
                {
                    var selectedIndividuals = SelectBestIndividuals(population, populationSize / 2);
                    var newGeneration = BreedNewGeneration(selectedIndividuals, populationSize, mutationRate, crossoverRate, parameters, parametersToOptimize);
                    
                    // Replace population individuals
                    population.Individuals.Clear();
                    population.Individuals.AddRange(newGeneration);
                }
            }
            
            _optimizerMaster?.SendLogMessage($"RunGeneticEvolutionWithExecutor: Completed with {allResults.Count} total results", LogMessageType.System);
            return allResults;
        }

        /// <summary>
        /// Evaluate all individuals in the current population.
        /// Оценить всех особей в текущей популяции.
        /// </summary>
        protected override void EvaluatePopulation()
        {
            if (_optimizerExecutor == null)
            {
                throw new InvalidOperationException("OptimizerExecutor must be set before running optimization");
            }

            var tasks = new List<System.Threading.Tasks.Task>();

            foreach (var individual in CurrentPopulation.Individuals)
            {
                if (!individual.IsEvaluated)
                {
                    var task = System.Threading.Tasks.Task.Run(() => EvaluateIndividual(individual));
                    tasks.Add(task);
                }
            }

            // Wait for all evaluations to complete
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray(), CancellationToken);

            // Update fitness evaluator normalization ranges
            var evaluatedIndividuals = CurrentPopulation.Individuals.Where(i => i.IsEvaluated && i.Report != null).ToList();
            if (evaluatedIndividuals.Count > 0)
            {
                var reports = evaluatedIndividuals.Select(i => i.Report).ToList();
                FitnessEvaluator.UpdateNormalizationRanges(reports);
            }
        }

        /// <summary>
        /// Evaluate a single individual by running the strategy with its parameters.
        /// Оценить одну особь, запустив стратегию с ее параметрами.
        /// </summary>
        /// <param name="individual">Individual to evaluate / Особь для оценки</param>
        private void EvaluateIndividual(Individual individual)
        {
            try
            {
                _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Starting evaluation of individual with {individual.Parameters.Count} parameters", LogMessageType.System);
                
                if (_optimizerExecutor == null || _optimizerMaster == null)
                {
                    _optimizerMaster?.SendLogMessage("EvaluateIndividual: OptimizerExecutor or OptimizerMaster is null", LogMessageType.Error);
                    individual.Fitness = 0.0;
                    individual.IsEvaluated = true;
                    return;
                }

                _optimizerMaster?.SendLogMessage("EvaluateIndividual: Validated executor and master", LogMessageType.System);

                // Create a temporary report for this individual test
                _optimizerMaster?.SendLogMessage("EvaluateIndividual: Creating OptimizerFazeReport for individual test", LogMessageType.System);
                var report = new OptimizerFazeReport();
                report.Faze = _currentFaze;
                
                _optimizerMaster?.SendLogMessage("EvaluateIndividual: Created OptimizerFazeReport, calling RunIndividualTest", LogMessageType.System);
                
                // Use the OptimizerExecutor's infrastructure to run the test
                _optimizerMaster?.SendLogMessage("EvaluateIndividual: About to call RunIndividualTest", LogMessageType.System);
                var result = RunIndividualTest(individual.Parameters, report);
                _optimizerMaster?.SendLogMessage("EvaluateIndividual: RunIndividualTest returned", LogMessageType.System);
                
                if (result != null)
                {
                    _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Individual test completed successfully, profit: {result.TotalProfit}", LogMessageType.System);
                    individual.Report = result;
                    _optimizerMaster?.SendLogMessage("EvaluateIndividual: About to calculate fitness", LogMessageType.System);
                    individual.Fitness = CalculateFitness(individual);
                    _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Calculated fitness: {individual.Fitness}", LogMessageType.System);
                }
                else
                {
                    _optimizerMaster?.SendLogMessage("EvaluateIndividual: Individual test failed - result is null", LogMessageType.Error);
                    individual.Fitness = 0.0;
                }

                _optimizerMaster?.SendLogMessage("EvaluateIndividual: Setting individual as evaluated", LogMessageType.System);
                individual.IsEvaluated = true;
                _optimizerMaster?.SendLogMessage("EvaluateIndividual: Individual evaluation completed", LogMessageType.System);
            }
            catch (Exception ex)
            {
                // Log error and set fitness to 0
                individual.Fitness = 0.0;
                individual.IsEvaluated = true;
                _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Error evaluating individual: {ex.Message}", LogMessageType.Error);
                _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Stack trace: {ex.StackTrace}", LogMessageType.Error);
            }
        }

        /// <summary>
        /// Run a test for an individual using the OptimizerExecutor infrastructure.
        /// Запустить тест для особи, используя инфраструктуру OptimizerExecutor.
        /// </summary>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="report">Report to fill with results / Отчет для заполнения результатами</param>
        /// <returns>Optimization report / Отчет об оптимизации</returns>
        private OptimizerReport RunIndividualTest(List<IIStrategyParameter> parameters, OptimizerFazeReport report)
        {
            try
            {
                _optimizerMaster?.SendLogMessage("RunIndividualTest: Starting", LogMessageType.System);
                
                if (_optimizerExecutor == null || _optimizerMaster == null)
                {
                    _optimizerMaster?.SendLogMessage("RunIndividualTest: OptimizerExecutor or OptimizerMaster is null", LogMessageType.Error);
                    return null;
                }

                _optimizerMaster?.SendLogMessage("RunIndividualTest: Validated executor and master", LogMessageType.System);

                // Create a unique bot name for this individual
                // Use a numeric format that's compatible with OptimizerReport.BotNum
                string botName = $"{Random.Next(100000, 999999)}";
                _optimizerMaster?.SendLogMessage($"RunIndividualTest: Created bot name: {botName}", LogMessageType.System);
                
                // Use reflection to access the OptimizerExecutor's StartNewBot method
                _optimizerMaster?.SendLogMessage("RunIndividualTest: Getting OptimizerExecutor type", LogMessageType.System);
                var executorType = typeof(OptimizerExecutor);
                
                _optimizerMaster?.SendLogMessage("RunIndividualTest: Looking for StartNewBot method", LogMessageType.System);
                var startNewBotMethod = executorType.GetMethod("StartNewBot", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (startNewBotMethod == null)
                {
                    _optimizerMaster?.SendLogMessage("RunIndividualTest: Failed to get StartNewBot method", LogMessageType.Error);
                    return null;
                }
                
                _optimizerMaster?.SendLogMessage("RunIndividualTest: Got StartNewBot method", LogMessageType.System);
                
                // Log parameters before calling StartNewBot
                _optimizerMaster?.SendLogMessage($"RunIndividualTest: About to call StartNewBot with {_parameters?.Count ?? 0} base parameters and {parameters?.Count ?? 0} individual parameters", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"RunIndividualTest: Report object is {(report == null ? "null" : "valid")}", LogMessageType.System);
                
                // Call StartNewBot with the individual's parameters
                _optimizerMaster?.SendLogMessage("RunIndividualTest: Calling StartNewBot method", LogMessageType.System);
                
                try
                {
                    // Start a task to call StartNewBot with a timeout
                    var startNewBotTask = Task.Run(() =>
                    {
                        startNewBotMethod.Invoke(_optimizerExecutor, new object[] { 
                            _parameters, // Use original parameters as base
                            parameters,  // Use individual's parameters as optimized parameters
                            report, 
                            botName 
                        });
                    });
                    
                    // Wait for the task to complete with a 30-second timeout
                    if (startNewBotTask.Wait(30000))
                    {
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: StartNewBot method call completed successfully", LogMessageType.System);
                    }
                    else
                    {
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: StartNewBot method call timed out after 30 seconds", LogMessageType.Error);
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: TIMEOUT CAUSE: Bot connection timeout or strategy execution timeout", LogMessageType.Error);
                        return null;
                    }
                }
                catch (Exception invokeEx)
                {
                    _optimizerMaster?.SendLogMessage($"RunIndividualTest: Exception during StartNewBot invoke: {invokeEx.Message}", LogMessageType.Error);
                    _optimizerMaster?.SendLogMessage($"RunIndividualTest: Exception type: {invokeEx.GetType().Name}", LogMessageType.Error);
                    _optimizerMaster?.SendLogMessage($"RunIndividualTest: StartNewBot invoke stack trace: {invokeEx.StackTrace}", LogMessageType.Error);
                    
                    // Log specific exception causes
                    if (invokeEx is ArgumentException)
                    {
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: ARGUMENT ERROR - Invalid parameter values or ranges", LogMessageType.Error);
                    }
                    else if (invokeEx is InvalidOperationException)
                    {
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: INVALID OPERATION - OptimizerExecutor not in correct state", LogMessageType.Error);
                    }
                    else if (invokeEx is TargetInvocationException)
                    {
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: TARGET INVOCATION ERROR - Error in StartNewBot method execution", LogMessageType.Error);
                    }
                    
                    return null;
                }
                
                _optimizerMaster?.SendLogMessage("RunIndividualTest: StartNewBot completed, waiting for test results", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"RunIndividualTest: Initial report.Reports.Count = {report.Reports.Count}", LogMessageType.System);
                
                // Wait for the test to complete and results to be populated
                // StartNewBot starts the test asynchronously, so we need to wait for completion
                DateTime startWaiting = DateTime.Now;
                int waitCount = 0;
                
                _optimizerMaster?.SendLogMessage("RunIndividualTest: Starting wait loop for test results", LogMessageType.System);
                
                while (report.Reports.Count == 0)
                {
                    Thread.Sleep(100);
                    waitCount++;
                    
                    // Log progress every 5 seconds
                    if (waitCount % 50 == 0)
                    {
                        _optimizerMaster?.SendLogMessage($"RunIndividualTest: Still waiting for test results... ({waitCount * 100}ms), report.Reports.Count = {report.Reports.Count}", LogMessageType.System);
                    }
                    
                    // Timeout after 60 seconds
                    if (startWaiting.AddSeconds(60) < DateTime.Now)
                    {
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: Test timeout after 60 seconds", LogMessageType.Error);
                        _optimizerMaster?.SendLogMessage($"RunIndividualTest: Final report.Reports.Count = {report.Reports.Count}", LogMessageType.Error);
                        _optimizerMaster?.SendLogMessage("RunIndividualTest: TIMEOUT CAUSE: Strategy execution timeout, data loading issues, or bot connection problems", LogMessageType.Error);
                        return null;
                    }
                }
                
                _optimizerMaster?.SendLogMessage($"RunIndividualTest: Got {report.Reports.Count} reports", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"RunIndividualTest: Returning first report with profit: {report.Reports[0]?.TotalProfit ?? 0}", LogMessageType.System);
                return report.Reports[0];
            }
            catch (Exception ex)
            {
                // Log error if possible
                _optimizerMaster?.SendLogMessage($"Error in RunIndividualTest: {ex.Message}", LogMessageType.Error);
                _optimizerMaster?.SendLogMessage($"Stack trace: {ex.StackTrace}", LogMessageType.Error);
                return null;
            }
        }


        /// <summary>
        /// Check if the algorithm has converged.
        /// Проверить, сошелся ли алгоритм.
        /// </summary>
        protected override bool HasConverged()
        {
            if (CurrentPopulation == null || CurrentPopulation.Individuals.Count == 0)
                return false;

            double convergenceThreshold = (double)AlgorithmParameters["ConvergenceThreshold"];
            int maxStagnationGenerations = (int)AlgorithmParameters["MaxStagnationGenerations"];

            // Check fitness standard deviation
            double stdDev = CurrentPopulation.FitnessStandardDeviation;
            if (stdDev < convergenceThreshold)
            {
                return true;
            }

            // Check for stagnation (no improvement in best fitness for several generations)
            if (BestIndividuals.Count >= maxStagnationGenerations)
            {
                var recentBest = BestIndividuals.Take(maxStagnationGenerations).ToList();
                var oldestBest = recentBest.Last();
                var newestBest = recentBest.First();

                if (Math.Abs(newestBest.Fitness - oldestBest.Fitness) < convergenceThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Set default algorithm parameters.
        /// Установить параметры алгоритма по умолчанию.
        /// </summary>
        protected override void SetDefaultParameters()
        {
            base.SetDefaultParameters();
            
            // Override with GA-specific defaults
            AlgorithmParameters["PopulationSize"] = 50;
            AlgorithmParameters["MaxGenerations"] = 100;
            AlgorithmParameters["EliteCount"] = 5;
            AlgorithmParameters["CrossoverRate"] = 0.8;
            AlgorithmParameters["MutationRate"] = 0.1;
            AlgorithmParameters["MutationStrength"] = 0.1;
            AlgorithmParameters["SelectionMethod"] = SelectionMethod.Tournament;
            AlgorithmParameters["TournamentSize"] = 3;
            AlgorithmParameters["ConvergenceThreshold"] = 0.001;
            AlgorithmParameters["MaxStagnationGenerations"] = 20;
        }

        /// <summary>
        /// Get algorithm-specific parameters.
        /// Получить специфичные для алгоритма параметры.
        /// </summary>
        public override Dictionary<string, object> GetAlgorithmParameters()
        {
            var parameters = base.GetAlgorithmParameters();
            
            // Add GA-specific parameters
            parameters["SelectionMethod"] = AlgorithmParameters["SelectionMethod"];
            parameters["TournamentSize"] = AlgorithmParameters["TournamentSize"];
            parameters["ConvergenceThreshold"] = AlgorithmParameters["ConvergenceThreshold"];
            parameters["MaxStagnationGenerations"] = AlgorithmParameters["MaxStagnationGenerations"];
            
            return parameters;
        }

        /// <summary>
        /// Set algorithm-specific parameters.
        /// Установить специфичные для алгоритма параметры.
        /// </summary>
        public override void SetAlgorithmParameters(Dictionary<string, object> parameters)
        {
            base.SetAlgorithmParameters(parameters);
            
            // Validate GA-specific parameters
            if (parameters.ContainsKey("SelectionMethod") && parameters["SelectionMethod"] is SelectionMethod selectionMethod)
            {
                AlgorithmParameters["SelectionMethod"] = selectionMethod;
            }
            
            if (parameters.ContainsKey("TournamentSize") && parameters["TournamentSize"] is int tournamentSize && tournamentSize > 0)
            {
                AlgorithmParameters["TournamentSize"] = tournamentSize;
            }
            
            if (parameters.ContainsKey("ConvergenceThreshold") && parameters["ConvergenceThreshold"] is double threshold && threshold > 0)
            {
                AlgorithmParameters["ConvergenceThreshold"] = threshold;
            }
            
            if (parameters.ContainsKey("MaxStagnationGenerations") && parameters["MaxStagnationGenerations"] is int maxStagnation && maxStagnation > 0)
            {
                AlgorithmParameters["MaxStagnationGenerations"] = maxStagnation;
            }
        }


        /// <summary>
        /// Create next generation with advanced genetic operations.
        /// Создать следующее поколение с продвинутыми генетическими операциями.
        /// </summary>
        private Population CreateNextGenerationAdvanced(
            int eliteCount, double crossoverRate, double mutationRate, 
            double mutationStrength, SelectionMethod selectionMethod)
        {
            var nextGeneration = new Population(new List<Individual>(), CurrentPopulation.Generation + 1);

            // Preserve elite individuals
            var elite = CurrentPopulation.GetTopIndividuals(eliteCount);
            foreach (var individual in elite)
            {
                nextGeneration.AddIndividual(individual.Clone());
            }

            // Create offspring to fill the rest of the population
            int offspringNeeded = CurrentPopulation.Individuals.Count - eliteCount;

            for (int i = 0; i < offspringNeeded; i++)
            {
                // Select parents using specified method
                var parents = CurrentPopulation.SelectIndividuals(2, selectionMethod);
                if (parents.Count >= 2)
                {
                    // Create offspring through crossover
                    var offspring = parents[0].Crossover(parents[1], crossoverRate);
                    
                    // Apply mutation
                    offspring = offspring.Mutate(mutationRate, mutationStrength);
                    
                    nextGeneration.AddIndividual(offspring);
                }
            }

            return nextGeneration;
        }

        /// <summary>
        /// Evaluate population using the existing OptimizerExecutor infrastructure.
        /// Оценить популяцию используя существующую инфраструктуру OptimizerExecutor.
        /// </summary>
        /// <param name="population">Population to evaluate / Популяция для оценки</param>
        /// <param name="faze">Optimization phase / Фаза оптимизации</param>
        private void EvaluatePopulationWithExecutor(Population population, OptimizerFaze faze)
        {
            _optimizerMaster?.SendLogMessage($"EvaluatePopulationWithExecutor: Evaluating {population.Individuals.Count} individuals", LogMessageType.System);
            
            foreach (var individual in population.Individuals)
            {
                if (!individual.IsEvaluated)
                {
                    EvaluateIndividualWithExecutor(individual, faze);
                }
            }
        }

        /// <summary>
        /// Evaluate individual using the existing OptimizerExecutor infrastructure.
        /// Оценить особь используя существующую инфраструктуру OptimizerExecutor.
        /// </summary>
        /// <param name="individual">Individual to evaluate / Особь для оценки</param>
        /// <param name="faze">Optimization phase / Фаза оптимизации</param>
        private void EvaluateIndividualWithExecutor(Individual individual, OptimizerFaze faze)
        {
            _totalEvaluations++;
            
            try
            {
                _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: Evaluating individual {_totalEvaluations} with {individual.Parameters.Count} parameters", LogMessageType.System);
                
                // Log parameter details for debugging
                for (int i = 0; i < individual.Parameters.Count; i++)
                {
                    var param = individual.Parameters[i];
                    if (param.Type == StrategyParameterType.Int)
                    {
                        var intParam = (StrategyParameterInt)param;
                        _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: Param {i}: {param.Name} = {intParam.ValueInt} (range: {intParam.ValueIntStart}-{intParam.ValueIntStop})", LogMessageType.System);
                    }
                    else if (param.Type == StrategyParameterType.Decimal)
                    {
                        var decParam = (StrategyParameterDecimal)param;
                        _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: Param {i}: {param.Name} = {decParam.ValueDecimal} (range: {decParam.ValueDecimalStart}-{decParam.ValueDecimalStop})", LogMessageType.System);
                    }
                    else if (param.Type == StrategyParameterType.Bool)
                    {
                        var boolParam = (StrategyParameterBool)param;
                        _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: Param {i}: {param.Name} = {boolParam.ValueBool}", LogMessageType.System);
                    }
                }
                
                // Use the existing EvaluateIndividual method from the base class
                // This will use the OptimizerExecutor infrastructure properly
                EvaluateIndividual(individual);
                
                if (individual.Report != null)
                {
                    individual.Fitness = CalculateFitness(individual);
                    _successfulEvaluations++;
                    _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: Individual test SUCCESS - fitness: {individual.Fitness:F2}, profit: {individual.Report.TotalProfit:F2}", LogMessageType.System);
                }
                else
                {
                    individual.Fitness = 0.0;
                    individual.IsEvaluated = true;
                    _otherFailures++;
                    _optimizerMaster?.SendLogMessage("EvaluateIndividualWithExecutor: Individual test FAILED - no report generated", LogMessageType.Error);
                    
                    // Log potential causes
                    _optimizerMaster?.SendLogMessage("EvaluateIndividualWithExecutor: Possible causes: bot connection timeout, strategy compilation error, invalid parameters, or data issues", LogMessageType.Error);
                }
            }
            catch (Exception ex)
            {
                individual.Fitness = 0.0;
                individual.IsEvaluated = true;
                _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: EXCEPTION during evaluation: {ex.Message}", LogMessageType.Error);
                _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: Exception type: {ex.GetType().Name}", LogMessageType.Error);
                _optimizerMaster?.SendLogMessage($"EvaluateIndividualWithExecutor: Stack trace: {ex.StackTrace}", LogMessageType.Error);
                
                // Track specific exception types
                if (ex is TimeoutException)
                {
                    _timeoutFailures++;
                    _optimizerMaster?.SendLogMessage("EvaluateIndividualWithExecutor: TIMEOUT - Bot took too long to connect or complete test", LogMessageType.Error);
                }
                else if (ex is ArgumentException)
                {
                    _parameterFailures++;
                    _optimizerMaster?.SendLogMessage("EvaluateIndividualWithExecutor: INVALID ARGUMENTS - Check parameter values and ranges", LogMessageType.Error);
                }
                else if (ex is InvalidOperationException)
                {
                    _connectionFailures++;
                    _optimizerMaster?.SendLogMessage("EvaluateIndividualWithExecutor: INVALID OPERATION - Check OptimizerExecutor state", LogMessageType.Error);
                }
                else
                {
                    _otherFailures++;
                }
            }
        }

        /// <summary>
        /// Log failure statistics for diagnostics.
        /// Логировать статистику ошибок для диагностики.
        /// </summary>
        /// <param name="generation">Current generation number / Номер текущего поколения</param>
        private void LogFailureStatistics(int generation)
        {
            if (_totalEvaluations > 0)
            {
                double successRate = (double)_successfulEvaluations / _totalEvaluations * 100;
                double timeoutRate = (double)_timeoutFailures / _totalEvaluations * 100;
                double parameterRate = (double)_parameterFailures / _totalEvaluations * 100;
                double connectionRate = (double)_connectionFailures / _totalEvaluations * 100;
                double otherRate = (double)_otherFailures / _totalEvaluations * 100;
                
                _optimizerMaster?.SendLogMessage($"=== Generation {generation} Failure Statistics ===", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"Total Evaluations: {_totalEvaluations}", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"Success Rate: {successRate:F1}% ({_successfulEvaluations}/{_totalEvaluations})", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"Timeout Failures: {timeoutRate:F1}% ({_timeoutFailures})", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"Parameter Failures: {parameterRate:F1}% ({_parameterFailures})", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"Connection Failures: {connectionRate:F1}% ({_connectionFailures})", LogMessageType.System);
                _optimizerMaster?.SendLogMessage($"Other Failures: {otherRate:F1}% ({_otherFailures})", LogMessageType.System);
                _optimizerMaster?.SendLogMessage("===============================================", LogMessageType.System);
                
                // Provide recommendations based on failure patterns
                if (timeoutRate > 20)
                {
                    _optimizerMaster?.SendLogMessage("RECOMMENDATION: High timeout rate - consider increasing timeout values or checking data availability", LogMessageType.Error);
                }
                if (parameterRate > 10)
                {
                    _optimizerMaster?.SendLogMessage("RECOMMENDATION: High parameter failure rate - check parameter ranges and validation", LogMessageType.Error);
                }
                if (connectionRate > 15)
                {
                    _optimizerMaster?.SendLogMessage("RECOMMENDATION: High connection failure rate - check OptimizerExecutor state and bot creation", LogMessageType.Error);
                }
            }
        }

        /// <summary>
        /// Select the best individuals for breeding.
        /// Выбрать лучших особей для размножения.
        /// </summary>
        /// <param name="population">Population to select from / Популяция для выбора</param>
        /// <param name="count">Number of individuals to select / Количество особей для выбора</param>
        /// <returns>Selected individuals / Выбранные особи</returns>
        private List<Individual> SelectBestIndividuals(Population population, int count)
        {
            return population.Individuals
                .OrderByDescending(i => i.Fitness)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Breed a new generation from selected individuals.
        /// Вывести новое поколение от выбранных особей.
        /// </summary>
        /// <param name="selectedIndividuals">Selected individuals for breeding / Выбранные особи для размножения</param>
        /// <param name="populationSize">Size of new population / Размер новой популяции</param>
        /// <param name="mutationRate">Mutation rate / Частота мутаций</param>
        /// <param name="crossoverRate">Crossover rate / Частота скрещивания</param>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="parametersToOptimize">Parameters to optimize / Параметры для оптимизации</param>
        /// <returns>New generation / Новое поколение</returns>
        private List<Individual> BreedNewGeneration(
            List<Individual> selectedIndividuals,
            int populationSize,
            double mutationRate,
            double crossoverRate,
            List<IIStrategyParameter> parameters,
            List<bool> parametersToOptimize)
        {
            var newGeneration = new List<Individual>();
            
            // Keep some of the best individuals (elitism)
            var eliteCount = Math.Max(1, populationSize / 10);
            newGeneration.AddRange(selectedIndividuals.Take(eliteCount));
            
            // Breed new individuals
            while (newGeneration.Count < populationSize)
            {
                var parent1 = selectedIndividuals[Random.Next(selectedIndividuals.Count)];
                var parent2 = selectedIndividuals[Random.Next(selectedIndividuals.Count)];
                
                if (Random.NextDouble() < crossoverRate)
                {
                    // Crossover - create new individual with mixed parameters
                    var child = CreateCrossoverIndividual(parent1, parent2, parameters, parametersToOptimize);
                    newGeneration.Add(child);
                }
                else
                {
                    // Clone parent with mutation
                    var child = CloneIndividual(parent1, parameters, parametersToOptimize);
                    if (Random.NextDouble() < mutationRate)
                    {
                        // Apply mutation
                        ApplyMutation(child, parameters, parametersToOptimize);
                    }
                    newGeneration.Add(child);
                }
            }
            
            return newGeneration;
        }

        /// <summary>
        /// Check if the population has converged.
        /// Проверить, сходится ли популяция.
        /// </summary>
        /// <param name="population">Population to check / Популяция для проверки</param>
        /// <returns>True if converged / True если сошлась</returns>
        private bool HasConverged(Population population)
        {
            if (population.Individuals.Count < 2) return false;
            
            var fitnesses = population.Individuals.Select(i => i.Fitness).ToList();
            var maxFitness = fitnesses.Max();
            var minFitness = fitnesses.Min();
            
            // Consider converged if fitness variation is small
            return (maxFitness - minFitness) < 0.01;
        }


        /// <summary>
        /// Apply mutation to an individual.
        /// Применить мутацию к особи.
        /// </summary>
        /// <param name="individual">Individual to mutate / Особь для мутации</param>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="parametersToOptimize">Parameters to optimize / Параметры для оптимизации</param>
        private void ApplyMutation(Individual individual, List<IIStrategyParameter> parameters, List<bool> parametersToOptimize)
        {
            try
            {
                for (int i = 0; i < individual.Parameters.Count; i++)
                {
                    if (parametersToOptimize[i])
                    {
                        var param = individual.Parameters[i];
                        
                        if (param.Type == StrategyParameterType.Int)
                        {
                            var intParam = (StrategyParameterInt)param;
                            var range = intParam.ValueIntStop - intParam.ValueIntStart;
                            var mutation = (int)(range * 0.1 * (Random.NextDouble() - 0.5)); // 10% mutation
                            intParam.ValueInt = Math.Max(intParam.ValueIntStart, 
                                Math.Min(intParam.ValueIntStop, intParam.ValueInt + mutation));
                        }
                        else if (param.Type == StrategyParameterType.Decimal)
                        {
                            var decParam = (StrategyParameterDecimal)param;
                            var range = decParam.ValueDecimalStop - decParam.ValueDecimalStart;
                            var mutation = range * 0.1m * (decimal)(Random.NextDouble() - 0.5); // 10% mutation
                            decParam.ValueDecimal = Math.Max(decParam.ValueDecimalStart, 
                                Math.Min(decParam.ValueDecimalStop, decParam.ValueDecimal + mutation));
                        }
                        else if (param.Type == StrategyParameterType.Bool)
                        {
                            var boolParam = (StrategyParameterBool)param;
                            if (Random.NextDouble() < 0.1) // 10% chance to flip
                            {
                                boolParam.ValueBool = !boolParam.ValueBool;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _optimizerMaster?.SendLogMessage($"ApplyMutation: Error applying mutation: {ex.Message}", LogMessageType.Error);
            }
        }

        /// <summary>
        /// Create a crossover individual from two parents.
        /// Создать особь скрещивания от двух родителей.
        /// </summary>
        /// <param name="parent1">First parent / Первый родитель</param>
        /// <param name="parent2">Second parent / Второй родитель</param>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="parametersToOptimize">Parameters to optimize / Параметры для оптимизации</param>
        /// <returns>Crossover individual / Особь скрещивания</returns>
        private Individual CreateCrossoverIndividual(Individual parent1, Individual parent2, List<IIStrategyParameter> parameters, List<bool> parametersToOptimize)
        {
            // Use the existing Crossover method from Individual class
            return parent1.Crossover(parent2, 0.8);
        }

        /// <summary>
        /// Clone an individual.
        /// Клонировать особь.
        /// </summary>
        /// <param name="individual">Individual to clone / Особь для клонирования</param>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="parametersToOptimize">Parameters to optimize / Параметры для оптимизации</param>
        /// <returns>Cloned individual / Клонированная особь</returns>
        private Individual CloneIndividual(Individual individual, List<IIStrategyParameter> parameters, List<bool> parametersToOptimize)
        {
            // Use the existing Clone method from Individual class
            return individual.Clone();
        }
    }
}
