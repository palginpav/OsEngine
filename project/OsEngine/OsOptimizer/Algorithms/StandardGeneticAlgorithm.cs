using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.OsOptimizer.OptEntity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Tab.Internal;

namespace OsEngine.OsOptimizer.Algorithms
{
    /// <summary>
    /// Standard Genetic Algorithm implementation for strategy optimization.
    /// Стандартная реализация генетического алгоритма для оптимизации стратегий.
    /// </summary>
    public class StandardGeneticAlgorithm : IOptimizationAlgorithm, IDisposable
    {
        #region Fields

        private OptimizerExecutor _optimizerExecutor;
        private OptimizerMaster _optimizerMaster;
        private Random _random;
        private Population _population;
        private int _currentFaze;
        private int _zeroTradeBots;
        private OptimizerFaze _currentOptimizerFaze;
        private bool _needToStop;
        private int _threadsCount;
        private int _originalPopulationSize;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the Standard Genetic Algorithm.
        /// Инициализировать стандартный генетический алгоритм.
        /// </summary>
        public StandardGeneticAlgorithm()
        {
            _random = new Random();
            _zeroTradeBots = 0;
        }

        #endregion

        #region IOptimizationAlgorithm Implementation

        /// <summary>
        /// Algorithm name.
        /// Название алгоритма.
        /// </summary>
        public string AlgorithmName => "Standard Genetic Algorithm";

        /// <summary>
        /// Algorithm description.
        /// Описание алгоритма.
        /// </summary>
        public string AlgorithmDescription => "Standard genetic algorithm with selection, crossover, and mutation operations";

        /// <summary>
        /// Whether this algorithm supports multi-objective optimization.
        /// Поддерживает ли этот алгоритм многоцелевую оптимизацию.
        /// </summary>
        public bool SupportsMultiObjective => false;

        /// <summary>
        /// Default algorithm parameters.
        /// Параметры алгоритма по умолчанию.
        /// </summary>
        public Dictionary<string, object> DefaultParameters => new Dictionary<string, object>
        {
            { "PopulationSize", 50 },
            { "MaxGenerations", 100 },
            { "CrossoverRate", 0.8 },
            { "MutationRate", 0.1 },
            { "ElitismCount", 5 },
            { "SelectionMethod", SelectionMethod.Tournament },
            { "TournamentSize", 3 }
        };

        /// <summary>
        /// Set the optimizer executor for bot testing.
        /// Установить исполнитель оптимизатора для тестирования ботов.
        /// </summary>
        /// <param name="executor">Optimizer executor / Исполнитель оптимизатора</param>
        /// <param name="master">Optimizer master / Мастер оптимизатора</param>
        public void SetOptimizerExecutor(OptimizerExecutor executor, OptimizerMaster master)
        {
            _optimizerExecutor = executor;
            _optimizerMaster = master;
        }

        /// <summary>
        /// Get algorithm-specific parameters that can be configured.
        /// Получить специфичные для алгоритма параметры, которые можно настроить.
        /// </summary>
        /// <returns>Dictionary of parameter names and their default values / Словарь имен параметров и их значений по умолчанию</returns>
        public Dictionary<string, object> GetAlgorithmParameters()
        {
            return new Dictionary<string, object>(DefaultParameters);
        }

        /// <summary>
        /// Set algorithm parameters.
        /// Установить параметры алгоритма.
        /// </summary>
        /// <param name="parameters">Algorithm parameters / Параметры алгоритма</param>
        public void SetAlgorithmParameters(Dictionary<string, object> parameters)
        {
            // Parameters are stored in the population when it's created
        }

        /// <summary>
        /// Stop the optimization process.
        /// Остановить процесс оптимизации.
        /// </summary>
        public void Stop()
        {
            _needToStop = true;
            _optimizerMaster?.SendLogMessage("StandardGeneticAlgorithm: Stop requested", LogMessageType.System);
        }

        /// <summary>
        /// Event fired when optimization progress is updated.
        /// Событие, срабатывающее при обновлении прогресса оптимизации.
        /// </summary>
        public event Action<int, double, string> ProgressUpdated;

        /// <summary>
        /// Event fired when optimization is completed.
        /// Событие, срабатывающее при завершении оптимизации.
        /// </summary>
        public event Action<List<OptimizerReport>> OptimizationCompleted;

        /// <summary>
        /// Run the optimization process.
        /// Запустить процесс оптимизации.
        /// </summary>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="parametersToOptimize">Parameters to optimize / Параметры для оптимизации</param>
        /// <param name="faze">Optimization phase / Фаза оптимизации</param>
        /// <param name="maxIterations">Maximum number of iterations / Максимальное количество итераций</param>
        /// <param name="populationSize">Population size for population-based algorithms / Размер популяции для популяционных алгоритмов</param>
        /// <param name="cancellationToken">Cancellation token / Токен отмены</param>
        /// <returns>List of optimization reports / Список отчетов об оптимизации</returns>
        public List<OptimizerReport> Optimize(
            List<IIStrategyParameter> parameters,
            List<bool> parametersToOptimize,
            OptimizerFaze faze,
            int maxIterations,
            int populationSize,
            CancellationToken cancellationToken)
        {
            if (_optimizerExecutor == null)
            {
                throw new InvalidOperationException("OptimizerExecutor must be set before running optimization");
            }

            
            // Initialize stop flag and thread count
            _needToStop = false;
            _threadsCount = _optimizerMaster.ThreadsCount;
            _currentOptimizerFaze = faze;
            
            // Thread pool approach: 1 thread = 1 species, no semaphore needed
            
            var results = new List<OptimizerReport>();
            
            try
            {
                // Get algorithm parameters
                var crossoverRate = GetParameterValue("CrossoverRate", 0.8);
                var mutationRate = GetParameterValue("MutationRate", 0.1);
                var elitismCount = GetParameterValue("ElitismCount", 5);
                var selectionMethod = GetParameterValue("SelectionMethod", SelectionMethod.Tournament);
                var tournamentSize = GetParameterValue("TournamentSize", 3);

                // Initialize population
                _population = new Population(populationSize, parameters, parametersToOptimize, 0);
                _originalPopulationSize = populationSize; // Store the original population size
                _currentFaze = 1; // Use a simple counter instead of faze.Number
                _currentOptimizerFaze = faze; // Store the actual optimization phase
                
                // Check if we need to run OutSample testing
                var allFazes = _optimizerMaster.Fazes;
                var inSampleFaze = allFazes.FirstOrDefault(f => f.TypeFaze == OptimizerFazeType.InSample);
                var outSampleFaze = allFazes.FirstOrDefault(f => f.TypeFaze == OptimizerFazeType.OutOfSample);
                
                
                if (inSampleFaze == null)
                {
                    return new List<OptimizerReport>();
                }

                // Phase 1: Run genetic algorithm on InSample data
                _currentOptimizerFaze = inSampleFaze;
                
                for (int generation = 1; generation <= maxIterations; generation++)
                {
                    // Check for emergency stop
                    if (_needToStop)
                    {
                        break;
                    }

                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    // Evaluate population
                    EvaluatePopulation();
                    
                    // Sort by fitness
                    _population.Individuals.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
                    
                    // Log best individual
                    var bestIndividual = _population.Individuals[0];
                    
                    // Fire progress event
                    ProgressUpdated?.Invoke(generation, bestIndividual.Fitness, $"InSample Generation {generation}/{maxIterations}");
                    
                    // Check for convergence or early stopping
                    if (generation > 10 && CheckConvergence())
                    {
                        break;
                    }
                    
                    // Create next generation
                    if (generation < maxIterations)
                    {
                        CreateNextGeneration(crossoverRate, mutationRate, elitismCount, selectionMethod, tournamentSize);
                    }
                }
                
                // Phase 2: Test best individuals on OutSample data (if available)
                if (outSampleFaze != null)
                {
                    _currentOptimizerFaze = outSampleFaze;
                    
                    // Get top individuals from InSample optimization
                    var topIndividuals = _population.Individuals.Take(Math.Min(10, _population.Individuals.Count)).ToList();
                    
                    // Test each top individual on OutSample data in parallel
                    try
                    {
                        var parallelOptions = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = _threadsCount,
                            CancellationToken = cancellationToken
                        };
                        
                        Parallel.ForEach(topIndividuals, parallelOptions, individual =>
                        {
                            // Check for emergency stop
                            if (_needToStop || cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                            
                            try
                            {
                                // Create a new report for OutSample testing
                                var outSampleReport = new OptimizerFazeReport();
                                outSampleReport.Faze = outSampleFaze;
                                
                                // Test the individual on OutSample data, preserving the original bot name
                                var outSampleResult = RunIndividualTest(individual.Parameters, outSampleReport, individual.Report?.BotName);
                                
                                if (outSampleResult != null)
                                {
                                    // Store OutSample results in the individual
                                    individual.OutSampleReport = outSampleResult;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue with other individuals
                            }
                        });
                        
                    }
                    catch (OperationCanceledException)
                    {
                        // OutSample testing was cancelled
                    }
                    catch (Exception ex)
                    {
                        // OutSample testing failed
                    }
                }

                // Return best results in proper OptimizerFazeReport structure
                var bestIndividuals = _population.Individuals.Take(10).ToList();
                
                // Create InSample OptimizerFazeReport
                var inSampleFazeReport = new OptimizerFazeReport();
                inSampleFazeReport.Faze = inSampleFaze;
                
                // Create OutSample OptimizerFazeReport if OutSample testing was performed
                OptimizerFazeReport outSampleFazeReport = null;
                if (outSampleFaze != null)
                {
                    outSampleFazeReport = new OptimizerFazeReport();
                    outSampleFazeReport.Faze = outSampleFaze;
                }
                
                foreach (var individual in bestIndividuals)
                {
                    // Add InSample result to InSample faze report
                    if (individual.Report != null)
                    {
                        inSampleFazeReport.Reports.Add(individual.Report);
                    }
                    
                    // Add OutSample result to OutSample faze report if available
                    if (individual.OutSampleReport != null && outSampleFazeReport != null)
                    {
                        outSampleFazeReport.Reports.Add(individual.OutSampleReport);
                    }
                }
                
                // Add faze reports to results (this is what the UI expects)
                if (inSampleFazeReport.Reports.Count > 0)
                {
                    results.AddRange(inSampleFazeReport.Reports);
                }
                
                if (outSampleFazeReport != null && outSampleFazeReport.Reports.Count > 0)
                {
                    results.AddRange(outSampleFazeReport.Reports);
                }

                // Fire completion event
                OptimizationCompleted?.Invoke(results);
            }
            catch (Exception ex)
            {
                _optimizerMaster?.SendLogMessage($"Genetic algorithm error: {ex.Message}", LogMessageType.Error);
            }
            finally
            {
                // Stop the optimizer executor
                _optimizerExecutor?.Stop();
            }

            return results;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get parameter value with type conversion.
        /// Получить значение параметра с преобразованием типа.
        /// </summary>
        /// <typeparam name="T">Parameter type / Тип параметра</typeparam>
        /// <param name="key">Parameter key / Ключ параметра</param>
        /// <param name="defaultValue">Default value / Значение по умолчанию</param>
        /// <returns>Parameter value / Значение параметра</returns>
        private T GetParameterValue<T>(string key, T defaultValue)
        {
            if (_optimizerMaster?.AlgorithmParameters != null && _optimizerMaster.AlgorithmParameters.ContainsKey(key))
            {
                try
                {
                    return (T)Convert.ChangeType(_optimizerMaster.AlgorithmParameters[key], typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Evaluate all individuals in the population using parallel execution.
        /// Оценить всех особей в популяции используя параллельное выполнение.
        /// </summary>
        private void EvaluatePopulation()
        {
            // Get unevaluated individuals
            var unevaluatedIndividuals = _population.Individuals.Where(i => !i.IsEvaluated).ToList();
            
            if (unevaluatedIndividuals.Count == 0)
            {
                return;
            }
            
            // Use thread pool pattern: 1 thread = 1 species
            var tasks = new List<Task>();
            var individualQueue = new Queue<Individual>(unevaluatedIndividuals);
            var queueLock = new object();
            var completedCount = 0;
            var totalCount = unevaluatedIndividuals.Count;
            
            // Create worker tasks (one per thread)
            for (int i = 0; i < _threadsCount; i++)
            {
                int threadId = i;
                var task = Task.Run(() =>
                {
                    while (true)
                    {
                        // Check for emergency stop
                        if (_needToStop)
                        {
                            break;
                        }
                        
                        Individual individual = null;
                        
                        // Get next individual to evaluate
                        lock (queueLock)
                        {
                            if (individualQueue.Count == 0)
                            {
                                break; // No more individuals to evaluate
                            }
                            individual = individualQueue.Dequeue();
                        }
                        
                        if (individual != null)
                        {
                            EvaluateIndividual(individual);
                            
                            // Update progress
                            lock (queueLock)
                            {
                                completedCount++;
                                _optimizerMaster?.SendLogMessage($"Thread {threadId}: Completed evaluation. Progress: {completedCount}/{totalCount}", LogMessageType.System);
                            }
                        }
                    }
                });
                
                tasks.Add(task);
            }
            
            // Wait for all threads to complete
            Task.WaitAll(tasks.ToArray());
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
                
                if (_optimizerExecutor == null || _optimizerMaster == null)
                {
                    _optimizerMaster?.SendLogMessage("EvaluateIndividual: OptimizerExecutor or OptimizerMaster is null", LogMessageType.Error);
                    individual.Fitness = 0.0;
                    individual.IsEvaluated = true;
                    return;
                }

                // Create a temporary report for this individual test
                var report = new OptimizerFazeReport();
                report.Faze = _currentOptimizerFaze; // Use the actual optimization phase with correct date range
                
                // Use the OptimizerExecutor's infrastructure to run the test
                var result = RunIndividualTest(individual.Parameters, report);
                
                if (result != null)
                {
                    individual.Report = result;
                    individual.Fitness = CalculateFitness(individual);
                    
                    _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Individual {individual.Id} evaluated successfully - Fitness: {individual.Fitness:F4}, Profit: {result.TotalProfit:F2}", LogMessageType.System);
                    
                    if (individual.Report.PositionsCount == 0)
                    {
                        _zeroTradeBots++;
                    }
                }
                else
                {
                    _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Individual {individual.Id} evaluation failed - result is null", LogMessageType.Error);
                    // Create a dummy report with poor fitness for failed evaluations
                    individual.Report = CreateDummyReport(individual.Parameters);
                    individual.Fitness = -1000; // Very poor fitness for failed evaluations
                }

                individual.IsEvaluated = true;
            }
            catch (Exception ex)
            {
                _optimizerMaster?.SendLogMessage($"EvaluateIndividual: Exception evaluating individual {individual.Id}: {ex.Message}", LogMessageType.Error);
                // Create a dummy report with poor fitness for exceptions
                individual.Report = CreateDummyReport(individual.Parameters);
                individual.Fitness = -1000; // Very poor fitness for exceptions
                individual.IsEvaluated = true;
            }
        }

        /// <summary>
        /// Run a test for an individual using the improved genetic algorithm bot testing infrastructure.
        /// Запустить тест для особи используя улучшенную инфраструктуру тестирования ботов генетического алгоритма.
        /// </summary>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="report">Report to fill with results / Отчет для заполнения результатами</param>
        /// <param name="originalBotName">Original bot name to preserve (optional) / Оригинальное имя бота для сохранения (опционально)</param>
        /// <returns>Optimization report / Отчет об оптимизации</returns>
        private async Task<OptimizerReport> RunIndividualTestAsync(List<IIStrategyParameter> parameters, OptimizerFazeReport report, string originalBotName = null)
        {
            if (_optimizerExecutor == null || _optimizerMaster == null)
            {
                throw new InvalidOperationException("OptimizerExecutor and OptimizerMaster must be set before running individual tests");
            }

            // Check if optimization should stop
            if (_needToStop)
            {
                return null;
            }

            // Use the new genetic algorithm-specific method with retry logic
            const int maxRetries = 2; // Reduced retries to prevent excessive delays
            const int retryDelayMs = 2000; // Increased delay to give system more time to recover
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // Check if optimization should stop before each attempt
                if (_needToStop)
                {
                    return null;
                }
                
                try
                {
                    // Use the new TestBotForGeneticAlgorithm method
                    var result = _optimizerExecutor.TestBotForGeneticAlgorithm(parameters, report.Faze, originalBotName);
                    
                    if (result != null)
                    {
                        return result;
                    }
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                    else
                    {
                        // Return null instead of throwing to allow the algorithm to continue with other individuals
                        return null;
                    }
                }
            }
            
            // Return null instead of throwing to allow the algorithm to continue
            return null;
        }

        /// <summary>
        /// Run a test for an individual using the improved genetic algorithm bot testing infrastructure.
        /// Запустить тест для особи используя улучшенную инфраструктуру тестирования ботов генетического алгоритма.
        /// </summary>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="report">Report to fill with results / Отчет для заполнения результатами</param>
        /// <returns>Optimization report / Отчет об оптимизации</returns>
        private OptimizerReport RunIndividualTest(List<IIStrategyParameter> parameters, OptimizerFazeReport report, string originalBotName = null)
        {
            // Run the async version synchronously
            return RunIndividualTestAsync(parameters, report, originalBotName).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Calculate fitness for an individual based on its performance.
        /// Рассчитать пригодность особи на основе ее производительности.
        /// </summary>
        /// <param name="individual">Individual to evaluate / Особь для оценки</param>
        /// <returns>Fitness value / Значение пригодности</returns>
        private double CalculateFitness(Individual individual)
        {
            if (individual.Report == null)
            {
                return 0.0;
            }

            // Simple fitness function based on profit and drawdown
            double profit = (double)individual.Report.TotalProfit;
            double drawdown = (double)individual.Report.MaxDrawDawn;
            double positions = individual.Report.PositionsCount;

            // Penalize zero-trade bots
            if (positions == 0)
            {
                return -1000.0;
            }

            // Basic fitness: profit - drawdown penalty
            double fitness = profit - (drawdown * 10); // Penalize drawdown heavily

            return fitness;
        }

        /// <summary>
        /// Create a dummy report for failed evaluations.
        /// Создать фиктивный отчет для неудачных оценок.
        /// </summary>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <returns>Dummy optimization report / Фиктивный отчет об оптимизации</returns>
        private OptimizerReport CreateDummyReport(List<IIStrategyParameter> parameters)
        {
            var dummyReport = new OptimizerReport(parameters);
            dummyReport.TotalProfit = -1000; // Very poor profit
            dummyReport.MaxDrawDawn = -1000; // Very poor drawdown
            dummyReport.PositionsCount = 0; // No trades
            dummyReport.BotName = "FailedBot_" + Guid.NewGuid().ToString("N")[..8];
            return dummyReport;
        }

        /// <summary>
        /// Check if the population has converged.
        /// Проверить, сошлась ли популяция.
        /// </summary>
        /// <returns>True if converged / True, если сошлась</returns>
        private bool CheckConvergence()
        {
            if (_population.Individuals.Count < 10)
            {
                return false;
            }

            // Check if top 10% of individuals have similar fitness
            var topCount = Math.Max(1, _population.Individuals.Count / 10);
            var topIndividuals = _population.Individuals.Take(topCount).ToList();
            
            double maxFitness = topIndividuals[0].Fitness;
            double minFitness = topIndividuals[topCount - 1].Fitness;
            
            // Consider converged if fitness range is small
            return (maxFitness - minFitness) < 0.01;
        }

        /// <summary>
        /// Create the next generation using genetic operations.
        /// Создать следующее поколение используя генетические операции.
        /// </summary>
        /// <param name="crossoverRate">Crossover rate / Частота скрещивания</param>
        /// <param name="mutationRate">Mutation rate / Частота мутации</param>
        /// <param name="elitismCount">Number of elite individuals to preserve / Количество элитных особей для сохранения</param>
        /// <param name="selectionMethod">Selection method / Метод отбора</param>
        /// <param name="tournamentSize">Tournament size for tournament selection / Размер турнира для турнирного отбора</param>
        private void CreateNextGeneration(double crossoverRate, double mutationRate, int elitismCount, SelectionMethod selectionMethod, int tournamentSize)
        {
            var newIndividuals = new List<Individual>();
            
            // Use the original population size to ensure we always maintain the target population size
            int targetPopulationSize = _originalPopulationSize;
            
            // Preserve elite individuals
            for (int i = 0; i < elitismCount && i < _population.Individuals.Count; i++)
            {
                newIndividuals.Add(_population.Individuals[i]);
            }
            
            // Generate remaining individuals through crossover and mutation
            // Use targetPopulationSize to ensure we always maintain the original population size
            while (newIndividuals.Count < targetPopulationSize)
            {
                // Select parents
                var parent1 = SelectParent(selectionMethod, tournamentSize);
                var parent2 = SelectParent(selectionMethod, tournamentSize);
                
                // Create offspring through crossover
                var offspring = Crossover(parent1, parent2, crossoverRate);
                
                // Apply mutation
                Mutate(offspring, mutationRate);
                
                // Reset evaluation status
                offspring.IsEvaluated = false;
                offspring.Report = null;
                offspring.Fitness = 0.0;
                
                newIndividuals.Add(offspring);
            }
            
            // Replace the population individuals and increment generation
            _population.Individuals.Clear();
            foreach (var individual in newIndividuals)
            {
                _population.Individuals.Add(individual);
            }
            _population.Generation++;
        }

        /// <summary>
        /// Select a parent using the specified selection method.
        /// Выбрать родителя используя указанный метод отбора.
        /// </summary>
        /// <param name="selectionMethod">Selection method / Метод отбора</param>
        /// <param name="tournamentSize">Tournament size / Размер турнира</param>
        /// <returns>Selected parent / Выбранный родитель</returns>
        private Individual SelectParent(SelectionMethod selectionMethod, int tournamentSize)
        {
            switch (selectionMethod)
            {
                case SelectionMethod.Tournament:
                    return TournamentSelection(tournamentSize);
                case SelectionMethod.Roulette:
                    return RouletteSelection();
                default:
                    return TournamentSelection(tournamentSize);
            }
        }

        /// <summary>
        /// Tournament selection.
        /// Турнирный отбор.
        /// </summary>
        /// <param name="tournamentSize">Tournament size / Размер турнира</param>
        /// <returns>Selected individual / Выбранная особь</returns>
        private Individual TournamentSelection(int tournamentSize)
        {
            var tournament = new List<Individual>();
            
            for (int i = 0; i < tournamentSize; i++)
            {
                int randomIndex = _random.Next(_population.Individuals.Count);
                tournament.Add(_population.Individuals[randomIndex]);
            }
            
            return tournament.OrderByDescending(x => x.Fitness).First();
        }

        /// <summary>
        /// Roulette wheel selection.
        /// Отбор методом рулетки.
        /// </summary>
        /// <returns>Selected individual / Выбранная особь</returns>
        private Individual RouletteSelection()
        {
            double totalFitness = _population.Individuals.Sum(x => Math.Max(0, x.Fitness));
            
            if (totalFitness <= 0)
            {
                return _population.Individuals[_random.Next(_population.Individuals.Count)];
            }
            
            double randomValue = _random.NextDouble() * totalFitness;
            double currentSum = 0;
            
            foreach (var individual in _population.Individuals)
            {
                currentSum += Math.Max(0, individual.Fitness);
                if (currentSum >= randomValue)
                {
                    return individual;
                }
            }
            
            return _population.Individuals.Last();
        }

        /// <summary>
        /// Perform crossover between two parents to create offspring.
        /// Выполнить скрещивание между двумя родителями для создания потомства.
        /// </summary>
        /// <param name="parent1">First parent / Первый родитель</param>
        /// <param name="parent2">Second parent / Второй родитель</param>
        /// <param name="crossoverRate">Crossover rate / Частота скрещивания</param>
        /// <returns>Offspring / Потомство</returns>
        private Individual Crossover(Individual parent1, Individual parent2, double crossoverRate)
        {
            if (_random.NextDouble() > crossoverRate)
            {
                // No crossover, return copy of parent1
                return parent1.Clone();
            }
            
            var offspringParams = new List<IIStrategyParameter>();
            
            // Uniform crossover
            for (int i = 0; i < parent1.Parameters.Count; i++)
            {
                if (_random.NextDouble() < 0.5)
                {
                    offspringParams.Add(parent1.Parameters[i]);
                }
                else
                {
                    offspringParams.Add(parent2.Parameters[i]);
                }
            }
            
            return new Individual(offspringParams);
        }

        /// <summary>
        /// Apply mutation to an individual.
        /// Применить мутацию к особи.
        /// </summary>
        /// <param name="individual">Individual to mutate / Особь для мутации</param>
        /// <param name="mutationRate">Mutation rate / Частота мутации</param>
        private void Mutate(Individual individual, double mutationRate)
        {
            foreach (var parameter in individual.Parameters)
            {
                if (_random.NextDouble() < mutationRate)
                {
                    ApplyMutation(parameter);
                }
            }
        }

        /// <summary>
        /// Apply mutation to a specific parameter.
        /// Применить мутацию к конкретному параметру.
        /// </summary>
        /// <param name="parameter">Parameter to mutate / Параметр для мутации</param>
        private void ApplyMutation(IIStrategyParameter parameter)
        {
            try
            {
                // Simple mutation: randomly change parameter value within its range
                if (parameter is StrategyParameterDecimalCheckBox decimalParam)
                {
                    // For decimal parameters, add small random change
                    double currentValue = (double)decimalParam.ValueDecimal;
                    double range = (double)(decimalParam.ValueDecimalStop - decimalParam.ValueDecimalStart);
                    double mutation = (_random.NextDouble() - 0.5) * range * 0.1; // 10% of range
                    double newValue = Math.Max((double)decimalParam.ValueDecimalStart, 
                                             Math.Min((double)decimalParam.ValueDecimalStop, currentValue + mutation));
                    decimalParam.ValueDecimal = (decimal)newValue;
                }
                else if (parameter is StrategyParameterTimeOfDay timeParam)
                {
                    // For time parameters, randomly change time
                    int totalMinutes = timeParam.Value.Hour * 60 + timeParam.Value.Minute;
                    int mutation = _random.Next(-60, 61); // ±1 hour
                    totalMinutes = Math.Max(0, Math.Min(1439, totalMinutes + mutation)); // 0-23:59
                    timeParam.Value.Hour = totalMinutes / 60;
                    timeParam.Value.Minute = totalMinutes % 60;
                }
            }
            catch (Exception ex)
            {
                _optimizerMaster?.SendLogMessage($"ApplyMutation: Error applying mutation: {ex.Message}", LogMessageType.Error);
            }
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        /// <summary>
        /// Dispose of resources used by the genetic algorithm.
        /// Освободить ресурсы, используемые генетическим алгоритмом.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of resources used by the genetic algorithm.
        /// Освободить ресурсы, используемые генетическим алгоритмом.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer / True если вызвано из Dispose(), false если вызвано из финализатора</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }
                _disposed = true;
            }
        }

        #endregion
    }
}
