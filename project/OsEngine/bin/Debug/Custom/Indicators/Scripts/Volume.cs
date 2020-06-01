using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace CustomIndicators.Scripts
{
    public class Volume : Aindicator
    {
        private IndicatorDataSeries _series;

        public override void OnStateChange(IndicatorState state)
        {
            _series = CreateSeries("Volume", Color.White, IndicatorChartPaintType.Column, true);
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            _series.Values[index] = GetValueVolume(candles, index);
        }
        private decimal GetValueVolume(List<Candle> candles, int index)
        {
            System.Drawing.Color color;
            if (candles[index].Close > candles[index].Open)
                color = Color.LawnGreen;
            else
                color = Color.Red;
            _series.Color = color;
            return candles[index].Volume;
        }
    }
}