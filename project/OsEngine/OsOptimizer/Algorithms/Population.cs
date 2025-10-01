/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Linq;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;

namespace OsEngine.OsOptimizer.Algorithms
{
    /// <summary>
    /// Represents a population of individuals in the genetic algorithm.
    /// Представляет популяцию особей в генетическом алгоритме.
    /// </summary>
    public class Population
    {
        /// <summary>
        /// List of individuals in the population.
        /// Список особей в популяции.
        /// </summary>
        public List<Individual> Individuals { get; private set; }

        /// <summary>
        /// Current generation number.
        /// Номер текущего поколения.
        /// </summary>
        public int Generation { get; set; }

        /// <summary>
        /// Best individual in the population.
        /// Лучшая особь в популяции.
        /// </summary>
        public Individual BestIndividual => Individuals?.OrderByDescending(i => i.Fitness).FirstOrDefault();

        /// <summary>
        /// Average fitness of the population.
        /// Средняя пригодность популяции.
        /// </summary>
        public double AverageFitness => Individuals?.Where(i => i.IsEvaluated).Average(i => i.Fitness) ?? 0.0;

        /// <summary>
        /// Standard deviation of fitness in the population.
        /// Стандартное отклонение пригодности в популяции.
        /// </summary>
        public double FitnessStandardDeviation
        {
            get
            {
                if (Individuals == null || Individuals.Count == 0)
                    return 0.0;

                var evaluatedIndividuals = Individuals.Where(i => i.IsEvaluated).ToList();
                if (evaluatedIndividuals.Count == 0)
                    return 0.0;

                var avg = AverageFitness;
                var variance = evaluatedIndividuals.Average(i => Math.Pow(i.Fitness - avg, 2));
                return Math.Sqrt(variance);
            }
        }

        /// <summary>
        /// Diversity measure of the population.
        /// Мера разнообразия популяции.
        /// </summary>
        public double Diversity
        {
            get
            {
                if (Individuals == null || Individuals.Count <= 1)
                    return 0.0;

                double totalDistance = 0.0;
                int comparisons = 0;

                for (int i = 0; i < Individuals.Count; i++)
                {
                    for (int j = i + 1; j < Individuals.Count; j++)
                    {
                        totalDistance += CalculateDistance(Individuals[i], Individuals[j]);
                        comparisons++;
                    }
                }

                return comparisons > 0 ? totalDistance / comparisons : 0.0;
            }
        }

        /// <summary>
        /// Initialize a new population with given individuals.
        /// Инициализировать новую популяцию с заданными особями.
        /// </summary>
        /// <param name="individuals">List of individuals / Список особей</param>
        /// <param name="generation">Generation number / Номер поколения</param>
        public Population(List<Individual> individuals, int generation = 0)
        {
            Individuals = individuals ?? new List<Individual>();
            Generation = generation;
        }

        /// <summary>
        /// Initialize a new random population.
        /// Инициализировать новую случайную популяцию.
        /// </summary>
        /// <param name="size">Population size / Размер популяции</param>
        /// <param name="parameters">Strategy parameters template / Шаблон параметров стратегии</param>
        /// <param name="parametersToOptimize">Which parameters to optimize / Какие параметры оптимизировать</param>
        /// <param name="generation">Generation number / Номер поколения</param>
        public Population(int size, List<IIStrategyParameter> parameters, List<bool> parametersToOptimize, int generation = 0)
        {
            Individuals = new List<Individual>();
            Generation = generation;

            for (int i = 0; i < size; i++)
            {
                var individual = CreateRandomIndividual(parameters, parametersToOptimize);
                individual.Generation = generation;
                Individuals.Add(individual);
            }
        }

        /// <summary>
        /// Add an individual to the population.
        /// Добавить особь в популяцию.
        /// </summary>
        /// <param name="individual">Individual to add / Особь для добавления</param>
        public void AddIndividual(Individual individual)
        {
            if (individual != null)
            {
                Individuals.Add(individual);
            }
        }

        /// <summary>
        /// Remove an individual from the population.
        /// Удалить особь из популяции.
        /// </summary>
        /// <param name="individual">Individual to remove / Особь для удаления</param>
        public bool RemoveIndividual(Individual individual)
        {
            return Individuals.Remove(individual);
        }

        /// <summary>
        /// Get individuals sorted by fitness (descending).
        /// Получить особей, отсортированных по пригодности (по убыванию).
        /// </summary>
        /// <returns>Sorted list of individuals / Отсортированный список особей</returns>
        public List<Individual> GetSortedIndividuals()
        {
            return Individuals.OrderByDescending(i => i.Fitness).ToList();
        }

        /// <summary>
        /// Get top N individuals by fitness.
        /// Получить топ N особей по пригодности.
        /// </summary>
        /// <param name="count">Number of top individuals / Количество лучших особей</param>
        /// <returns>List of top individuals / Список лучших особей</returns>
        public List<Individual> GetTopIndividuals(int count)
        {
            return GetSortedIndividuals().Take(count).ToList();
        }

        /// <summary>
        /// Get individuals for selection (tournament, roulette, etc.).
        /// Получить особей для селекции (турнир, рулетка и т.д.).
        /// </summary>
        /// <param name="count">Number of individuals to select / Количество особей для выбора</param>
        /// <param name="selectionMethod">Selection method / Метод селекции</param>
        /// <returns>List of selected individuals / Список выбранных особей</returns>
        public List<Individual> SelectIndividuals(int count, SelectionMethod selectionMethod = SelectionMethod.Tournament)
        {
            switch (selectionMethod)
            {
                case SelectionMethod.Tournament:
                    return TournamentSelection(count);
                case SelectionMethod.Roulette:
                    return RouletteSelection(count);
                case SelectionMethod.Rank:
                    return RankSelection(count);
                case SelectionMethod.Random:
                    return RandomSelection(count);
                default:
                    return TournamentSelection(count);
            }
        }

        /// <summary>
        /// Create next generation from current population.
        /// Создать следующее поколение из текущей популяции.
        /// </summary>
        /// <param name="eliteCount">Number of elite individuals to preserve / Количество элитных особей для сохранения</param>
        /// <param name="crossoverRate">Crossover rate / Частота скрещивания</param>
        /// <param name="mutationRate">Mutation rate / Частота мутации</param>
        /// <param name="mutationStrength">Mutation strength / Сила мутации</param>
        /// <returns>New population / Новая популяция</returns>
        public Population CreateNextGeneration(int eliteCount = 2, double crossoverRate = 0.8, double mutationRate = 0.1, double mutationStrength = 0.1)
        {
            var nextGeneration = new Population(new List<Individual>(), Generation + 1);

            // Preserve elite individuals
            var elite = GetTopIndividuals(eliteCount);
            foreach (var individual in elite)
            {
                nextGeneration.AddIndividual(individual.Clone());
            }

            // Create offspring to fill the rest of the population
            int offspringNeeded = Individuals.Count - eliteCount;
            var random = new Random();

            for (int i = 0; i < offspringNeeded; i++)
            {
                // Select parents
                var parents = SelectIndividuals(2, SelectionMethod.Tournament);
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
        /// Tournament selection method.
        /// Метод турнирной селекции.
        /// </summary>
        private List<Individual> TournamentSelection(int count, int tournamentSize = 3)
        {
            var selected = new List<Individual>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                var tournament = new List<Individual>();
                
                // Select random individuals for tournament
                for (int j = 0; j < tournamentSize && j < Individuals.Count; j++)
                {
                    var randomIndex = random.Next(Individuals.Count);
                    tournament.Add(Individuals[randomIndex]);
                }

                // Select best from tournament
                var winner = tournament.OrderByDescending(ind => ind.Fitness).First();
                selected.Add(winner);
            }

            return selected;
        }

        /// <summary>
        /// Roulette wheel selection method.
        /// Метод селекции рулетки.
        /// </summary>
        private List<Individual> RouletteSelection(int count)
        {
            var selected = new List<Individual>();
            var random = new Random();

            // Calculate total fitness
            var evaluatedIndividuals = Individuals.Where(i => i.IsEvaluated).ToList();
            if (evaluatedIndividuals.Count == 0)
                return RandomSelection(count);

            double totalFitness = evaluatedIndividuals.Sum(i => Math.Max(0, i.Fitness));
            if (totalFitness <= 0)
                return RandomSelection(count);

            for (int i = 0; i < count; i++)
            {
                double randomValue = random.NextDouble() * totalFitness;
                double currentSum = 0;

                foreach (var individual in evaluatedIndividuals)
                {
                    currentSum += Math.Max(0, individual.Fitness);
                    if (currentSum >= randomValue)
                    {
                        selected.Add(individual);
                        break;
                    }
                }
            }

            return selected;
        }

        /// <summary>
        /// Rank-based selection method.
        /// Метод селекции на основе ранга.
        /// </summary>
        private List<Individual> RankSelection(int count)
        {
            var selected = new List<Individual>();
            var random = new Random();

            var sortedIndividuals = GetSortedIndividuals();
            if (sortedIndividuals.Count == 0)
                return selected;

            // Assign ranks (higher fitness = higher rank)
            for (int i = 0; i < sortedIndividuals.Count; i++)
            {
                var individual = sortedIndividuals[i];
                // Rank is position in sorted list (0-based, so add 1)
                double rank = sortedIndividuals.Count - i;
                
                // Use rank for selection
                if (random.NextDouble() < rank / sortedIndividuals.Count)
                {
                    selected.Add(individual);
                    if (selected.Count >= count)
                        break;
                }
            }

            // If we don't have enough, fill with random selection
            while (selected.Count < count && sortedIndividuals.Count > 0)
            {
                var randomIndex = random.Next(sortedIndividuals.Count);
                selected.Add(sortedIndividuals[randomIndex]);
            }

            return selected;
        }

        /// <summary>
        /// Random selection method.
        /// Метод случайной селекции.
        /// </summary>
        private List<Individual> RandomSelection(int count)
        {
            var selected = new List<Individual>();
            var random = new Random();

            for (int i = 0; i < count && Individuals.Count > 0; i++)
            {
                var randomIndex = random.Next(Individuals.Count);
                selected.Add(Individuals[randomIndex]);
            }

            return selected;
        }

        /// <summary>
        /// Create a random individual with given parameters.
        /// Создать случайную особь с заданными параметрами.
        /// </summary>
        private Individual CreateRandomIndividual(List<IIStrategyParameter> parameters, List<bool> parametersToOptimize)
        {
            var random = new Random();
            var individualParams = new List<IIStrategyParameter>();

            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                var shouldOptimize = i < parametersToOptimize.Count && parametersToOptimize[i];

                if (shouldOptimize)
                {
                    individualParams.Add(CreateRandomParameter(param, random));
                }
                else
                {
                    individualParams.Add(Individual.CopyParameter(param));
                }
            }

            return new Individual(individualParams);
        }

        /// <summary>
        /// Validate and round a decimal value to the nearest valid step.
        /// This ensures that parameter values respect the step constraints defined in the optimization grid.
        /// For example, with start=1.0, step=0.1, only values like 1.0, 1.1, 1.2, 1.3, etc. are valid.
        /// Values like 1.1111516 will be rounded to the nearest valid step (1.1 in this case).
        /// 
        /// Проверить и округлить десятичное значение до ближайшего допустимого шага.
        /// Это гарантирует, что значения параметров соблюдают ограничения шага, определенные в сетке оптимизации.
        /// Например, при start=1.0, step=0.1, допустимы только значения типа 1.0, 1.1, 1.2, 1.3 и т.д.
        /// Значения типа 1.1111516 будут округлены до ближайшего допустимого шага (1.1 в данном случае).
        /// </summary>
        /// <param name="value">Value to validate / Значение для проверки</param>
        /// <param name="start">Start value / Начальное значение</param>
        /// <param name="step">Step size / Размер шага</param>
        /// <returns>Validated value / Проверенное значение</returns>
        private static decimal ValidateDecimalStep(decimal value, decimal start, decimal step)
        {
            if (step <= 0)
                return value;

            // Calculate how many steps from start
            decimal stepsFromStart = (value - start) / step;
            
            // Round to nearest step
            int roundedSteps = (int)Math.Round(stepsFromStart);
            
            // Calculate the valid value
            return start + (roundedSteps * step);
        }

        /// <summary>
        /// Validate and round an integer value to the nearest valid step.
        /// Проверить и округлить целочисленное значение до ближайшего допустимого шага.
        /// </summary>
        /// <param name="value">Value to validate / Значение для проверки</param>
        /// <param name="start">Start value / Начальное значение</param>
        /// <param name="step">Step size / Размер шага</param>
        /// <returns>Validated value / Проверенное значение</returns>
        private static int ValidateIntStep(int value, int start, int step)
        {
            if (step <= 0)
                return value;

            // Calculate how many steps from start
            int stepsFromStart = (value - start) / step;
            
            // Calculate the valid value
            return start + (stepsFromStart * step);
        }

        /// <summary>
        /// Generate a random decimal value that respects the step constraint.
        /// Сгенерировать случайное десятичное значение, которое соблюдает ограничение шага.
        /// </summary>
        /// <param name="start">Start value / Начальное значение</param>
        /// <param name="stop">Stop value / Конечное значение</param>
        /// <param name="step">Step size / Размер шага</param>
        /// <param name="random">Random number generator / Генератор случайных чисел</param>
        /// <returns>Random value respecting step constraint / Случайное значение, соблюдающее ограничение шага</returns>
        private static decimal GenerateRandomDecimalWithStep(decimal start, decimal stop, decimal step, Random random)
        {
            if (step <= 0)
                return (decimal)(random.NextDouble() * (double)(stop - start) + (double)start);

            // Calculate number of valid steps
            int stepCount = (int)((stop - start) / step);
            
            if (stepCount <= 0)
                return start;

            // Generate random step index
            int randomStepIndex = random.Next(0, stepCount + 1);
            
            // Calculate the valid value
            return start + (randomStepIndex * step);
        }

        /// <summary>
        /// Generate a random integer value that respects the step constraint.
        /// Сгенерировать случайное целочисленное значение, которое соблюдает ограничение шага.
        /// </summary>
        /// <param name="start">Start value / Начальное значение</param>
        /// <param name="stop">Stop value / Конечное значение</param>
        /// <param name="step">Step size / Размер шага</param>
        /// <param name="random">Random number generator / Генератор случайных чисел</param>
        /// <returns>Random value respecting step constraint / Случайное значение, соблюдающее ограничение шага</returns>
        private static int GenerateRandomIntWithStep(int start, int stop, int step, Random random)
        {
            if (step <= 0)
                return random.Next(start, stop + 1);

            // Calculate number of valid steps
            int stepCount = (stop - start) / step;
            
            if (stepCount <= 0)
                return start;

            // Generate random step index
            int randomStepIndex = random.Next(0, stepCount + 1);
            
            // Calculate the valid value
            return start + (randomStepIndex * step);
        }

        /// <summary>
        /// Create a random value for a parameter.
        /// Создать случайное значение для параметра.
        /// </summary>
        private IIStrategyParameter CreateRandomParameter(IIStrategyParameter parameter, Random random)
        {
            switch (parameter.Type)
            {
                case StrategyParameterType.Bool:
                    return new StrategyParameterBool(parameter.Name, random.NextDouble() < 0.5);

                case StrategyParameterType.Int:
                    var intParam = (StrategyParameterInt)parameter;
                    var intValue = GenerateRandomIntWithStep(intParam.ValueIntStart, intParam.ValueIntStop, intParam.ValueIntStep, random);
                    var newIntParam = new StrategyParameterInt(parameter.Name,
                        intParam.ValueIntDefolt, intParam.ValueIntStart, intParam.ValueIntStop, intParam.ValueIntStep);
                    newIntParam.ValueInt = intValue;
                    return newIntParam;

                case StrategyParameterType.Decimal:
                    var decimalParam = (StrategyParameterDecimal)parameter;
                    var decimalValue = GenerateRandomDecimalWithStep(decimalParam.ValueDecimalStart, decimalParam.ValueDecimalStop, decimalParam.ValueDecimalStep, random);
                    var newDecimalParam = new StrategyParameterDecimal(parameter.Name,
                        decimalParam.ValueDecimalDefolt, decimalParam.ValueDecimalStart, decimalParam.ValueDecimalStop, decimalParam.ValueDecimalStep);
                    newDecimalParam.ValueDecimal = decimalValue;
                    return newDecimalParam;

                case StrategyParameterType.String:
                    var stringParam = (StrategyParameterString)parameter;
                    if (stringParam.ValuesString != null && stringParam.ValuesString.Count > 0)
                    {
                        var randomIndex = random.Next(stringParam.ValuesString.Count);
                        return new StrategyParameterString(parameter.Name, stringParam.ValuesString[randomIndex], stringParam.ValuesString);
                    }
                    return new StrategyParameterString(parameter.Name, stringParam.ValueString, stringParam.ValuesString);

                case StrategyParameterType.TimeOfDay:
                    var timeParam = (StrategyParameterTimeOfDay)parameter;
                    var randomMinutes = random.Next(0, 1440); // 0 to 1439 minutes in a day
                    var randomHour = randomMinutes / 60;
                    var randomMinute = randomMinutes % 60;
                    return new StrategyParameterTimeOfDay(parameter.Name, randomHour, randomMinute, 0, 0);

                case StrategyParameterType.CheckBox:
                    var randomCheckState = random.NextDouble() < 0.5;
                    return new StrategyParameterCheckBox(parameter.Name, randomCheckState);

                case StrategyParameterType.DecimalCheckBox:
                    var decimalCheckParam = (StrategyParameterDecimalCheckBox)parameter;
                    var randomDecimalValue = GenerateRandomDecimalWithStep(decimalCheckParam.ValueDecimalStart, decimalCheckParam.ValueDecimalStop, decimalCheckParam.ValueDecimalStep, random);
                    var randomCheckState2 = random.NextDouble() < 0.5;
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
        /// Test method to verify step validation is working correctly.
        /// Тестовый метод для проверки правильности работы валидации шагов.
        /// </summary>
        /// <param name="start">Start value / Начальное значение</param>
        /// <param name="stop">Stop value / Конечное значение</param>
        /// <param name="step">Step size / Размер шага</param>
        /// <param name="testValue">Value to test / Значение для тестирования</param>
        /// <returns>Validated value / Проверенное значение</returns>
        public static decimal TestStepValidation(decimal start, decimal stop, decimal step, decimal testValue)
        {
            return ValidateDecimalStep(testValue, start, step);
        }

        /// <summary>
        /// Calculate distance between two individuals.
        /// Вычислить расстояние между двумя особями.
        /// </summary>
        private double CalculateDistance(Individual individual1, Individual individual2)
        {
            if (individual1.Parameters.Count != individual2.Parameters.Count)
                return double.MaxValue;

            double totalDistance = 0.0;
            int comparableParams = 0;

            for (int i = 0; i < individual1.Parameters.Count; i++)
            {
                var param1 = individual1.Parameters[i];
                var param2 = individual2.Parameters[i];

                if (param1.Type == param2.Type)
                {
                    double distance = CalculateParameterDistance(param1, param2);
                    if (distance >= 0)
                    {
                        totalDistance += distance;
                        comparableParams++;
                    }
                }
            }

            return comparableParams > 0 ? totalDistance / comparableParams : 0.0;
        }

        /// <summary>
        /// Calculate distance between two parameters.
        /// Вычислить расстояние между двумя параметрами.
        /// </summary>
        private double CalculateParameterDistance(IIStrategyParameter param1, IIStrategyParameter param2)
        {
            switch (param1.Type)
            {
                case StrategyParameterType.Bool:
                    var bool1 = ((StrategyParameterBool)param1).ValueBool;
                    var bool2 = ((StrategyParameterBool)param2).ValueBool;
                    return bool1 == bool2 ? 0.0 : 1.0;

                case StrategyParameterType.Int:
                    var int1 = ((StrategyParameterInt)param1).ValueInt;
                    var int2 = ((StrategyParameterInt)param2).ValueInt;
                    var intRange = ((StrategyParameterInt)param1).ValueIntStop - ((StrategyParameterInt)param1).ValueIntStart;
                    return intRange > 0 ? Math.Abs(int1 - int2) / (double)intRange : 0.0;

                case StrategyParameterType.Decimal:
                    var decimal1 = ((StrategyParameterDecimal)param1).ValueDecimal;
                    var decimal2 = ((StrategyParameterDecimal)param2).ValueDecimal;
                    var decimalRange = ((StrategyParameterDecimal)param1).ValueDecimalStop - ((StrategyParameterDecimal)param1).ValueDecimalStart;
                    return (double)decimalRange > 0 ? Math.Abs((double)(decimal1 - decimal2)) / (double)decimalRange : 0.0;

                case StrategyParameterType.String:
                    var string1 = ((StrategyParameterString)param1).ValueString;
                    var string2 = ((StrategyParameterString)param2).ValueString;
                    return string1 == string2 ? 0.0 : 1.0;

                case StrategyParameterType.TimeOfDay:
                    var time1 = ((StrategyParameterTimeOfDay)param1).Value;
                    var time2 = ((StrategyParameterTimeOfDay)param2).Value;
                    var time1Minutes = time1.Hour * 60 + time1.Minute;
                    var time2Minutes = time2.Hour * 60 + time2.Minute;
                    var timeDiff = Math.Abs(time1Minutes - time2Minutes);
                    return timeDiff / 1440.0; // Normalize to 0-1 (minutes in a day)

                case StrategyParameterType.CheckBox:
                    var check1 = ((StrategyParameterCheckBox)param1).CheckState;
                    var check2 = ((StrategyParameterCheckBox)param2).CheckState;
                    return check1 == check2 ? 0.0 : 1.0;

                case StrategyParameterType.DecimalCheckBox:
                    var decimalCheck1 = ((StrategyParameterDecimalCheckBox)param1);
                    var decimalCheck2 = ((StrategyParameterDecimalCheckBox)param2);
                    var decimalCheckRange = decimalCheck1.ValueDecimalStop - decimalCheck1.ValueDecimalStart;
                    var valueDistance = (double)decimalCheckRange > 0 ? Math.Abs((double)(decimalCheck1.ValueDecimal - decimalCheck2.ValueDecimal)) / (double)decimalCheckRange : 0.0;
                    var checkDistance = decimalCheck1.CheckState == decimalCheck2.CheckState ? 0.0 : 1.0;
                    return (valueDistance + checkDistance) / 2.0;

                default:
                    return 0.0;
            }
        }
    }

    /// <summary>
    /// Selection methods for genetic algorithm.
    /// Методы селекции для генетического алгоритма.
    /// </summary>
    public enum SelectionMethod
    {
        /// <summary>
        /// Tournament selection / Турнирная селекция
        /// </summary>
        Tournament,

        /// <summary>
        /// Roulette wheel selection / Селекция рулетки
        /// </summary>
        Roulette,

        /// <summary>
        /// Rank-based selection / Селекция на основе ранга
        /// </summary>
        Rank,

        /// <summary>
        /// Random selection / Случайная селекция
        /// </summary>
        Random
    }
}
