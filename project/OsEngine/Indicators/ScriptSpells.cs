using System.Collections.Generic;
using OsEngine.Entity;

namespace OsEngine.Indicators
{
    public static class ScriptSpells
    {

        public static decimal Summ(this List<Candle> values, int startIndex, int endIndex, string type)
        {
            decimal result = 0;

            // Add null check to prevent AccessViolationException
            // Добавляем проверку на null для предотвращения AccessViolationException
            if (values == null)
            {
                return result;
            }

            if (endIndex < startIndex)
            {
                int i = endIndex;
                endIndex = startIndex;
                startIndex = i;
            }

            if (startIndex < 0)
            {
                startIndex = 0;
            }

            if (endIndex >= values.Count)
            {
                endIndex = values.Count - 1;
            }

            // Additional bounds check to prevent memory access violations
            // Дополнительная проверка границ для предотвращения нарушений доступа к памяти
            if (startIndex >= values.Count || endIndex < 0)
            {
                return result;
            }

            for (int i = startIndex + 1; i < endIndex + 1; i++)
            {
                // Add null check for individual candle to prevent AccessViolationException
                // Добавляем проверку на null для отдельных свечей для предотвращения AccessViolationException
                if (i >= 0 && i < values.Count && values[i] != null)
                {
                    result += values[i].GetPoint(type);
                }
            }

            return result;
        }

        public static List<decimal> ByName(this List<IndicatorDataSeries> values, string name)
        {
            // Add null check to prevent AccessViolationException
            // Добавляем проверку на null для предотвращения AccessViolationException
            if (values == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (int i = 0; i < values.Count; i++)
            {
                // Add null check for individual indicator data series
                // Добавляем проверку на null для отдельных серий данных индикатора
                if (values[i] != null && values[i].Name == name)
                {
                    return values[i].Values;
                }
            }

            return null;
        }

        public static decimal Highest(this List<Candle> values, int startIndex, int endIndex)
        {
            // Add null check to prevent AccessViolationException
            // Добавляем проверку на null для предотвращения AccessViolationException
            if (values == null)
            {
                return 0;
            }

            if (endIndex < startIndex)
            {
                int i = endIndex;
                endIndex = startIndex;
                startIndex = i;
            }

            if (startIndex < 0)
            {
                startIndex = 0;
            }

            if (endIndex >= values.Count)
            {
                endIndex = values.Count - 1;
            }

            if (endIndex == startIndex)
            {
                return 0;
            }

            // Additional bounds check to prevent memory access violations
            // Дополнительная проверка границ для предотвращения нарушений доступа к памяти
            if (startIndex >= values.Count || endIndex < 0)
            {
                return 0;
            }

            decimal result = decimal.MinValue;

            for (int i = startIndex + 1; i < endIndex + 1; i++)
            {
                // Add null check for individual candle to prevent AccessViolationException
                // Добавляем проверку на null для отдельных свечей для предотвращения AccessViolationException
                if (i >= 0 && i < values.Count && values[i] != null)
                {
                    if (values[i].High > result)
                    {
                        result = values[i].High;
                    }
                }
            }

            return result;
        }

        public static decimal Lowest(this List<Candle> values, int startIndex, int endIndex)
        {
            // Add null check to prevent AccessViolationException
            // Добавляем проверку на null для предотвращения AccessViolationException
            if (values == null)
            {
                return 0;
            }

            if (endIndex < startIndex)
            {
                int i = endIndex;
                endIndex = startIndex;
                startIndex = i;
            }

            if (startIndex < 0)
            {
                startIndex = 0;
            }

            if (endIndex >= values.Count)
            {
                endIndex = values.Count - 1;
            }

            if (endIndex == startIndex)
            {
                return 0;
            }

            // Additional bounds check to prevent memory access violations
            // Дополнительная проверка границ для предотвращения нарушений доступа к памяти
            if (startIndex >= values.Count || endIndex < 0)
            {
                return 0;
            }

            decimal result = decimal.MaxValue;

            for (int i = startIndex + 1; i < endIndex + 1; i++)
            {
                // Add null check for individual candle to prevent AccessViolationException
                // Добавляем проверку на null для отдельных свечей для предотвращения AccessViolationException
                if (i >= 0 && i < values.Count && values[i] != null)
                {
                    if (values[i].Low < result)
                    {
                        result = values[i].Low;
                    }
                }
            }

            return result;
        }

    }
}
