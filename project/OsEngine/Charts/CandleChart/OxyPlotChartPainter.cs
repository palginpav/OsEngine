using CustomAnnotations;
using OsEngine.Alerts;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Entities;
using OsEngine.Charts.CandleChart.OxyAreas;
using OsEngine.Charts.ColorKeeper;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Logging;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Integration;
using System.Windows.Input;



namespace OsEngine.Charts.CandleChart
{
    public class OxyChartPainter : IChartPainter
    {
        public event Action<string, LogMessageType> LogMessageEvent;
        public event Action<int> ClickToIndexEvent;
        public event Action<int> SizeAxisXChangeEvent;
        public event Action<ChartClickType> ChartClickEvent;
        public event Action<int> LastXIndexChangeEvent;
        public Task delay = new Task(() => { return; });

        public bool IsPatternChart { get; set; }
        private WindowsFormsHost host;
        public StartProgram start_program;
        public string chart_name;
        public string bot_name;
        public int bot_tab;
        private ChartMasterColorKeeper color_keeper;
        private System.Windows.Forms.Panel panel_winforms;
        private bool isPaint = false;
        private System.Windows.Controls.Grid main_grid_chart;
        public TimeSpan time_frame_span = new TimeSpan();
        public TimeFrame time_frame = new TimeFrame();

        private List<GridSplitter> splitters = new List<GridSplitter>();
        public OxyMediator mediator;

        public List<IndicatorSeria> series = new List<IndicatorSeria>();
        private List<OxyArea> all_areas = new List<OxyArea>();

        public event Action UpdateCandlesEvent;
        public event Action UpdateIndicatorEvent;

        public bool can_draw = false;

        public int OpenChartScale { get; set; }

        // Track exchange, trading pair, and timeframe for robust indicator update detection
        // Отслеживание биржи, торговой пары и таймфрейма для надежного обновления индикаторов
        private string _currentExchange = "";
        private string _currentTradingPair = "";
        private TimeFrame _currentTimeFrame = TimeFrame.Sec1;

        public OxyChartPainter(string name, StartProgram startProgram)
        {
            this.chart_name = name;
            
            // Parse bot name and tab number safely
            // Безопасный парсинг имени бота и номера вкладки
            string[] nameParts = name.Replace("tab", ";").Split(';');
            this.bot_name = nameParts[0];
            
            // Default to 0 if no tab number is found
            // По умолчанию 0, если номер вкладки не найден
            if (nameParts.Length > 1 && int.TryParse(nameParts[1], out int tabNumber))
            {
                this.bot_tab = tabNumber;
            }
            else
            {
                this.bot_tab = 0;
            }
            
            start_program = startProgram;
            color_keeper = new ChartMasterColorKeeper(name);
            mediator = new OxyMediator(this);
        }

        public void SendCandlesUpdated()
        {
            UpdateCandlesEvent?.Invoke();
        }

        public void SendIndicatorsUpdated()
        {
            UpdateIndicatorEvent?.Invoke();
        }

        public void MainChartMouseButtonClick(ChartClickType click_type)
        {
            ChartClickEvent?.Invoke(click_type);
        }

        public bool AreaIsCreate(string area_name)
        {
            if (area_name == "Prime")
                return true;
            
            foreach (var area in all_areas)
            {
                string areaTag = area.Tag?.ToString() ?? "";
                
                // Try multiple comparison methods to handle different types
                // Попробовать несколько методов сравнения для обработки различных типов
                if (area.Tag != null && 
                    (area.Tag.ToString() == area_name || 
                     area.Tag.Equals(area_name) ||
                     areaTag == area_name))
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearAlerts(List<IIAlert> alertArray)
        {
            //throw new NotImplementedException();
        }

        public void ClearDataPointsAndSizeValue()
        {
           
            for (int i = 0; i < all_areas.Count; i++)
            {
                if (all_areas[i].chart_name == this.chart_name)
                {
                    all_areas[i].Dispose();
                }
            }
        }

        public void MoveChartToTheRight(int scaleSize)
        {
            // Implementation needed for OxyPlot
            // Реализация необходима для OxyPlot
        }

        public void ClearSeries()
        {
            series.Clear();
        }

        public string CreateArea(string nameArea, int height)
        {
            if (all_areas.Exists(x => (string)x.Tag == nameArea))
            {
                return nameArea;
            }

            if (nameArea == "Prime" || all_areas.Exists(x => (string)x.Tag == nameArea))
            {
                return nameArea;
            }

            var indicator_chart = new IndicatorArea(new OxyAreaSettings()
            {
                cursor_X_is_active = true,
                cursor_Y_is_active = true,
                Tag = nameArea,
                AbsoluteMinimum = double.MinValue,
                Y_Axies_is_visible = true,
                X_Axies_is_visible = true,
                brush_background = "#111721"
            }, all_areas, nameArea, this);

            indicator_chart.indicator_name = nameArea.Replace("Area", ";").Split(';')[0];
            indicator_chart.bot_tab = this.bot_tab;

            indicator_chart.plot_model.Axes[0].TextColor = OxyColors.Transparent;
            indicator_chart.plot_model.Axes[0].TicklineColor = OxyColors.Transparent;
            indicator_chart.plot_model.Axes[0].AxisDistance = -50;
            indicator_chart.plot_model.Axes[1].IntervalLength = 10;
            indicator_chart.plot_model.Axes[1].MinorGridlineStyle = LineStyle.None;
            indicator_chart.plot_model.PlotMargins = new OxyThickness(0, indicator_chart.plot_model.PlotMargins.Top, indicator_chart.plot_model.PlotMargins.Right, indicator_chart.plot_model.PlotMargins.Bottom);
            indicator_chart.plot_model.Padding = new OxyThickness(0, 0, indicator_chart.plot_model.Padding.Right, 0);
            indicator_chart.plot_model.PlotMargins = new OxyThickness(0, 0, indicator_chart.plot_model.PlotMargins.Right, 0);
            indicator_chart.plot_view.Padding = new System.Windows.Thickness(0, 0, indicator_chart.plot_view.Padding.Right, 0);
            indicator_chart.plot_view.Margin = new System.Windows.Thickness(0, 0, indicator_chart.plot_view.Margin.Right, 0);

            all_areas.Add(indicator_chart);
            mediator.AddOxyArea(indicator_chart);

            // Add the new area to the existing chart layout without rebuilding everything
            // Добавить новую область в существующий макет чарта без перестройки всего
            if (main_grid_chart != null)
            {
                AddAreaToChartLayout(indicator_chart);
            }

            // Ensure synchronization after creating a new area
            // Обеспечить синхронизацию после создания новой области
            SynchronizeMediatorAreas();

            return nameArea;
        }

        public string CreateSeries(string areaName, IndicatorChartPaintType indicatorType, string seria_name)
        {
            var existingSeries = series.Find(x => (string)x.SeriaName == seria_name);
            if (existingSeries != null)
            {
                // Update existing series with correct indicator type
                // Обновить существующую серию с правильным типом индикатора
                existingSeries.IndicatorType = indicatorType;
                return seria_name;
            }

            var new_seria = new IndicatorSeria()
            {
                AreaName = areaName,
                IndicatorType = indicatorType,
                SeriaName = seria_name,
                BotTab = this.bot_tab
            };

            if (!series.Contains(new_seria))
            {
                series.Add(new IndicatorSeria()
                {
                    AreaName = areaName,
                    IndicatorType = indicatorType,
                    SeriaName = seria_name,
                    BotTab = this.bot_tab
                });
            }

            return seria_name;
        }

        public void CreateTickArea()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            Task delay = new Task(() =>
            {
                Thread.Sleep(1000);
            });

            delay.Start();
            delay.Wait();


            color_keeper.Delete();

            series.Clear();
            time_frame_span = new TimeSpan();
            time_frame = new TimeFrame();

            for (int i = 0; i < all_areas.Count; i++)
            {
                if (all_areas[i].chart_name == this.chart_name)
                {
                    all_areas[i].Dispose();
                }

                all_areas[i] = null;
            }

            mediator.Dispose();
            all_areas.Clear();
            

            if (main_grid_chart != null)
            {
                main_grid_chart.RowDefinitions.Clear();
                main_grid_chart.Children.Clear();
                main_grid_chart = null;
            }
        }

        public void DeleteIndicator(IIndicator indicator)
        {
            // Clear all series associated with this indicator
            // Очистить все серии, связанные с этим индикатором
            ClearIndicatorSeries(indicator);

            // If this is an indicator area (not Prime), remove the entire area
            // Если это область индикатора (не Prime), удалить всю область
            if (indicator.NameArea != "Prime")
            {
                var area = all_areas.Find(x => ((string)x.Tag).Contains(indicator.NameArea) && x.chart_name == this.chart_name);
                if (area != null)
                {
                    // Remove the area from data structures first
                    // Сначала удалить область из структур данных
                    var splitter = splitters.Find(x => (string)x.Tag == indicator.NameArea);
                    if (splitter != null)
                    {
                        splitters.Remove(splitter);
                    }

                    ((IndicatorArea)area).Dispose();
                    mediator.RemoveOxyArea(area);
                    all_areas.Remove(area);
                    
                    // Now remove the area from the grid layout (which will rebuild without the removed area)
                    // Теперь удалить область из макета сетки (которая перестроится без удаленной области)
                    RemoveAreaFromChartLayout(area);
                }
            }
            else
            {
                // For Prime area indicators, ensure immediate redraw
                // Для индикаторов области Prime обеспечить немедленную перерисовку
                var primeArea = all_areas.Find(x => x is CandleStickArea);
                if (primeArea != null)
                {
                    // Force immediate redraw of the Prime area
                    // Принудительно немедленно перерисовать область Prime
                    primeArea.Redraw();
                    
                    // Also trigger mediator redraw for Prime area
                    // Также запустить перерисовку медиатора для области Prime
                    mediator.RedrawPrime(true);
                }
            }

            // Force chart refresh
            // Принудительно обновить чарт
            RefreshChart();
        }

        public void DeleteTickArea()
        {
            throw new NotImplementedException();
        }

        public List<string> GetAreasNames()
        {
            return all_areas.Where(x => (string)x.Tag != "ControlPanel" && (string)x.Tag != "ScrollChart").Select(x => (string)x.Tag).ToList();
        }

        public int GetCursorSelectCandleNumber()
        {
            throw new NotImplementedException();
        }

        public decimal GetCursorSelectPrice()
        {
            throw new NotImplementedException();
        }

        public void GoChartToIndex(int index)
        {
            GoChartToTime(OxyArea.my_candles[index].TimeStart);
        }

        public void GoChartToTime(DateTime time)
        {
            foreach (var area in all_areas.Where(x => x.Tag != (object)"ScrollChart"))
            {
                Action action = () =>
                {
                    if (area is CandleStickArea)
                    {
                        var main_area = (CandleStickArea)area;
                        mediator.PrimeChart_BuildCandleSeries();
                        main_area.Calculate(area.owner.time_frame_span, area.owner.time_frame);
                    }

                    double first_value = area.plot_model.Axes[0].ActualMinimum;
                    double last_value = area.plot_model.Axes[0].ActualMinimum;

                    double main_volume = DateTimeAxis.ToDouble(time);

                    area.date_time_axis_X.Zoom(main_volume - (last_value - first_value) / 2, main_volume + (last_value - first_value) / 2);

                    List<double> max_min = new List<double>();

                    if (area is CandleStickArea)
                        max_min = area.GetHighLow(true, main_volume - (last_value - first_value) / 2, main_volume + (last_value - first_value) / 2);
                    else
                        max_min = area.GetHighLow(false, main_volume - (last_value - first_value) / 2, main_volume + (last_value - first_value) / 2);

                    area.linear_axis_Y.Zoom(max_min[0], max_min[1]);
                };

                area.plot_view.Dispatcher.Invoke(action);
            }
        }

        public void HideAreaOnChart()
        {
            foreach (var row in main_grid_chart.RowDefinitions.Where(x => (string)x.Tag != "Prime" && (string)x.Tag != "ScrollChart" && (string)x.Tag != "ControlPanel"))
                row.Height = new GridLength(0, GridUnitType.Pixel);
        }

        public bool IndicatorIsCreate(string name)
        {
            if (series.Exists(x => x.SeriaName == name))
                return true;
            else
                return false;            
        }

        public void PaintAlert(AlertToChart alert)
        {
            throw new NotImplementedException();
        }

        public void RemoveAlert(AlertToChart alertToChart)
        {
            throw new NotImplementedException();
        }

        public bool HaveAlertOnChart(AlertToChart alertToChart)
        {
            throw new NotImplementedException();
        }

        public void ProcessAlert(AlertToChart alert, bool needToWait)
        {
            throw new NotImplementedException();
        }

        public void PaintHorisiontalLineOnArea(LineHorisontal lineElement)
        {
            throw new NotImplementedException();
        }

        public void PaintHorizontalLineOnArea(LineHorisontal lineElement)
        {
            throw new NotImplementedException();
        }

        public void PaintInDifColor(int indexStart, int indexEnd, string seriesName)
        {
            throw new NotImplementedException();
        }

        public void PaintOneLine(System.Windows.Forms.DataVisualization.Charting.Series mySeries, List<Candle> candles, ChartAlertLine line, Color colorLine, int borderWidth, Color colorLabel, string label)
        {
            throw new NotImplementedException();
        }

        public void PaintPoint(PointElement point)
        {
            throw new NotImplementedException();
        }

        public void PaintSingleCandlePattern(List<Candle> candles)
        {
            throw new NotImplementedException();
        }

        public void PaintSingleVolumePattern(List<Candle> candles)
        {
            throw new NotImplementedException();
        }


        public void ProcessCandles(List<Candle> history)
        {
            if (isPaint == false)
            {
                return;
            }

            if (mediator.count_skiper > 0)
                mediator.count_skiper--;

            if (mediator.count_skiper > 0)
            {
                can_draw = false;
                return;
            }

            if (mediator.factor < 1)
            {
                Task delay = new Task(() =>
                {
                    Thread.Sleep((int)(50 / mediator.factor - 50));
                });

                delay.Start();
                delay.Wait();

                delay = new Task(() =>
                {
                    Delay((int)(50 / mediator.factor - 50)).Wait((int)(50 / mediator.factor - 50) + 50);
                });

                delay.Start();
                delay.Wait((int)(50 / mediator.factor - 50) + 100);
            }


            can_draw = true;

            OxyArea.my_candles = history;
     
            if (mediator.prime_chart != null)
            mediator.PrimeChart_BuildCandleSeries();

            if (mediator.prime_chart != null)
            UpdateCandlesEvent?.Invoke();
        }

        private async Task Delay(int millisec)
        {
            await Task.Delay(millisec);
        }

        public void ProcessClearElem(IChartElement element)
        {
            throw new NotImplementedException();
        }

        public void ProcessElem(IChartElement element)
        {
            if (isPaint == false || can_draw == false)
                return;

            mediator.ProcessElem(element);
        }

        public void ProcessIndicator(IIndicator indicator)
        {
            if (isPaint == false || can_draw == false)
                return;

            OxyArea area = all_areas.Find(x => (string)x.Tag == indicator.NameArea);
            if (area == null)
            {
                // Create the area if it doesn't exist (for non-Prime areas)
                // Создать область, если она не существует (для не-Prime областей)
                if (indicator.NameArea != "Prime")
                {
                    CreateArea(indicator.NameArea, 100); // Default height / Высота по умолчанию
                    area = all_areas.Find(x => (string)x.Tag == indicator.NameArea);
                    if (area == null)
                        return; // Still can't find the area, something went wrong / Все еще не могу найти область, что-то пошло не так
                }
                else
                {
                    return; // Prime area should always exist / Область Prime должна всегда существовать
                }
            }

            // Ensure the area is properly registered with the mediator
            // Обеспечить правильную регистрацию области в медиаторе
            if (area is IndicatorArea && !mediator.indicators_list.Contains((IndicatorArea)area))
            {
                mediator.AddOxyArea(area);
            }

            // Ensure all existing indicator areas are properly synchronized with the mediator
            // Обеспечить правильную синхронизацию всех существующих областей индикаторов с медиатором
            // This is important when adding indicators to Prime area, as it might affect synchronization
            // Это важно при добавлении индикаторов в область Prime, так как это может повлиять на синхронизацию
            SynchronizeMediatorAreas();

            if (indicator.ValuesToChart != null)
            {
                if (indicator.PaintOn == false)
                {
                    // Clear indicator data when disabled
                    // Очистить данные индикатора при отключении
                    ClearIndicatorData(indicator);
                    
                    // For Prime area indicators, ensure immediate redraw
                    // Для индикаторов области Prime обеспечить немедленную перерисовку
                    if (indicator.NameArea == "Prime")
                    {
                        mediator.RedrawPrime(true);
                    }
                    else
                    {
                        area.Redraw();
                    }
                    
                    UpdateIndicatorEvent?.Invoke();
                    return;
                }

                List<List<decimal>> val_list = indicator.ValuesToChart;
                List<Color> colors = indicator.Colors;
                string name = indicator.Name;

                for (int i = 0; i < val_list.Count; i++)
                {
                    if (val_list[i] == null || val_list[i].Count == 0)
                        continue;

                    string seriaName = name + i.ToString();
                    var seria = series.Find(x => x.SeriaName == seriaName);
                    
                    if (seria == null)
                    {
                        // Create new series if it doesn't exist
                        // Создать новую серию, если она не существует
                        seria = new IndicatorSeria()
                        {
                            AreaName = indicator.NameArea,
                            IndicatorType = indicator.TypeIndicator,
                            SeriaName = seriaName,
                            BotTab = this.bot_tab
                        };
                        series.Add(seria);
                    }
                    else
                    {
                        // Update existing series with correct indicator type
                        // Обновить существующую серию с правильным типом индикатора
                        seria.IndicatorType = indicator.TypeIndicator;
                    }

                    seria.OxyColor = OxyColor.FromArgb(colors[i].A, colors[i].R, colors[i].G, colors[i].B);
                    seria.Count = val_list.Count;

                    try
                    {
                        area.BuildIndicatorSeries(seria, val_list[i], time_frame_span);
                    }
                    catch (Exception ex) 
                    { 
                        // Log error if needed
                        // Записать ошибку при необходимости
                        continue; 
                    }
                }

                // Force redraw after building all series
                // Принудительно перерисовать после построения всех серий
                area.Redraw();
                UpdateIndicatorEvent?.Invoke();
                
                // If this is a Prime area indicator, ensure all indicator areas are still synchronized
                // Если это индикатор области Prime, обеспечить синхронизацию всех областей индикаторов
                if (indicator.NameArea == "Prime")
                {
                    SynchronizeMediatorAreas();
                }
                
                return;
            }

            Aindicator ind = (Aindicator)indicator;
            List<IndicatorDataSeries> indi_series = ind.DataSeries;

            // Check if all series are disabled
            // Проверить, отключены ли все серии
            bool allSeriesDisabled = true;
            for (int i = 0; i < indi_series.Count; i++)
            {
                if (indi_series[i].IsPaint)
                {
                    allSeriesDisabled = false;
                    break;
                }
            }

            if (allSeriesDisabled)
            {
                // Clear indicator data when all series are disabled
                // Очистить данные индикатора, когда все серии отключены
                ClearIndicatorData(indicator);
                
                // For Prime area indicators, ensure immediate redraw
                // Для индикаторов области Prime обеспечить немедленную перерисовку
                if (indicator.NameArea == "Prime")
                {
                    mediator.RedrawPrime(true);
                }
                else
                {
                    area.Redraw();
                }
                
                UpdateIndicatorEvent?.Invoke();
                return;
            }

            for (int i = 0; i < indi_series.Count; i++)
            {
                if (indi_series[i].IsPaint == false)
                    continue;

                string seriaName = indi_series[i].NameSeries;
                var seria = series.Find(x => x.SeriaName == seriaName);
                
                if (seria == null)
                {
                    // Create new series if it doesn't exist
                    // Создать новую серию, если она не существует
                    seria = new IndicatorSeria()
                    {
                        AreaName = indicator.NameArea,
                        IndicatorType = indi_series[i].ChartPaintType,
                        SeriaName = seriaName,
                        BotTab = this.bot_tab
                    };
                    series.Add(seria);
                }
                else
                {
                    // Update existing series with correct indicator type
                    // Обновить существующую серию с правильным типом индикатора
                    seria.IndicatorType = indi_series[i].ChartPaintType;
                }

                seria.OxyColor = OxyColor.FromArgb(indi_series[i].Color.A, indi_series[i].Color.R, indi_series[i].Color.G, indi_series[i].Color.B);
                seria.Count = indi_series.Count;
                
                try
                {
                    mediator.BuildIndicatorSeries(area, seria, indi_series[i].Values, time_frame_span);
                }
                catch (Exception ex) 
                { 
                    // Log error if needed
                    // Записать ошибку при необходимости
                    continue; 
                }
            }

            // Force redraw after building all series
            // Принудительно перерисовать после построения всех серий
            area.Redraw();
            UpdateIndicatorEvent?.Invoke();
            
            // If this is a Prime area indicator, ensure all indicator areas are still synchronized
            // Если это индикатор области Prime, обеспечить синхронизацию всех областей индикаторов
            if (indicator.NameArea == "Prime")
            {
                SynchronizeMediatorAreas();
            }
        }

        private void ClearIndicatorData(IIndicator indicator)
        {
            if (indicator.ValuesToChart != null)
            {
                List<List<decimal>> val_list = indicator.ValuesToChart;
                string name = indicator.Name;

                for (int i = 0; i < val_list.Count; i++)
                {
                    string seriaName = name + i.ToString();
                    var seria = series.Find(x => x.SeriaName == seriaName);
                    if (seria != null)
                    {
                        seria.DataPoints = new List<decimal>();
                    }
                }
            }
            else
            {
                Aindicator ind = (Aindicator)indicator;
                foreach (var indi_series in ind.DataSeries)
                {
                    var seria = series.Find(x => x.SeriaName == indi_series.NameSeries);
                    if (seria != null)
                    {
                        seria.DataPoints = new List<decimal>();
                    }
                }
            }
        }

        public void ProcessPositions(List<Position> deals)
        {
            if (isPaint == false || can_draw == false)
                return;

            if (deals == null || deals.Count == 0)
                return;          

            mediator.ProcessPositions(deals);
        }

        public void ProcessTrades(List<Trade> trades)
        {
            //обработка нужна для отображение аска бида. устарело
        }

        public void ProcessStopLimits(List<PositionOpenerToStopLimit> stopLimits)
        {
            if (stopLimits == null || stopLimits.Count == 0)
                return;

            if (isPaint == false || can_draw == false)
                return;

            // Clear existing stop limit lines
            // Очистить существующие линии стоп-лимитов
            ClearStopLimitLines();

            // Add new stop limit lines
            // Добавить новые линии стоп-лимитов
            foreach (var stopLimit in stopLimits)
            {
                AddStopLimitLine(stopLimit);
            }
        }

        private void ClearStopLimitLines()
        {
            // Remove existing stop limit lines from all areas
            // Удалить существующие линии стоп-лимитов из всех областей
            foreach (var area in all_areas)
            {
                if (area.plot_model.Series != null)
                {
                    var seriesToRemove = area.plot_model.Series
                        .Where(s => s.Title != null && s.Title.StartsWith("StopLimit_"))
                        .ToList();

                    foreach (var series in seriesToRemove)
                    {
                        area.plot_model.Series.Remove(series);
                    }
                }
            }
        }

        private void AddStopLimitLine(PositionOpenerToStopLimit stopLimit)
        {
            // Find the Prime area (main chart area)
            // Найти область Prime (основная область чарта)
            var primeArea = all_areas.FirstOrDefault(a => (string)a.Tag == "Prime");
            if (primeArea == null)
                return;

            // Create a horizontal line series for the stop limit
            // Создать горизонтальную линейную серию для стоп-лимита
            var lineSeries = new OxyPlot.Series.LineSeries
            {
                Title = $"StopLimit_{stopLimit.Number}",
                Color = stopLimit.Side == Side.Buy ? OxyColors.DarkCyan : OxyColors.MediumVioletRed,
                StrokeThickness = 1,
                LineStyle = LineStyle.Solid
            };

            // Add points to create a horizontal line across the chart
            // Добавить точки для создания горизонтальной линии через весь чарт
            if (OxyArea.my_candles != null && OxyArea.my_candles.Count > 0)
            {
                lineSeries.Points.Add(new OxyPlot.DataPoint(0, (double)stopLimit.PriceRedLine));
                lineSeries.Points.Add(new OxyPlot.DataPoint(OxyArea.my_candles.Count + 500, (double)stopLimit.PriceRedLine));
            }

            primeArea.plot_model.Series.Add(lineSeries);
            primeArea.plot_model.InvalidatePlot(false);
        }

        public void RefreshChartColor()
        {
            // кто нить другой сделает
        }

        public void RemoveCursor()
        {
            // устарело
        }

        public void RePaintIndicator(IIndicator indicatorCandle)
        {
            if (isPaint == false || can_draw == false)
                return;

            if (indicatorCandle == null)
                return;

            // Clear existing indicator series for this indicator if it's being disabled
            // Очистить существующие серии индикаторов для этого индикатора, если он отключается
            if (!indicatorCandle.PaintOn)
            {
                ClearIndicatorSeries(indicatorCandle);
                
                // For Prime area indicators, ensure immediate redraw
                if (indicatorCandle.NameArea == "Prime")
                {
                    var primeArea = all_areas.Find(x => x is CandleStickArea);
                    if (primeArea != null)
                    {
                        primeArea.Redraw();
                        mediator.RedrawPrime(true);
                    }
                }
                
                RefreshChart();
                return;
            }

            if (indicatorCandle.NameSeries != null)
            {
                if (!indicatorCandle.PaintOn)
                    return;

                if (!all_areas.Exists(x => (string)x.Tag == indicatorCandle.NameArea) && indicatorCandle.NameArea != "Prime")
                {
                    var indicator_chart = new IndicatorArea(new OxyAreaSettings()
                    {
                        cursor_X_is_active = true,
                        cursor_Y_is_active = true,
                        Tag = indicatorCandle.NameArea,
                        AbsoluteMinimum = double.MinValue,
                        Y_Axies_is_visible = true,
                        X_Axies_is_visible = true,
                        brush_background = "#111721"
                    }, all_areas, indicatorCandle.NameArea, this);

                    indicator_chart.indicator_name = indicatorCandle.NameArea.Replace("Area", ";").Split(';')[0];
                    indicator_chart.bot_tab = this.bot_tab;
                    indicator_chart.bot_name = this.bot_name;

                    indicator_chart.plot_model.Axes[0].TextColor = OxyColors.Transparent;
                    indicator_chart.plot_model.Axes[0].TicklineColor = OxyColors.Transparent;
                    indicator_chart.plot_model.Axes[0].AxisDistance = -50;
                    indicator_chart.plot_model.Axes[1].IntervalLength = 10;
                    indicator_chart.plot_model.Axes[1].MinorGridlineStyle = LineStyle.None;
                    indicator_chart.plot_model.PlotMargins = new OxyThickness(0, indicator_chart.plot_model.PlotMargins.Top, indicator_chart.plot_model.PlotMargins.Right, indicator_chart.plot_model.PlotMargins.Bottom);
                    indicator_chart.plot_model.Padding = new OxyThickness(0, 0, indicator_chart.plot_model.Padding.Right, 0);
                    indicator_chart.plot_model.PlotMargins = new OxyThickness(0, 0, indicator_chart.plot_model.PlotMargins.Right, 0);
                    indicator_chart.plot_view.Padding = new System.Windows.Thickness(0, 0, indicator_chart.plot_view.Padding.Right, 0);
                    indicator_chart.plot_view.Margin = new System.Windows.Thickness(0, 0, indicator_chart.plot_view.Margin.Right, 0);

                    all_areas.Add(indicator_chart);
                    mediator.AddOxyArea(indicator_chart);
                }

                var existingSeries = series.Find(x => x.SeriaName == indicatorCandle.NameSeries && x.AreaName == indicatorCandle.NameArea);
                if (existingSeries != null)
                {
                    // Update existing series with correct indicator type
                    existingSeries.IndicatorType = indicatorCandle.TypeIndicator;
                }
                else
                {
                    var indi_area = all_areas.FindLast(x => (string)x.Tag == indicatorCandle.NameArea);

                    if (indi_area == null)
                        return;

                    if (!indicatorCandle.NameSeries.StartsWith(this.bot_name))
                        return;

                    var new_seria = new IndicatorSeria()
                    {
                        AreaName = indicatorCandle.NameArea,
                        IndicatorType = indicatorCandle.TypeIndicator,
                        SeriaName = indicatorCandle.NameSeries,
                        BotTab = this.bot_tab
                    };

                    if (!series.Contains(new_seria))
                    {
                        series.Add(new IndicatorSeria()
                        {
                            AreaName = indicatorCandle.NameArea,
                            IndicatorType = indicatorCandle.TypeIndicator,
                            SeriaName = indicatorCandle.NameSeries,
                            BotTab = this.bot_tab
                        });
                    }
                }
            }
            else
            {
                foreach (var ser_name in ((Aindicator)indicatorCandle).DataSeries)
                {
                    if (!ser_name.IsPaint)
                        continue;

                    string seria_name = ser_name.NameSeries;

                    if (!all_areas.Exists(x => (string)x.Tag == indicatorCandle.NameArea) && indicatorCandle.NameArea != "Prime")
                    {
                        var indicator_chart = new IndicatorArea(new OxyAreaSettings()
                        {
                            cursor_X_is_active = true,
                            cursor_Y_is_active = true,
                            Tag = indicatorCandle.NameArea,
                            AbsoluteMinimum = double.MinValue,
                            Y_Axies_is_visible = true,
                            X_Axies_is_visible = true,
                            brush_background = "#111721"
                        }, all_areas, indicatorCandle.NameArea, this);

                        indicator_chart.indicator_name = indicatorCandle.NameArea.Replace("Area", ";").Split(';')[0];
                        indicator_chart.bot_tab = this.bot_tab;
                        indicator_chart.bot_name = this.bot_name;

                        indicator_chart.plot_model.Axes[0].TextColor = OxyColors.Transparent;
                        indicator_chart.plot_model.Axes[0].TicklineColor = OxyColors.Transparent;
                        indicator_chart.plot_model.Axes[0].AxisDistance = -50;
                        indicator_chart.plot_model.Axes[1].IntervalLength = 10;
                        indicator_chart.plot_model.Axes[1].MinorGridlineStyle = LineStyle.None;
                        indicator_chart.plot_model.PlotMargins = new OxyThickness(0, indicator_chart.plot_model.PlotMargins.Top, indicator_chart.plot_model.PlotMargins.Right, indicator_chart.plot_model.PlotMargins.Bottom);
                        indicator_chart.plot_model.Padding = new OxyThickness(0, 0, indicator_chart.plot_model.Padding.Right, 0);
                        indicator_chart.plot_model.PlotMargins = new OxyThickness(0, 0, indicator_chart.plot_model.PlotMargins.Right, 0);
                        indicator_chart.plot_view.Padding = new System.Windows.Thickness(0, 0, indicator_chart.plot_view.Padding.Right, 0);
                        indicator_chart.plot_view.Margin = new System.Windows.Thickness(0, 0, indicator_chart.plot_view.Margin.Right, 0);

                        all_areas.Add(indicator_chart);
                        mediator.AddOxyArea(indicator_chart);
                    }

                    var existingSeries = series.Find(x => x.SeriaName == seria_name && x.AreaName == indicatorCandle.NameArea);
                    if (existingSeries != null)
                    {
                        // Update existing series with correct indicator type for Aindicator
                        // Обновить существующую серию с правильным типом индикатора для Aindicator
                        existingSeries.IndicatorType = ser_name.ChartPaintType;
                    }
                    else
                    {
                        var indi_area = all_areas.FindLast(x => (string)x.Tag == indicatorCandle.NameArea);

                        if (indi_area == null)
                            return;

                        if (!seria_name.StartsWith(this.bot_name))
                            return;

                        var new_seria = new IndicatorSeria()
                        {
                            AreaName = indicatorCandle.NameArea,
                            IndicatorType = ser_name.ChartPaintType,
                            SeriaName = seria_name,
                            BotTab = this.bot_tab
                        };

                        if (!series.Contains(new_seria))
                        {
                            series.Add(new IndicatorSeria()
                            {
                                AreaName = indicatorCandle.NameArea,
                                IndicatorType = ser_name.ChartPaintType,
                                SeriaName = seria_name,
                                BotTab = this.bot_tab
                            });
                        }
                    }
                }
            }

            // Process the indicator to refresh the chart data
            // Обработать индикатор для обновления данных чарта
            ProcessIndicator(indicatorCandle);

            // Force chart refresh
            // Принудительно обновить чарт
            RefreshChart();
        }

        private void RefreshChart()
        {
            // Force refresh of all areas
            // Принудительно обновить все области
            foreach (var area in all_areas)
            {
                if (area.plot_model != null)
                {
                    area.plot_model.InvalidatePlot(true);
                }
            }

            // Ensure Prime area is properly redrawn
            // Обеспечить правильную перерисовку области Prime
            var primeArea = all_areas.Find(x => x is CandleStickArea);
            if (primeArea != null)
            {
                mediator.RedrawPrime(false);
            }

            // Trigger update events
            // Запустить события обновления
            UpdateIndicatorEvent?.Invoke();
            UpdateCandlesEvent?.Invoke();
        }

        private void AddAreaToChartLayout(IndicatorArea newArea)
        {
            try
            {
                System.Windows.Media.BrushConverter converter = new System.Windows.Media.BrushConverter();

                // Add a splitter before the new area
                // Добавить разделитель перед новой областью
                main_grid_chart.RowDefinitions.Add(new RowDefinition()
                {
                    Tag = "GridSplitter_" + newArea.Tag,
                    Height = new System.Windows.GridLength(3),
                });

                GridSplitter grid_splitter = new GridSplitter()
                {
                    ShowsPreview = false,
                    Tag = newArea.Tag,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Background = (System.Windows.Media.Brush)converter.ConvertFromString("#50BEFFD5"),
                };

                if (!splitters.Contains(grid_splitter))
                    splitters.Add(grid_splitter);

                System.Windows.Controls.Grid.SetColumn(grid_splitter, 0);
                System.Windows.Controls.Grid.SetRow(grid_splitter, main_grid_chart.RowDefinitions.Count - 1);

                main_grid_chart.Children.Add(grid_splitter);

                // Add the new area
                // Добавить новую область
                main_grid_chart.RowDefinitions.Add(new RowDefinition()
                {
                    Tag = newArea.Tag,
                    Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star),
                });

                System.Windows.Controls.Grid.SetRow(newArea.GetViewUI(), main_grid_chart.RowDefinitions.Count - 1);
                System.Windows.Controls.Grid.SetColumn(newArea.GetViewUI(), 0);

                main_grid_chart.Children.Add(newArea.GetViewUI());

                // Force a refresh of the new area
                // Принудительно обновить новую область
                newArea.plot_model.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                // If there's an error adding the area, fall back to full rebuild
                // Если есть ошибка при добавлении области, вернуться к полной перестройке
                main_grid_chart.Children.Clear();
                main_grid_chart.RowDefinitions.Clear();
                MakeChart(main_grid_chart);
            }
        }

        private void RemoveAreaFromChartLayout(OxyArea areaToRemove)
        {
            try
            {
                string areaTag = (string)areaToRemove.Tag;
                
                // Safety check: don't remove Prime area
                // Проверка безопасности: не удалять область Prime
                if (areaTag == "Prime")
                {
                    return;
                }
                
                // For now, use a simpler approach - just clear and rebuild the chart
                // Пока использовать более простой подход - просто очистить и перестроить чарт
                // This avoids the complex UI element manipulation that's causing build issues
                // Это избегает сложного манипулирования UI элементами, которые вызывают проблемы сборки
                main_grid_chart.Children.Clear();
                main_grid_chart.RowDefinitions.Clear();
                
                // Rebuild the chart without the removed area
                // Перестроить чарт без удаленной области
                MakeChart(main_grid_chart);
                
            }
            catch (Exception ex)
            {
                // If there's an error removing the area, fall back to full rebuild
                // Если есть ошибка при удалении области, вернуться к полной перестройке
                main_grid_chart.Children.Clear();
                main_grid_chart.RowDefinitions.Clear();
                MakeChart(main_grid_chart);
            }
        }



        private void ClearIndicatorSeries(IIndicator indicator)
        {
            // Clear series from the global series list
            var seriesToRemove = new List<IndicatorSeria>();

            if (indicator.ValuesToChart != null)
            {
                // For indicators with ValuesToChart (classic indicators)
                for (int i = 0; i < indicator.ValuesToChart.Count; i++)
                {
                    string seriaName = indicator.Name + i.ToString();
                    var seria = series.Find(x => x.SeriaName == seriaName);
                    if (seria != null)
                    {
                        seriesToRemove.Add(seria);
                    }
                }
            }
            else
            {
                // For indicators with DataSeries (Aindicator type)
                Aindicator ind = (Aindicator)indicator;
                foreach (var indi_series in ind.DataSeries)
                {
                    var seria = series.Find(x => x.SeriaName == indi_series.NameSeries);
                    if (seria != null)
                    {
                        seriesToRemove.Add(seria);
                    }
                }
            }

            // Remove series from global list
            foreach (var seria in seriesToRemove)
            {
                series.Remove(seria);
            }

            // Clear series from all areas and their internal lists
            foreach (var area in all_areas)
            {
                if (area is CandleStickArea candleArea)
                {
                    // Remove from internal lists in CandleStickArea
                    if (indicator.ValuesToChart != null)
                    {
                        // For classic indicators
                        for (int i = 0; i < indicator.ValuesToChart.Count; i++)
                        {
                            string seriaName = indicator.Name + i.ToString();
                            
                            // Remove from lines_series_list
                            candleArea.lines_series_list.RemoveAll(x => (string)x.Tag == seriaName);
                            
                            // Remove from linear_bar_series_list
                            candleArea.linear_bar_series_list.RemoveAll(x => (string)x.Tag == seriaName);
                            
                            // Remove from scatter_series_list
                            candleArea.scatter_series_list.RemoveAll(x => (string)x.Tag == seriaName);
                        }
                    }
                    else
                    {
                        // For Aindicator type
                        Aindicator ind = (Aindicator)indicator;
                        foreach (var indi_series in ind.DataSeries)
                        {
                            // Remove from lines_series_list
                            candleArea.lines_series_list.RemoveAll(x => (string)x.Tag == indi_series.NameSeries);
                            
                            // Remove from linear_bar_series_list
                            candleArea.linear_bar_series_list.RemoveAll(x => (string)x.Tag == indi_series.NameSeries);
                            
                            // Remove from scatter_series_list
                            candleArea.scatter_series_list.RemoveAll(x => (string)x.Tag == indi_series.NameSeries);
                        }
                    }
                    
                    // Redraw the area to update the chart
                    candleArea.Redraw();
                    
                    // For Prime area, also trigger mediator redraw to ensure proper update
                    if (indicator.NameArea == "Prime")
                    {
                        mediator.RedrawPrime(false);
                    }
                }
                else if (area is IndicatorArea indicatorArea)
                {
                    // For indicator areas, clear from plot model directly
                    if (area.plot_model.Series != null)
                    {
                        var plotSeriesToRemove = new List<OxyPlot.Series.Series>();

                        if (indicator.ValuesToChart != null)
                        {
                            // For classic indicators
                            for (int i = 0; i < indicator.ValuesToChart.Count; i++)
                            {
                                string seriaName = indicator.Name + i.ToString();
                                var plotSeries = area.plot_model.Series
                                    .Where(s => s.Title != null && s.Title == seriaName)
                                    .ToList();
                                plotSeriesToRemove.AddRange(plotSeries);
                            }
                        }
                        else
                        {
                            // For Aindicator type
                            Aindicator ind = (Aindicator)indicator;
                            foreach (var indi_series in ind.DataSeries)
                            {
                                var plotSeries = area.plot_model.Series
                                    .Where(s => s.Title != null && s.Title == indi_series.NameSeries)
                                    .ToList();
                                plotSeriesToRemove.AddRange(plotSeries);
                            }
                        }

                        // Remove series from plot model
                        foreach (var plotSeries in plotSeriesToRemove)
                        {
                            area.plot_model.Series.Remove(plotSeries);
                        }
                        
                        // Invalidate the plot
                        area.plot_model.InvalidatePlot(true);
                    }
                }
            }
        }

        public void SetBlackScheme()
        {
            // кто нить другой сделает не интересно
        }

        public void SetNewTimeFrame(TimeSpan timeFrameSpan, TimeFrame timeFrame)
        {
            // Check if timeframe has changed
            // Проверить, изменился ли таймфрейм
            if (this.time_frame != timeFrame)
            {
                // Notify about timeframe change for robust indicator updates
                // Уведомить об изменении таймфрейма для надежного обновления индикаторов
                SetCurrentTradingContext(_currentExchange, _currentTradingPair, timeFrame);
            }

            this.time_frame_span = timeFrameSpan;
            this.time_frame = timeFrame;

            foreach (var area in all_areas)
            {
                area.time_frame_span = timeFrameSpan;
                area.time_frame = timeFrame;
            }
        }

        public void SetPointSize(ChartPositionTradeSize pointSize)
        {
            // надо бы так то но лень
        }

        public void SetPointType(PointType type)
        {
            // надо бы так то но лень
        }

        public void SetAxisXSize(int size)
        {
            // Implementation needed for OxyPlot
        }

        public void SetAxisXPositionFromRight(int xPosition)
        {
            // Implementation needed for OxyPlot
        }

        public void SetWhiteScheme()
        {
            // кто нить другой сделает не интересно
        }

        public void ShowAreaOnChart()
        {
            foreach (var row in main_grid_chart.RowDefinitions.Where(x => (string)x.Tag != "Prime" && (string)x.Tag != "ScrollChart" && (string)x.Tag != "ControlPanel"))
            {
                string tag = (string)row.Tag;

                if (tag.Contains("GridSplitter_")) 
                    row.Height = new GridLength(2, GridUnitType.Pixel);
                else
                    row.Height = new GridLength(1, GridUnitType.Star);
            }
        }

        public void ShowContextMenu(System.Windows.Forms.ContextMenuStrip menu)
        {
            if (panel_winforms == null)
                return;

            menu.Show(panel_winforms, new System.Drawing.Point(0, 0));
        }

        public void StartPaintPrimeChart(System.Windows.Controls.Grid grid_chart, WindowsFormsHost host, System.Windows.Shapes.Rectangle rectangle)
        {
            if (isPaint == true)
                return;

            isPaint = true;

            this.host = host;

            host.Width = 0;

            panel_winforms = new System.Windows.Forms.Panel()
            {
                Width = 0,
            };

            this.host.Child = panel_winforms;

            MakeChart(grid_chart);
        }

        public void MakeChart(System.Windows.Controls.Grid grid_chart)
        {
            System.Windows.Media.BrushConverter converter = new System.Windows.Media.BrushConverter();

            main_grid_chart = new System.Windows.Controls.Grid();
            main_grid_chart = grid_chart;
            main_grid_chart.Margin = new System.Windows.Thickness(25, 0, 0, 0);

            main_grid_chart.RowDefinitions.Add(new RowDefinition()
            {
                Tag = "Prime",
                Height = new System.Windows.GridLength(4, System.Windows.GridUnitType.Star)
            });

            var main_chart = new CandleStickArea(new OxyAreaSettings()
            {
                cursor_X_is_active = true,
                cursor_Y_is_active = true,
                Tag = "Prime",
                brush_background = "#111721",
                AxislineStyle = LineStyle.Solid,
            }, all_areas, this);

            main_chart.chart_name = this.chart_name;
            main_chart.date_time_axis_X.MaximumMargin = 0;
            main_chart.date_time_axis_X.MinimumMargin = 0;
            main_chart.plot_view.Margin = new Thickness(0, main_chart.plot_view.Margin.Top, main_chart.plot_view.Margin.Right, main_chart.plot_view.Margin.Bottom);
            main_chart.plot_model.PlotMargins = new OxyThickness(0, main_chart.plot_model.PlotMargins.Top, main_chart.plot_model.PlotMargins.Right, main_chart.plot_model.PlotMargins.Bottom);
            main_chart.plot_model.Padding = new OxyThickness(0, main_chart.plot_model.Padding.Top, main_chart.plot_model.Padding.Right, main_chart.plot_model.Padding.Bottom);
            main_chart.time_frame = this.time_frame;
            main_chart.time_frame_span = this.time_frame_span;


            if (all_areas.Exists(x => (string)x.Tag == "Prime" && x.chart_name == this.chart_name))
            {
                OxyArea area_prime = all_areas.Find(x => (string)x.Tag == "Prime" && x.chart_name == this.chart_name);

                area_prime.Dispose();

                all_areas.Remove(all_areas.Find(x => (string)x.Tag == "Prime" && x.chart_name == this.chart_name));
            }

            System.Windows.Controls.Grid.SetRow(main_chart.GetViewUI(), 0);
            System.Windows.Controls.Grid.SetColumn(main_chart.GetViewUI(), 0);

            main_grid_chart.Children.Add(main_chart.GetViewUI());

            all_areas.Add(main_chart);
            mediator.AddOxyArea(main_chart);

            // Clear and rebuild indicator areas to ensure proper synchronization
            var existingIndicatorAreas = all_areas.Where(x => x is IndicatorArea).ToList();
            
            // Remove existing indicator areas from all_areas temporarily
            foreach (var area in existingIndicatorAreas)
            {
                all_areas.Remove(area);
            }

            // Re-add indicator areas to ensure proper initialization
            foreach (var area in existingIndicatorAreas)
            {
                // Ensure the area is properly initialized
                if (area is IndicatorArea indicatorArea)
                {
                    indicatorArea.chart_name = this.chart_name;
                    indicatorArea.time_frame = this.time_frame;
                    indicatorArea.time_frame_span = this.time_frame_span;
                }
                
                all_areas.Add(area);
                mediator.AddOxyArea(area);
            }

            var indi_areas = all_areas.Where(x => x is IndicatorArea);

            foreach (var area in indi_areas)
            {
                main_grid_chart.RowDefinitions.Add(new RowDefinition()
                {
                    Tag = "GridSplitter_" + area.Tag,
                    Height = new System.Windows.GridLength(3),
                });

                GridSplitter grid_splitter = new GridSplitter()
                {
                    ShowsPreview = false,
                    Tag = area.Tag,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Background = (System.Windows.Media.Brush)converter.ConvertFromString("#50BEFFD5"),
                };

                if (!splitters.Contains(grid_splitter))
                    splitters.Add(grid_splitter);

                System.Windows.Controls.Grid.SetColumn(grid_splitter, 0);
                System.Windows.Controls.Grid.SetRow(grid_splitter, main_grid_chart.RowDefinitions.Count - 1);

                main_grid_chart.Children.Add(grid_splitter);

                main_grid_chart.RowDefinitions.Add(new RowDefinition()
                {
                    Tag = area.Tag,
                    Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star),
                });

                System.Windows.Controls.Grid.SetRow(area.GetViewUI(), main_grid_chart.RowDefinitions.Count - 1);
                System.Windows.Controls.Grid.SetColumn(area.GetViewUI(), 0);

                main_grid_chart.Children.Add(area.GetViewUI());
            }

            if (all_areas.Exists(x => (string)x.Tag == "ScrollChart"))
            {
                var area_scroll = all_areas.Find(x => (string)x.Tag == "ScrollChart");

                ((ScrollBarArea)area_scroll).Dispose();

                all_areas.Remove(all_areas.Find(x => (string)x.Tag == "ScrollChart"));
            }

            var scroll_chart = new ScrollBarArea(new OxyAreaSettings()
            {            
                brush_background = "#282E38",
                brush_scroll_bacground = "#282E38",
                cursor_X_is_active = true,
                Tag = "ScrollChart",
            }, all_areas, this);

            scroll_chart.chart_name = this.chart_name;
            scroll_chart.date_time_axis_X.MaximumPadding = 0;
            scroll_chart.date_time_axis_X.MinimumPadding = 0;
            scroll_chart.plot_model.Padding = new OxyThickness(0, 0, 0, 0);
            scroll_chart.plot_model.PlotMargins = new OxyThickness(0, 0, 0, 0);
            scroll_chart.plot_model.PlotAreaBorderThickness = new OxyThickness(1, 1, 2, 2);
            scroll_chart.plot_model.PlotAreaBorderColor = OxyColor.Parse("#50BEFFD5");

            main_grid_chart.RowDefinitions.Add(new RowDefinition()
            {
                Tag = "ScrollChart",
                Height = new System.Windows.GridLength(40),
            });

            System.Windows.Controls.Grid.SetRow(scroll_chart.GetViewUI(), main_grid_chart.RowDefinitions.Count - 1);
            System.Windows.Controls.Grid.SetColumn(scroll_chart.GetViewUI(), 0);

            main_grid_chart.Children.Add(scroll_chart.GetViewUI());

            all_areas.Add(scroll_chart);
            mediator.AddOxyArea(scroll_chart);

            if (start_program != StartProgram.IsOsData)
            {
                var control_panel = new ControlPanelArea(new OxyAreaSettings()
                {                  
                    brush_background = "#111721",
                    Tag = "ControlPanel",
                }, all_areas, this);

                control_panel.chart_name = this.chart_name;
                control_panel.plot_view.Height = 50;

                if (all_areas.Exists(x => (string)x.Tag == "ControlPanel"))
                {
                    var area_control = all_areas.Find(x => (string)x.Tag == "ControlPanel");

                    ((ControlPanelArea)area_control).Dispose();

                    all_areas.Remove(all_areas.Find(x => (string)x.Tag == "ControlPanel"));
                }


                main_grid_chart.RowDefinitions.Add(new RowDefinition()
                {
                    Tag = "ControlPanel",
                    Height = new System.Windows.GridLength(40),
                });

            System.Windows.Controls.Grid.SetRow(control_panel.GetViewUI(), main_grid_chart.RowDefinitions.Count - 1);
            System.Windows.Controls.Grid.SetColumn(control_panel.GetViewUI(), 0);

            main_grid_chart.Children.Add(control_panel.GetViewUI());

                all_areas.Add(control_panel);
                mediator.AddOxyArea(control_panel);

                control_panel.plot_model.InvalidatePlot(true);
                control_panel.Calculate(time_frame_span, time_frame);
                control_panel.Redraw();                   
            }

            if (all_areas.Count > 3)
            {
                for (int i = 0; i < all_areas.Count; i++)
                {
                    if ((string)all_areas[i].Tag != "ScrollChart" && (string)all_areas[i].Tag != "Prime" && (string)all_areas[i].Tag != "ControlPanel")
                    {
                        var axes = all_areas[i].plot_model.Axes.ToList().Find(x => x.Key == "DateTime");

                        axes.TextColor = OxyColors.Transparent;
                        axes.TicklineColor = OxyColors.Transparent;
                        axes.AxisDistance = -50;
                        axes.MaximumPadding = 0;
                        axes.MinimumPadding = 0;

                        all_areas[i].plot_model.Padding = new OxyThickness(0, 0, all_areas[i].plot_model.Padding.Right, 0);
                        all_areas[i].plot_model.PlotMargins = new OxyThickness(0, 0, all_areas[i].plot_model.PlotMargins.Right, 0);
                        all_areas[i].plot_view.Padding = new System.Windows.Thickness(0, 0, all_areas[i].plot_view.Padding.Right, 0);
                        all_areas[i].plot_view.Margin = new System.Windows.Thickness(0, 0, all_areas[i].plot_view.Margin.Right, 0);
                    }
                    else
                    {
                        //цвет прозрачный + смещение вниз 
                    }
                }
            }

            // Force a refresh of all indicator areas to ensure they are properly initialized
            // Принудительно обновить все области индикаторов для обеспечения их правильной инициализации
            foreach (var area in all_areas.Where(x => x is IndicatorArea))
            {
                area.plot_model.InvalidatePlot(true);
            }

            // Ensure all indicator areas are properly synchronized with the mediator
            // Обеспечить правильную синхронизацию всех областей индикаторов с медиатором
            SynchronizeMediatorAreas();
        }

        public void StopPaint()
        {
            isPaint = false;
            mediator.is_first_start = true;

            // Clear the mediator's indicators list to ensure clean state
            // Очистить список индикаторов медиатора для обеспечения чистого состояния
            mediator.indicators_list.Clear();

            foreach (var area in all_areas)
            {
                area.Dispose();
            }

            if (main_grid_chart != null)
            {
                main_grid_chart.RowDefinitions.Clear();
                main_grid_chart.Children.Clear();
                main_grid_chart = null;
            }
        }

        /// <summary>
        /// Ensures that all indicator areas in all_areas are properly registered with the mediator
        /// Обеспечивает, что все области индикаторов в all_areas правильно зарегистрированы в медиаторе
        /// </summary>
        private void SynchronizeMediatorAreas()
        {
            if (mediator == null)
                return;

            // Get all indicator areas from all_areas
            // Получить все области индикаторов из all_areas
            var allIndicatorAreas = all_areas.Where(x => x is IndicatorArea).Cast<IndicatorArea>().ToList();
            
            // Get all areas currently in mediator
            // Получить все области, которые в настоящее время находятся в медиаторе
            var mediatorAreas = mediator.indicators_list.ToList();
            
            // Find areas that are in all_areas but not in mediator
            // Найти области, которые есть в all_areas, но нет в медиаторе
            var missingAreas = allIndicatorAreas.Where(area => !mediatorAreas.Contains(area)).ToList();
            
            // Add missing areas to mediator
            // Добавить недостающие области в медиатор
            foreach (var missingArea in missingAreas)
            {
                mediator.AddOxyArea(missingArea);
            }
            
            // Find areas that are in mediator but not in all_areas (shouldn't happen, but just in case)
            // Найти области, которые есть в медиаторе, но нет в all_areas (не должно происходить, но на всякий случай)
            var extraAreas = mediatorAreas.Where(area => !allIndicatorAreas.Contains(area)).ToList();
            
            // Remove extra areas from mediator
            // Удалить лишние области из медиатора
            foreach (var extraArea in extraAreas)
            {
                mediator.indicators_list.Remove(extraArea);
            }
        }

        /// <summary>
        /// Set current exchange, trading pair, and timeframe for robust indicator update detection
        /// Установить текущую биржу, торговую пару и таймфрейм для надежного обнаружения обновлений индикаторов
        /// </summary>
        /// <param name="exchange">Exchange name / Название биржи</param>
        /// <param name="tradingPair">Trading pair name / Название торговой пары</param>
        /// <param name="timeFrame">Timeframe / Таймфрейм</param>
        public void SetCurrentTradingContext(string exchange, string tradingPair, TimeFrame timeFrame)
        {
            bool isSignificantChange = false;

            // Check if any of the key values have changed
            // Проверить, изменились ли какие-либо ключевые значения
            if (_currentExchange != exchange || 
                _currentTradingPair != tradingPair || 
                _currentTimeFrame != timeFrame)
            {
                isSignificantChange = true;
            }

            // Update current values
            // Обновить текущие значения
            _currentExchange = exchange ?? "";
            _currentTradingPair = tradingPair ?? "";
            _currentTimeFrame = timeFrame;

            // If this is a significant change, clear all indicator data to force complete recalculation
            // Если это значительное изменение, очистить все данные индикаторов для принудительного полного пересчета
            if (isSignificantChange)
            {
                ClearAllIndicatorData();
            }
        }

        /// <summary>
        /// Clear all indicator data to force complete recalculation
        /// Очистить все данные индикаторов для принудительного полного пересчета
        /// </summary>
        private void ClearAllIndicatorData()
        {
            if (isPaint == false || can_draw == false)
                return;

            // Clear all indicator series data points
            // Очистить все точки данных серий индикаторов
            foreach (var seria in series)
            {
                if (seria.DataPoints != null)
                {
                    seria.DataPoints.Clear();
                }
                if (seria.IndicatorPoints != null)
                {
                    seria.IndicatorPoints.Clear();
                }
                if (seria.IndicatorHistogramPoints != null)
                {
                    seria.IndicatorHistogramPoints.Clear();
                }
                if (seria.IndicatorScatterPoints != null)
                {
                    seria.IndicatorScatterPoints.Clear();
                }
            }

            // Clear plot model series in all indicator areas
            // Очистить серии модели графика во всех областях индикаторов
            foreach (var area in all_areas)
            {
                if (area is IndicatorArea indicatorArea)
                {
                    if (indicatorArea.plot_model != null && indicatorArea.plot_model.Series != null)
                    {
                        indicatorArea.plot_model.Series.Clear();
                    }
                }
            }

            // Force redraw of all areas
            // Принудительно перерисовать все области
            mediator?.RedrawAll(null);
        }
    }
}
