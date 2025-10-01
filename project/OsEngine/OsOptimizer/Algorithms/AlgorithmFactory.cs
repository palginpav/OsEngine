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
    /// Factory for creating and managing optimization algorithms.
    /// Фабрика для создания и управления алгоритмами оптимизации.
    /// </summary>
    public static class AlgorithmFactory
    {
        /// <summary>
        /// Available optimization algorithms.
        /// Доступные алгоритмы оптимизации.
        /// </summary>
        public enum AlgorithmType
        {
            /// <summary>
            /// Brute force algorithm (exhaustive search) / Алгоритм полного перебора
            /// </summary>
            BruteForce,

            /// <summary>
            /// Standard Genetic Algorithm / Стандартный генетический алгоритм
            /// </summary>
            StandardGeneticAlgorithm,

            /// <summary>
            /// NEAT (NeuroEvolution of Augmenting Topologies) / NEAT (нейроэволюция увеличивающихся топологий)
            /// </summary>
            NEAT,

            /// <summary>
            /// Particle Swarm Optimization / Оптимизация роя частиц
            /// </summary>
            ParticleSwarmOptimization,

            /// <summary>
            /// Differential Evolution / Дифференциальная эволюция
            /// </summary>
            DifferentialEvolution
        }

        /// <summary>
        /// Get all available algorithm types.
        /// Получить все доступные типы алгоритмов.
        /// </summary>
        /// <returns>List of available algorithm types / Список доступных типов алгоритмов</returns>
        public static List<AlgorithmType> GetAvailableAlgorithms()
        {
            return Enum.GetValues(typeof(AlgorithmType)).Cast<AlgorithmType>().ToList();
        }

        /// <summary>
        /// Get algorithm information for a specific type.
        /// Получить информацию об алгоритме для конкретного типа.
        /// </summary>
        /// <param name="algorithmType">Algorithm type / Тип алгоритма</param>
        /// <returns>Algorithm information / Информация об алгоритме</returns>
        public static AlgorithmInfo GetAlgorithmInfo(AlgorithmType algorithmType)
        {
            switch (algorithmType)
            {
                case AlgorithmType.BruteForce:
                    return new AlgorithmInfo
                    {
                        Type = algorithmType,
                        Name = "Brute Force",
                        Description = "Exhaustive search through all parameter combinations. Guarantees finding the global optimum but can be very slow for large parameter spaces.",
                        SupportsMultiObjective = false,
                        RecommendedFor = "Small parameter spaces (< 1000 combinations)",
                        DefaultParameters = new Dictionary<string, object>
                        {
                            ["MaxIterations"] = 1000
                        }
                    };

                case AlgorithmType.StandardGeneticAlgorithm:
                    return new AlgorithmInfo
                    {
                        Type = algorithmType,
                        Name = "Standard Genetic Algorithm",
                        Description = "Classic genetic algorithm with selection, crossover, and mutation. Good balance of exploration and exploitation.",
                        SupportsMultiObjective = true,
                        RecommendedFor = "Medium to large parameter spaces, multi-objective optimization",
                        DefaultParameters = new Dictionary<string, object>
                        {
                            ["PopulationSize"] = 50,
                            ["MaxGenerations"] = 100,
                            ["EliteCount"] = 5,
                            ["CrossoverRate"] = 0.8,
                            ["MutationRate"] = 0.1,
                            ["MutationStrength"] = 0.1,
                            ["SelectionMethod"] = SelectionMethod.Tournament,
                            ["TournamentSize"] = 3,
                            ["ConvergenceThreshold"] = 0.001,
                            ["MaxStagnationGenerations"] = 20
                        }
                    };

                case AlgorithmType.NEAT:
                    return new AlgorithmInfo
                    {
                        Type = algorithmType,
                        Name = "NEAT (NeuroEvolution of Augmenting Topologies)",
                        Description = "Evolves neural network topologies along with weights. Excellent for complex, non-linear optimization problems.",
                        SupportsMultiObjective = true,
                        RecommendedFor = "Complex neural network strategies, non-linear parameter relationships",
                        DefaultParameters = new Dictionary<string, object>
                        {
                            ["PopulationSize"] = 100,
                            ["MaxGenerations"] = 200,
                            ["EliteCount"] = 10,
                            ["CrossoverRate"] = 0.75,
                            ["MutationRate"] = 0.1,
                            ["AddNodeMutationRate"] = 0.03,
                            ["AddConnectionMutationRate"] = 0.05,
                            ["WeightMutationRate"] = 0.8,
                            ["CompatibilityThreshold"] = 3.0,
                            ["SpeciesCount"] = 15
                        }
                    };

                case AlgorithmType.ParticleSwarmOptimization:
                    return new AlgorithmInfo
                    {
                        Type = algorithmType,
                        Name = "Particle Swarm Optimization",
                        Description = "Swarm intelligence algorithm inspired by bird flocking. Good for continuous parameter optimization.",
                        SupportsMultiObjective = true,
                        RecommendedFor = "Continuous parameter spaces, smooth fitness landscapes",
                        DefaultParameters = new Dictionary<string, object>
                        {
                            ["SwarmSize"] = 30,
                            ["MaxIterations"] = 100,
                            ["InertiaWeight"] = 0.9,
                            ["CognitiveWeight"] = 2.0,
                            ["SocialWeight"] = 2.0,
                            ["MaxVelocity"] = 0.1,
                            ["ConvergenceThreshold"] = 0.001
                        }
                    };

                case AlgorithmType.DifferentialEvolution:
                    return new AlgorithmInfo
                    {
                        Type = algorithmType,
                        Name = "Differential Evolution",
                        Description = "Robust global optimization algorithm. Good for noisy fitness landscapes and constrained optimization.",
                        SupportsMultiObjective = true,
                        RecommendedFor = "Noisy fitness functions, constrained optimization, global optimization",
                        DefaultParameters = new Dictionary<string, object>
                        {
                            ["PopulationSize"] = 50,
                            ["MaxGenerations"] = 100,
                            ["CrossoverRate"] = 0.9,
                            ["DifferentialWeight"] = 0.8,
                            ["Strategy"] = "DE/rand/1/bin",
                            ["ConvergenceThreshold"] = 0.001
                        }
                    };

                default:
                    throw new ArgumentException($"Unknown algorithm type: {algorithmType}");
            }
        }

        /// <summary>
        /// Create an algorithm instance of the specified type.
        /// Создать экземпляр алгоритма указанного типа.
        /// </summary>
        /// <param name="algorithmType">Algorithm type / Тип алгоритма</param>
        /// <returns>Created algorithm instance / Созданный экземпляр алгоритма</returns>
        public static IOptimizationAlgorithm CreateAlgorithm(AlgorithmType algorithmType)
        {
            switch (algorithmType)
            {
                case AlgorithmType.StandardGeneticAlgorithm:
                    return new StandardGeneticAlgorithm();

                case AlgorithmType.NEAT:
                    // TODO: Implement NEAT algorithm
                    throw new NotImplementedException("NEAT algorithm not yet implemented");

                case AlgorithmType.ParticleSwarmOptimization:
                    // TODO: Implement PSO algorithm
                    throw new NotImplementedException("Particle Swarm Optimization algorithm not yet implemented");

                case AlgorithmType.DifferentialEvolution:
                    // TODO: Implement DE algorithm
                    throw new NotImplementedException("Differential Evolution algorithm not yet implemented");

                case AlgorithmType.BruteForce:
                    // TODO: Implement BruteForce algorithm wrapper
                    throw new NotImplementedException("BruteForce algorithm wrapper not yet implemented");

                default:
                    throw new ArgumentException($"Unknown algorithm type: {algorithmType}");
            }
        }

        /// <summary>
        /// Get recommended algorithm for given parameter space characteristics.
        /// Получить рекомендуемый алгоритм для заданных характеристик пространства параметров.
        /// </summary>
        /// <param name="parameterCount">Number of parameters / Количество параметров</param>
        /// <param name="estimatedCombinations">Estimated number of combinations / Ориентировочное количество комбинаций</param>
        /// <param name="parameterTypes">Types of parameters / Типы параметров</param>
        /// <param name="multiObjective">Whether multi-objective optimization is needed / Нужна ли многоцелевая оптимизация</param>
        /// <returns>Recommended algorithm type / Рекомендуемый тип алгоритма</returns>
        public static AlgorithmType GetRecommendedAlgorithm(
            int parameterCount, 
            long estimatedCombinations, 
            List<string> parameterTypes, 
            bool multiObjective = false)
        {
            // For very small parameter spaces, brute force is best
            if (estimatedCombinations <= 1000)
            {
                return AlgorithmType.BruteForce;
            }

            // For neural network strategies or complex topologies
            if (parameterTypes.Contains("NeuralNetwork") || parameterTypes.Contains("Topology"))
            {
                return AlgorithmType.NEAT;
            }

            // For continuous parameters with smooth landscapes
            if (parameterTypes.All(t => t == "Decimal" || t == "Int"))
            {
                return AlgorithmType.ParticleSwarmOptimization;
            }

            // For mixed parameter types or multi-objective optimization
            if (multiObjective || parameterTypes.Count > 2)
            {
                return AlgorithmType.StandardGeneticAlgorithm;
            }

            // Default recommendation
            return AlgorithmType.StandardGeneticAlgorithm;
        }

        /// <summary>
        /// Validate algorithm parameters.
        /// Проверить параметры алгоритма.
        /// </summary>
        /// <param name="algorithmType">Algorithm type / Тип алгоритма</param>
        /// <param name="parameters">Parameters to validate / Параметры для проверки</param>
        /// <returns>Validation result / Результат проверки</returns>
        public static ValidationResult ValidateParameters(AlgorithmType algorithmType, Dictionary<string, object> parameters)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            switch (algorithmType)
            {
                case AlgorithmType.StandardGeneticAlgorithm:
                    ValidateGeneticAlgorithmParameters(parameters, result);
                    break;

                case AlgorithmType.NEAT:
                    ValidateNEATParameters(parameters, result);
                    break;

                case AlgorithmType.ParticleSwarmOptimization:
                    ValidatePSOParameters(parameters, result);
                    break;

                case AlgorithmType.DifferentialEvolution:
                    ValidateDEParameters(parameters, result);
                    break;

                case AlgorithmType.BruteForce:
                    ValidateBruteForceParameters(parameters, result);
                    break;

                default:
                    result.IsValid = false;
                    result.Errors.Add($"Unknown algorithm type: {algorithmType}");
                    break;
            }

            return result;
        }

        /// <summary>
        /// Validate genetic algorithm parameters.
        /// Проверить параметры генетического алгоритма.
        /// </summary>
        private static void ValidateGeneticAlgorithmParameters(Dictionary<string, object> parameters, ValidationResult result)
        {
            if (parameters.ContainsKey("PopulationSize"))
            {
                if (!(parameters["PopulationSize"] is int popSize) || popSize < 2)
                {
                    result.IsValid = false;
                    result.Errors.Add("PopulationSize must be an integer >= 2");
                }
            }

            if (parameters.ContainsKey("MaxGenerations"))
            {
                if (!(parameters["MaxGenerations"] is int maxGen) || maxGen < 1)
                {
                    result.IsValid = false;
                    result.Errors.Add("MaxGenerations must be an integer >= 1");
                }
            }

            if (parameters.ContainsKey("CrossoverRate"))
            {
                if (!(parameters["CrossoverRate"] is double crossRate) || crossRate < 0 || crossRate > 1)
                {
                    result.IsValid = false;
                    result.Errors.Add("CrossoverRate must be a double between 0 and 1");
                }
            }

            if (parameters.ContainsKey("MutationRate"))
            {
                if (!(parameters["MutationRate"] is double mutRate) || mutRate < 0 || mutRate > 1)
                {
                    result.IsValid = false;
                    result.Errors.Add("MutationRate must be a double between 0 and 1");
                }
            }
        }

        /// <summary>
        /// Validate NEAT parameters.
        /// Проверить параметры NEAT.
        /// </summary>
        private static void ValidateNEATParameters(Dictionary<string, object> parameters, ValidationResult result)
        {
            // TODO: Implement NEAT parameter validation
        }

        /// <summary>
        /// Validate PSO parameters.
        /// Проверить параметры PSO.
        /// </summary>
        private static void ValidatePSOParameters(Dictionary<string, object> parameters, ValidationResult result)
        {
            // TODO: Implement PSO parameter validation
        }

        /// <summary>
        /// Validate DE parameters.
        /// Проверить параметры DE.
        /// </summary>
        private static void ValidateDEParameters(Dictionary<string, object> parameters, ValidationResult result)
        {
            // TODO: Implement DE parameter validation
        }

        /// <summary>
        /// Validate brute force parameters.
        /// Проверить параметры полного перебора.
        /// </summary>
        private static void ValidateBruteForceParameters(Dictionary<string, object> parameters, ValidationResult result)
        {
            // TODO: Implement brute force parameter validation
        }
    }

    /// <summary>
    /// Information about an optimization algorithm.
    /// Информация об алгоритме оптимизации.
    /// </summary>
    public class AlgorithmInfo
    {
        /// <summary>
        /// Algorithm type / Тип алгоритма
        /// </summary>
        public AlgorithmFactory.AlgorithmType Type { get; set; }

        /// <summary>
        /// Algorithm name / Название алгоритма
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Algorithm description / Описание алгоритма
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether it supports multi-objective optimization / Поддерживает ли многоцелевую оптимизацию
        /// </summary>
        public bool SupportsMultiObjective { get; set; }

        /// <summary>
        /// Recommended use cases / Рекомендуемые случаи использования
        /// </summary>
        public string RecommendedFor { get; set; }

        /// <summary>
        /// Default parameters / Параметры по умолчанию
        /// </summary>
        public Dictionary<string, object> DefaultParameters { get; set; }
    }

    /// <summary>
    /// Result of parameter validation.
    /// Результат проверки параметров.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether validation passed / Прошла ли проверка
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors / Список ошибок проверки
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
