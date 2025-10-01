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
    /// Represents an individual in the genetic algorithm population.
    /// Представляет особь в популяции генетического алгоритма.
    /// </summary>
    public class Individual
    {
        /// <summary>
        /// Strategy parameters for this individual.
        /// Параметры стратегии для этой особи.
        /// </summary>
        public List<IIStrategyParameter> Parameters { get; set; }

        /// <summary>
        /// Fitness score of this individual.
        /// Оценка пригодности этой особи.
        /// </summary>
        public double Fitness { get; set; }

        /// <summary>
        /// Optimization report for this individual.
        /// Отчет об оптимизации для этой особи.
        /// </summary>
        public OptimizerReport Report { get; set; }

        /// <summary>
        /// Whether this individual has been evaluated.
        /// Была ли эта особь оценена.
        /// </summary>
        public bool IsEvaluated { get; set; }

        /// <summary>
        /// Generation when this individual was created.
        /// Поколение, когда была создана эта особь.
        /// </summary>
        public int Generation { get; set; }

        /// <summary>
        /// Unique identifier for this individual.
        /// Уникальный идентификатор для этой особи.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Initialize a new individual with given parameters.
        /// Инициализировать новую особь с заданными параметрами.
        /// </summary>
        /// <param name="parameters">Strategy parameters / Параметры стратегии</param>
        /// <param name="generation">Generation number / Номер поколения</param>
        public Individual(List<IIStrategyParameter> parameters, int generation = 0)
        {
            Parameters = CopyParameters(parameters);
            Fitness = 0.0;
            Report = null;
            IsEvaluated = false;
            Generation = generation;
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Create a copy of this individual.
        /// Создать копию этой особи.
        /// </summary>
        /// <returns>New individual with copied parameters / Новая особь с скопированными параметрами</returns>
        public Individual Clone()
        {
            var cloned = new Individual(Parameters, Generation)
            {
                Fitness = Fitness,
                Report = Report,
                IsEvaluated = IsEvaluated,
                Id = Guid.NewGuid()
            };
            return cloned;
        }

        /// <summary>
        /// Create a mutated version of this individual.
        /// Создать мутированную версию этой особи.
        /// </summary>
        /// <param name="mutationRate">Probability of mutation for each parameter / Вероятность мутации для каждого параметра</param>
        /// <param name="mutationStrength">Strength of mutation / Сила мутации</param>
        /// <returns>New mutated individual / Новая мутированная особь</returns>
        public Individual Mutate(double mutationRate, double mutationStrength = 0.1)
        {
            var mutated = Clone();
            mutated.Generation = Generation + 1;
            mutated.IsEvaluated = false;
            mutated.Report = null;

            var random = new Random();

            for (int i = 0; i < mutated.Parameters.Count; i++)
            {
                if (random.NextDouble() < mutationRate)
                {
                    mutated.Parameters[i] = MutateParameter(mutated.Parameters[i], mutationStrength, random);
                }
            }

            return mutated;
        }

        /// <summary>
        /// Create offspring by crossing this individual with another.
        /// Создать потомка путем скрещивания этой особи с другой.
        /// </summary>
        /// <param name="other">Other parent individual / Другая родительская особь</param>
        /// <param name="crossoverRate">Probability of crossover / Вероятность скрещивания</param>
        /// <returns>New offspring individual / Новая особь-потомок</returns>
        public Individual Crossover(Individual other, double crossoverRate = 0.8)
        {
            var random = new Random();
            if (random.NextDouble() > crossoverRate)
            {
                // No crossover, return copy of this individual
                return Clone();
            }

            var offspring = new Individual(Parameters, Math.Max(Generation, other.Generation) + 1);

            for (int i = 0; i < offspring.Parameters.Count; i++)
            {
                if (random.NextDouble() < 0.5)
                {
                    // Take parameter from other parent
                    offspring.Parameters[i] = CopyParameter(other.Parameters[i]);
                }
                // Otherwise keep parameter from this parent (already copied)
            }

            return offspring;
        }

        /// <summary>
        /// Get parameter values as a dictionary for easy access.
        /// Получить значения параметров в виде словаря для удобного доступа.
        /// </summary>
        /// <returns>Dictionary of parameter names and values / Словарь имен параметров и значений</returns>
        public Dictionary<string, object> GetParameterValues()
        {
            var values = new Dictionary<string, object>();

            foreach (var param in Parameters)
            {
                switch (param.Type)
                {
                    case StrategyParameterType.Bool:
                        values[param.Name] = ((StrategyParameterBool)param).ValueBool;
                        break;
                    case StrategyParameterType.Int:
                        values[param.Name] = ((StrategyParameterInt)param).ValueInt;
                        break;
                    case StrategyParameterType.Decimal:
                        values[param.Name] = ((StrategyParameterDecimal)param).ValueDecimal;
                        break;
                    case StrategyParameterType.String:
                        values[param.Name] = ((StrategyParameterString)param).ValueString;
                        break;
                    case StrategyParameterType.TimeOfDay:
                        values[param.Name] = ((StrategyParameterTimeOfDay)param).Value;
                        break;
                    case StrategyParameterType.CheckBox:
                        values[param.Name] = ((StrategyParameterCheckBox)param).CheckState;
                        break;
                    case StrategyParameterType.DecimalCheckBox:
                        values[param.Name] = ((StrategyParameterDecimalCheckBox)param).ValueDecimal;
                        break;
                }
            }

            return values;
        }

        /// <summary>
        /// Copy strategy parameters list.
        /// Скопировать список параметров стратегии.
        /// </summary>
        private static List<IIStrategyParameter> CopyParameters(List<IIStrategyParameter> parameters)
        {
            var copied = new List<IIStrategyParameter>();

            foreach (var param in parameters)
            {
                copied.Add(CopyParameter(param));
            }

            return copied;
        }

        /// <summary>
        /// Copy a single strategy parameter.
        /// Скопировать один параметр стратегии.
        /// </summary>
        public static IIStrategyParameter CopyParameter(IIStrategyParameter parameter)
        {
            switch (parameter.Type)
            {
                case StrategyParameterType.Bool:
                    var boolParam = (StrategyParameterBool)parameter;
                    return new StrategyParameterBool(parameter.Name, boolParam.ValueBool);

                case StrategyParameterType.Int:
                    var intParam = (StrategyParameterInt)parameter;
                    var newIntParam = new StrategyParameterInt(parameter.Name,
                        intParam.ValueIntDefolt, intParam.ValueIntStart, intParam.ValueIntStop, intParam.ValueIntStep);
                    newIntParam.ValueInt = intParam.ValueInt;
                    return newIntParam;

                case StrategyParameterType.Decimal:
                    var decimalParam = (StrategyParameterDecimal)parameter;
                    var newDecimalParam = new StrategyParameterDecimal(parameter.Name,
                        decimalParam.ValueDecimalDefolt, decimalParam.ValueDecimalStart, decimalParam.ValueDecimalStop, decimalParam.ValueDecimalStep);
                    newDecimalParam.ValueDecimal = decimalParam.ValueDecimal;
                    return newDecimalParam;

                case StrategyParameterType.String:
                    var stringParam = (StrategyParameterString)parameter;
                    return new StrategyParameterString(parameter.Name, stringParam.ValueString, stringParam.ValuesString);

                case StrategyParameterType.TimeOfDay:
                    var timeParam = (StrategyParameterTimeOfDay)parameter;
                    return new StrategyParameterTimeOfDay(parameter.Name, timeParam.Value.Hour, timeParam.Value.Minute, timeParam.Value.Second, timeParam.Value.Millisecond);

                case StrategyParameterType.CheckBox:
                    var checkParam = (StrategyParameterCheckBox)parameter;
                    return new StrategyParameterCheckBox(parameter.Name, checkParam.CheckState == System.Windows.Forms.CheckState.Checked);

                case StrategyParameterType.DecimalCheckBox:
                    var decimalCheckParam = (StrategyParameterDecimalCheckBox)parameter;
                    var newDecimalCheckParam = new StrategyParameterDecimalCheckBox(parameter.Name,
                        decimalCheckParam.ValueDecimalDefolt, decimalCheckParam.ValueDecimalStart, decimalCheckParam.ValueDecimalStop, decimalCheckParam.ValueDecimalStep,
                        decimalCheckParam.CheckState == System.Windows.Forms.CheckState.Checked);
                    newDecimalCheckParam.ValueDecimal = decimalCheckParam.ValueDecimal;
                    return newDecimalCheckParam;

                default:
                    throw new ArgumentException($"Unsupported parameter type: {parameter.Type}");
            }
        }

        /// <summary>
        /// Mutate a single parameter.
        /// Мутировать один параметр.
        /// </summary>
        private static IIStrategyParameter MutateParameter(IIStrategyParameter parameter, double mutationStrength, Random random)
        {
            switch (parameter.Type)
            {
                case StrategyParameterType.Bool:
                    var boolParam = (StrategyParameterBool)parameter;
                    return new StrategyParameterBool(parameter.Name, !boolParam.ValueBool);

                case StrategyParameterType.Int:
                    var intParam = (StrategyParameterInt)parameter;
                    var range = intParam.ValueIntStop - intParam.ValueIntStart;
                    var mutation = (int)(range * mutationStrength * (random.NextDouble() - 0.5) * 2);
                    var newValue = Math.Max(intParam.ValueIntStart, Math.Min(intParam.ValueIntStop, intParam.ValueInt + mutation));
                    var newIntParam = new StrategyParameterInt(parameter.Name,
                        intParam.ValueIntDefolt, intParam.ValueIntStart, intParam.ValueIntStop, intParam.ValueIntStep);
                    newIntParam.ValueInt = newValue;
                    return newIntParam;

                case StrategyParameterType.Decimal:
                    var decimalParam = (StrategyParameterDecimal)parameter;
                    var decimalRange = (double)(decimalParam.ValueDecimalStop - decimalParam.ValueDecimalStart);
                    var decimalMutation = decimalRange * mutationStrength * (random.NextDouble() - 0.5) * 2;
                    var newDecimalValue = Math.Max(decimalParam.ValueDecimalStart, Math.Min(decimalParam.ValueDecimalStop, decimalParam.ValueDecimal + (decimal)decimalMutation));
                    var newDecimalParam = new StrategyParameterDecimal(parameter.Name,
                        decimalParam.ValueDecimalDefolt, decimalParam.ValueDecimalStart, decimalParam.ValueDecimalStop, decimalParam.ValueDecimalStep);
                    newDecimalParam.ValueDecimal = newDecimalValue;
                    return newDecimalParam;

                case StrategyParameterType.String:
                    var stringParam = (StrategyParameterString)parameter;
                    if (stringParam.ValuesString != null && stringParam.ValuesString.Count > 1)
                    {
                        var randomIndex = random.Next(stringParam.ValuesString.Count);
                        return new StrategyParameterString(parameter.Name, stringParam.ValuesString[randomIndex], stringParam.ValuesString);
                    }
                    return CopyParameter(parameter);

                case StrategyParameterType.TimeOfDay:
                    var timeParam = (StrategyParameterTimeOfDay)parameter;
                    var timeMutation = random.Next(-60, 61); // ±1 hour in minutes
                    var totalMinutes = timeParam.Value.Hour * 60 + timeParam.Value.Minute + timeMutation;
                    if (totalMinutes < 0) totalMinutes += 1440; // Wrap around
                    if (totalMinutes >= 1440) totalMinutes -= 1440;
                    var newHour = totalMinutes / 60;
                    var newMinute = totalMinutes % 60;
                    return new StrategyParameterTimeOfDay(parameter.Name, newHour, newMinute, 0, 0);

                case StrategyParameterType.CheckBox:
                    var checkParam = (StrategyParameterCheckBox)parameter;
                    var newCheckState = checkParam.CheckState != System.Windows.Forms.CheckState.Checked;
                    return new StrategyParameterCheckBox(parameter.Name, newCheckState);

                case StrategyParameterType.DecimalCheckBox:
                    var decimalCheckParam = (StrategyParameterDecimalCheckBox)parameter;
                    var newDecimalCheckParam = new StrategyParameterDecimalCheckBox(parameter.Name,
                        decimalCheckParam.ValueDecimalDefolt, decimalCheckParam.ValueDecimalStart, decimalCheckParam.ValueDecimalStop, decimalCheckParam.ValueDecimalStep,
                        decimalCheckParam.CheckState != System.Windows.Forms.CheckState.Checked);
                    newDecimalCheckParam.ValueDecimal = decimalCheckParam.ValueDecimal;
                    return newDecimalCheckParam;

                default:
                    return CopyParameter(parameter);
            }
        }
    }
}
