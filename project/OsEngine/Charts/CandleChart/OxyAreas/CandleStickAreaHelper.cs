using OsEngine.Entity;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OsEngine.Charts.CandleChart.OxyAreas
{
    /// <summary>
    /// Helper class containing utility functions for CandleStickArea
    /// Вспомогательный класс, содержащий утилитарные функции для CandleStickArea
    /// </summary>
    public static class CandleStickAreaHelper
    {
        
        /// <summary>
        /// Get the number of decimal places in a decimal number
        /// Получаем количество знаков после запятой в десятичном числе
        /// </summary>
        public static int GetDecimalsCount(decimal d)
        {
            int i = 0;
            while (d * GetPow(10, 1 + i) % 10 != 0) { i++; }
            return i;
        }

        /// <summary>
        /// Calculate power of a decimal number
        /// Вычисляем степень десятичного числа
        /// </summary>
        public static decimal GetPow(decimal num, int pow)
        {
            decimal num_n = 1;
            for (int i = 0; i < pow; i++)
            {
                num_n *= num;
            }
            return num_n;
        }

        /// <summary>
        /// Create scatter series for position markers
        /// Создаем точечные серии для маркеров позиций
        /// </summary>
        public static ScatterSeries CreatePositionScatterSeries(string tag, OxyColor fillColor, ScreenPoint[] outline)
        {
            return new ScatterSeries()
            {
                Tag = tag,
                MarkerType = MarkerType.Custom,
                MarkerSize = 4,
                MarkerStrokeThickness = 0.5,
                MarkerStroke = OxyColors.AliceBlue,
                EdgeRenderingMode = EdgeRenderingMode.Adaptive,
                MarkerFill = fillColor,
                MarkerOutline = outline
            };
        }

        /// <summary>
        /// Filter positions by direction and state
        /// Фильтруем позиции по направлению и состоянию
        /// </summary>
        public static Position[] FilterPositionsByDirection(List<Position> deals, Side direction)
        {
            return deals.Where(x => x.Direction == direction && 
                                   x.State != PositionStateType.OpeningFail && 
                                   x.EntryPrice != 0).ToArray();
        }

        /// <summary>
        /// Create scatter points for position entries
        /// Создаем точки данных для входов в позиции
        /// </summary>
        public static List<ScatterPoint> CreateEntryPoints(Position[] positions)
        {
            return positions.Select(x => new ScatterPoint(
                DateTimeAxis.ToDouble(x.TimeOpen), 
                (double)x.EntryPrice)).ToList();
        }

        /// <summary>
        /// Create scatter points for position exits
        /// Создаем точки данных для выходов из позиций
        /// </summary>
        public static List<ScatterPoint> CreateExitPoints(Position[] positions)
        {
            return positions.Where(x => x.State == PositionStateType.Done)
                           .Select(x => new ScatterPoint(
                               DateTimeAxis.ToDouble(x.TimeClose), 
                               (double)x.ClosePrice)).ToList();
        }



        /// <summary>
        /// Calculate time step for indicator series
        /// Вычисляем временной шаг для серий индикаторов
        /// </summary>
        public static double CalculateTimeStep(TimeSpan timeFrameSpan)
        {
            return 1 / (1000 * 60 * 60 * 24 / timeFrameSpan.TotalMilliseconds);
        }

        /// <summary>
        /// Create data points for indicator series
        /// Создаем точки данных для серий индикаторов
        /// </summary>
        public static List<DataPoint> CreateIndicatorDataPoints(List<decimal> dataPoints, List<DateTime> candleTimes)
        {
            var points = new List<DataPoint>();
            
            for (int i = 0; i < dataPoints.Count && i < candleTimes.Count; i++)
            {
                double value = (double)dataPoints[i];
                if (value == 0)
                    value = double.NaN;
                    
                points.Add(new DataPoint(DateTimeAxis.ToDouble(candleTimes[i]), value));
            }
            
            return points;
        }
    }
}
