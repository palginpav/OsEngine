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
            if (alertArray == null || alertArray.Count == 0)
                return;

            // Remove alert-related annotations and series from all areas
            // Удалить аннотации и серии, связанные с предупреждениями, из всех областей
            foreach (var area in all_areas)
            {
                if (area?.plot_model == null)
                    continue;

                try
                {
                    // Remove alert annotations
                    // Удалить аннотации предупреждений
                    var alertAnnotations = area.plot_model.Annotations?
                        .Where(a => a?.Tag != null && a.Tag.ToString().StartsWith("Alert_"))
                        .ToList();

                    if (alertAnnotations != null)
                    {
                        foreach (var annotation in alertAnnotations)
                        {
                            if (annotation != null)
                                area.plot_model.Annotations.Remove(annotation);
                        }
                    }

                    // Remove alert series
                    // Удалить серии предупреждений
                    var alertSeries = area.plot_model.Series?
                        .Where(s => s?.Title != null && s.Title.StartsWith("Alert_"))
                        .ToList();

                    if (alertSeries != null)
                    {
                        foreach (var series in alertSeries)
                        {
                            if (series != null)
                                area.plot_model.Series.Remove(series);
                        }
                    }

                    area.plot_model.InvalidatePlot(false);
                }
                catch (Exception ex)
                {
                    // Log error if logging is available
                    // Записать ошибку, если доступно журналирование
                    continue;
                }
            }
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
            // Move the chart view to the rightmost position
            // Переместить вид чарта в крайнее правое положение
            if (OxyArea.my_candles == null || OxyArea.my_candles.Count == 0)
                return;

            foreach (var area in all_areas.Where(x => x.Tag != (object)"ScrollChart" && x.Tag != (object)"ControlPanel"))
            {
                if (area?.date_time_axis_X == null)
                    continue;

                try
                {
                    // Calculate the rightmost position based on candle data
                    // Вычислить крайнее правое положение на основе данных свечей
                    double rightmostTime = DateTimeAxis.ToDouble(OxyArea.my_candles.Last().TimeStart);
                    double timeRange = scaleSize * 60; // Assuming minutes, adjust as needed
                    
                    // Set the axis range to show the rightmost data
                    // Установить диапазон оси для отображения крайних правых данных
                    area.date_time_axis_X.Zoom(rightmostTime - timeRange, rightmostTime + timeRange * 0.1);
                    
                    // Update Y axis to fit the visible data
                    // Обновить ось Y для соответствия видимым данным
                    if (area is CandleStickArea)
                    {
                        var minMax = area.GetHighLow(true, rightmostTime - timeRange, rightmostTime + timeRange * 0.1);
                        if (minMax?.Count >= 2)
                        {
                            area.linear_axis_Y?.Zoom(minMax[0], minMax[1]);
                        }
                    }
                    else
                    {
                        var minMax = area.GetHighLow(false, rightmostTime - timeRange, rightmostTime + timeRange * 0.1);
                        if (minMax?.Count >= 2)
                        {
                            area.linear_axis_Y?.Zoom(minMax[0], minMax[1]);
                        }
                    }
                    
                    area.plot_model?.InvalidatePlot(false);
                }
                catch (Exception ex)
                {
                    // Log error if logging is available
                    // Записать ошибку, если доступно журналирование
                    continue;
                }
            }
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
            // Create a tick data area for displaying tick-level information
            // Создать область данных тиков для отображения информации уровня тиков
            
            if (all_areas.Exists(x => (string)x.Tag == "TickArea"))
            {
                return; // Tick area already exists / Область тиков уже существует
            }

            try
            {
                var tick_area = new IndicatorArea(new OxyAreaSettings()
                {
                    cursor_X_is_active = true,
                    cursor_Y_is_active = true,
                    Tag = "TickArea",
                    AbsoluteMinimum = double.MinValue,
                    Y_Axies_is_visible = true,
                    X_Axies_is_visible = true,
                    brush_background = "#111721"
                }, all_areas, "TickArea", this);

                tick_area.indicator_name = "Ticks";
                tick_area.bot_tab = this.bot_tab;
                tick_area.bot_name = this.bot_name;

                // Configure the tick area appearance
                // Настроить внешний вид области тиков
                if (tick_area.plot_model?.Axes?.Count > 0)
                {
                    tick_area.plot_model.Axes[0].TextColor = OxyColors.White;
                    tick_area.plot_model.Axes[0].TicklineColor = OxyColors.Gray;
                }
                if (tick_area.plot_model?.Axes?.Count > 1)
                {
                    tick_area.plot_model.Axes[1].IntervalLength = 10;
                    tick_area.plot_model.Axes[1].MinorGridlineStyle = LineStyle.Dot;
                }

                all_areas.Add(tick_area);
                mediator?.AddOxyArea(tick_area);

                // Add the tick area to chart layout if chart is already built
                // Добавить область тиков в макет чарта, если чарт уже построен
                if (main_grid_chart != null)
                {
                    AddAreaToChartLayout(tick_area);
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
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
            // Remove the tick area from the chart
            // Удалить область тиков из чарта
            var tickArea = all_areas.Find(x => (string)x.Tag == "TickArea");
            if (tickArea != null)
            {
                try
                {
                    // Remove from mediator first
                    // Сначала удалить из медиатора
                    mediator?.RemoveOxyArea(tickArea);
                    
                    // Dispose the area
                    // Освободить область
                    tickArea.Dispose();
                    
                    // Remove from areas list
                    // Удалить из списка областей
                    all_areas.Remove(tickArea);
                    
                    // Remove from chart layout
                    // Удалить из макета чарта
                    RemoveAreaFromChartLayout(tickArea);
                    
                    // Remove associated splitter
                    // Удалить связанный разделитель
                    var splitter = splitters?.Find(x => (string)x.Tag == "TickArea");
                    if (splitter != null)
                    {
                        splitters.Remove(splitter);
                    }
                }
                catch (Exception ex)
                {
                    // Log error if logging is available
                    // Записать ошибку, если доступно журналирование
                }
            }
        }

        public List<string> GetAreasNames()
        {
            return all_areas.Where(x => (string)x.Tag != "ControlPanel" && (string)x.Tag != "ScrollChart").Select(x => (string)x.Tag).ToList();
        }

        public int GetCursorSelectCandleNumber()
        {
            // Get the candle number at the current cursor position
            // Получить номер свечи в текущей позиции курсора
            if (OxyArea.my_candles == null || OxyArea.my_candles.Count == 0)
                return -1;

            var primeArea = all_areas.FirstOrDefault(x => x is CandleStickArea);
            if (primeArea?.date_time_axis_X == null)
                return -1;

            try
            {
                // Get the current mouse position in data coordinates
                // Получить текущую позицию мыши в координатах данных
                var mouseDataPoint = primeArea.date_time_axis_X.InverseTransform(primeArea.mouse_screen_point.X);
                var mouseDateTime = DateTimeAxis.ToDateTime(mouseDataPoint);

                // Find the closest candle to the mouse position
                // Найти ближайшую свечу к позиции мыши
                int closestIndex = -1;
                TimeSpan minDifference = TimeSpan.MaxValue;

                for (int i = 0; i < OxyArea.my_candles.Count; i++)
                {
                    var difference = Math.Abs((OxyArea.my_candles[i].TimeStart - mouseDateTime).Ticks);
                    if (difference < minDifference.Ticks)
                    {
                        minDifference = new TimeSpan(difference);
                        closestIndex = i;
                    }
                }

                return closestIndex;
            }
            catch
            {
                return -1;
            }
        }

        public decimal GetCursorSelectPrice()
        {
            // Get the price at the current cursor position
            // Получить цену в текущей позиции курсора
            var primeArea = all_areas.FirstOrDefault(x => x is CandleStickArea);
            if (primeArea?.linear_axis_Y == null)
                return 0;

            try
            {
                // Get the current mouse position in data coordinates
                // Получить текущую позицию мыши в координатах данных
                var mousePricePoint = primeArea.linear_axis_Y.InverseTransform(primeArea.mouse_screen_point.Y);
                return (decimal)mousePricePoint;
            }
            catch
            {
                return 0;
            }
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
            if (alert == null)
                return;

            var targetArea = all_areas.FirstOrDefault(x => (string)x.Tag == "Prime");
            if (targetArea == null || targetArea.plot_model == null)
                return;

            try
            {
                // Create a line annotation for the alert
                // Создать линейную аннотацию для предупреждения
                var alertLine = new OxyPlot.Annotations.LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = alert.Lines != null && alert.Lines.Length > 0 && alert.Lines[0] != null ? (double)alert.Lines[0].LastPoint : 0,
                    Color = alert.ColorLine.A > 0 ? 
                        OxyColor.FromArgb(alert.ColorLine.A, alert.ColorLine.R, alert.ColorLine.G, alert.ColorLine.B) : 
                        OxyColors.Red,
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Solid,
                    Tag = $"Alert_{alert.GetHashCode()}",
                    Text = alert.Message ?? "Alert",
                    TextColor = OxyColors.White
                };

                targetArea.plot_model.Annotations.Add(alertLine);
                targetArea.plot_model.InvalidatePlot(false);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void RemoveAlert(AlertToChart alertToChart)
        {
            if (alertToChart == null)
                return;

            // Remove the alert from all areas
            // Удалить предупреждение из всех областей
            foreach (var area in all_areas)
            {
                if (area?.plot_model == null)
                    continue;

                try
                {
                    var alertTag = $"Alert_{alertToChart.GetHashCode()}";
                    
                    // Remove alert annotations
                    // Удалить аннотации предупреждений
                    var annotationsToRemove = area.plot_model.Annotations?
                        .Where(a => a?.Tag != null && a.Tag.ToString() == alertTag)
                        .ToList();

                    if (annotationsToRemove != null)
                    {
                        foreach (var annotation in annotationsToRemove)
                        {
                            if (annotation != null)
                                area.plot_model.Annotations.Remove(annotation);
                        }
                    }

                    // Remove alert series
                    // Удалить серии предупреждений
                    var seriesToRemove = area.plot_model.Series?
                        .Where(s => s?.Title != null && s.Title == alertTag)
                        .ToList();

                    if (seriesToRemove != null)
                    {
                        foreach (var series in seriesToRemove)
                        {
                            if (series != null)
                                area.plot_model.Series.Remove(series);
                        }
                    }

                    if ((annotationsToRemove?.Count ?? 0) > 0 || (seriesToRemove?.Count ?? 0) > 0)
                    {
                        area.plot_model.InvalidatePlot(false);
                    }
                }
                catch (Exception ex)
                {
                    // Log error if logging is available
                    // Записать ошибку, если доступно журналирование
                    continue;
                }
            }
        }

        public bool HaveAlertOnChart(AlertToChart alertToChart)
        {
            if (alertToChart == null)
                return false;

            var alertTag = $"Alert_{alertToChart.GetHashCode()}";

            // Check if alert exists in any area
            // Проверить, существует ли предупреждение в любой области
            foreach (var area in all_areas)
            {
                if (area?.plot_model == null)
                    continue;

                try
                {
                    // Check annotations
                    // Проверить аннотации
                    var hasAnnotation = area.plot_model.Annotations?
                        .Any(a => a?.Tag != null && a.Tag.ToString() == alertTag) ?? false;

                    if (hasAnnotation)
                        return true;

                    // Check series
                    // Проверить серии
                    var hasSeries = area.plot_model.Series?
                        .Any(s => s?.Title != null && s.Title == alertTag) ?? false;

                    if (hasSeries)
                        return true;
                }
                catch (Exception ex)
                {
                    // Log error if logging is available
                    // Записать ошибку, если доступно журналирование
                    continue;
                }
            }

            return false;
        }

        public void ProcessAlert(AlertToChart alert, bool needToWait)
        {
            if (alert == null || isPaint == false)
                return;

            if (needToWait)
            {
                // If we need to wait, process the alert asynchronously
                // Если нужно подождать, обработать предупреждение асинхронно
                Task.Run(() =>
                {
                    try
                    {
                        Thread.Sleep(50); // Small delay to ensure chart is ready
                        PaintAlert(alert);
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                    }
                });
            }
            else
            {
                // Process immediately
                // Обработать немедленно
                PaintAlert(alert);
            }
        }

        public void PaintHorisiontalLineOnArea(LineHorisontal lineElement)
        {
            if (lineElement == null)
                return;

            var targetArea = all_areas.FirstOrDefault(x => (string)x.Tag == lineElement.Area);
            if (targetArea?.plot_model == null)
                return;

            try
            {
                // Create horizontal line annotation
                // Создать аннотацию горизонтальной линии
                var horizontalLine = new OxyPlot.Annotations.LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = (double)lineElement.Value,
                    Color = OxyColor.FromArgb(lineElement.Color.A, lineElement.Color.R, lineElement.Color.G, lineElement.Color.B),
                    StrokeThickness = lineElement.LineWidth,
                    LineStyle = LineStyle.Solid,
                    Tag = $"HorizontalLine_{lineElement.GetHashCode()}"
                };

                targetArea.plot_model.Annotations.Add(horizontalLine);
                targetArea.plot_model.InvalidatePlot(false);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void PaintHorizontalLineOnArea(LineHorisontal lineElement)
        {
            PaintHorisiontalLineOnArea(lineElement);
        }

        public void PaintInDifColor(int indexStart, int indexEnd, string seriesName)
        {
            if (OxyArea.my_candles == null || OxyArea.my_candles.Count == 0)
                return;

            if (indexStart < 0 || indexEnd >= OxyArea.my_candles.Count || indexStart > indexEnd)
                return;

            var targetArea = all_areas.FirstOrDefault(x => x is CandleStickArea);
            if (targetArea?.plot_model == null)
                return;

            try
            {
                // Create a highlighted series for the specified range
                // Создать подсвеченную серию для указанного диапазона
                var highlightSeries = new CandleStickSeries
                {
                    Title = $"Highlight_{seriesName}_{indexStart}_{indexEnd}",
                    Color = OxyColors.Orange,
                    IncreasingColor = OxyColors.LightGreen,
                    DecreasingColor = OxyColors.LightCoral,
                    StrokeThickness = 2
                };

                // Add candles from the specified range
                // Добавить свечи из указанного диапазона
                for (int i = indexStart; i <= indexEnd; i++)
                {
                    var candle = OxyArea.my_candles[i];
                    highlightSeries.Items.Add(new HighLowItem(
                        DateTimeAxis.ToDouble(candle.TimeStart),
                        (double)candle.High,
                        (double)candle.Low,
                        (double)candle.Open,
                        (double)candle.Close
                    ));
                }

                // Remove any existing highlight series with the same name
                // Удалить любые существующие подсвеченные серии с тем же именем
                var existingSeries = targetArea.plot_model.Series?
                    .Where(s => s?.Title != null && s.Title.StartsWith($"Highlight_{seriesName}"))
                    .ToList();

                if (existingSeries != null)
                {
                    foreach (var series in existingSeries)
                    {
                        if (series != null)
                            targetArea.plot_model.Series.Remove(series);
                    }
                }

                targetArea.plot_model.Series.Add(highlightSeries);
                targetArea.plot_model.InvalidatePlot(false);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void PaintOneLine(System.Windows.Forms.DataVisualization.Charting.Series mySeries, List<Candle> candles, ChartAlertLine line, Color colorLine, int borderWidth, Color colorLabel, string label)
        {
            if (candles == null || candles.Count == 0 || line == null)
                return;

            var targetArea = all_areas.FirstOrDefault(x => x is CandleStickArea);
            if (targetArea?.plot_model == null)
                return;

            try
            {
                // Create a line series for the alert line
                // Создать линейную серию для линии предупреждения
                var lineSeries = new LineSeries
                {
                    Title = label ?? $"AlertLine_{line.GetHashCode()}",
                    Color = OxyColor.FromArgb(colorLine.A, colorLine.R, colorLine.G, colorLine.B),
                    StrokeThickness = borderWidth,
                    LineStyle = LineStyle.Solid
                };

                // Add line points based on the alert line properties
                // Добавить точки линии на основе свойств линии предупреждения
                // Create a line from first point to second point
                // Создать линию от первой точки ко второй точке
                lineSeries.Points.Add(new OxyPlot.DataPoint(
                    DateTimeAxis.ToDouble(line.TimeFirstPoint), 
                    (double)line.ValueFirstPoint
                ));
                lineSeries.Points.Add(new OxyPlot.DataPoint(
                    DateTimeAxis.ToDouble(line.TimeSecondPoint), 
                    (double)line.ValueSecondPoint
                ));

                // Remove existing line with same label
                // Удалить существующую линию с тем же лабелом
                var existingSeries = targetArea.plot_model.Series?
                    .Where(s => s?.Title == lineSeries.Title)
                    .ToList();

                if (existingSeries != null)
                {
                    foreach (var series in existingSeries)
                    {
                        if (series != null)
                            targetArea.plot_model.Series.Remove(series);
                    }
                }

                targetArea.plot_model.Series.Add(lineSeries);
                targetArea.plot_model.InvalidatePlot(false);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void PaintPoint(PointElement point)
        {
            if (point == null)
                return;

            var targetArea = all_areas.FirstOrDefault(x => (string)x.Tag == point.Area || x is CandleStickArea);
            if (targetArea?.plot_model == null)
                return;

            try
            {
                // Create a scatter series for the point
                // Создать серию точек для точки
                var scatterSeries = new ScatterSeries
                {
                    Title = $"Point_{point.GetHashCode()}",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = point.Size,
                    MarkerFill = OxyColor.FromArgb(point.Color.A, point.Color.R, point.Color.G, point.Color.B),
                    MarkerStroke = OxyColors.Black,
                    MarkerStrokeThickness = 1
                };

                // Add the point
                // Добавить точку
                scatterSeries.Points.Add(new ScatterPoint(
                    DateTimeAxis.ToDouble(point.TimePoint),
                    (double)point.Y
                ));

                // Remove existing point with same hash
                // Удалить существующую точку с тем же хэшем
                var existingSeries = targetArea.plot_model.Series?
                    .Where(s => s?.Title == scatterSeries.Title)
                    .ToList();

                if (existingSeries != null)
                {
                    foreach (var series in existingSeries)
                    {
                        if (series != null)
                            targetArea.plot_model.Series.Remove(series);
                    }
                }

                targetArea.plot_model.Series.Add(scatterSeries);
                targetArea.plot_model.InvalidatePlot(false);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void PaintSingleCandlePattern(List<Candle> candles)
        {
            if (candles == null || candles.Count == 0)
                return;

            var targetArea = all_areas.FirstOrDefault(x => x is CandleStickArea);
            if (targetArea?.plot_model == null)
                return;

            try
            {
                // Create a pattern series for highlighting specific candles
                // Создать серию паттернов для подсвечивания определенных свечей
                var patternSeries = new CandleStickSeries
                {
                    Title = "CandlePattern",
                    Color = OxyColors.Yellow,
                    IncreasingColor = OxyColors.LightYellow,
                    DecreasingColor = OxyColors.Gold,
                    StrokeThickness = 3
                };

                // Add pattern candles
                // Добавить свечи паттерна
                foreach (var candle in candles)
                {
                    patternSeries.Items.Add(new HighLowItem(
                        DateTimeAxis.ToDouble(candle.TimeStart),
                        (double)candle.High,
                        (double)candle.Low,
                        (double)candle.Open,
                        (double)candle.Close
                    ));
                }

                // Remove existing pattern series
                // Удалить существующие серии паттернов
                var existingSeries = targetArea.plot_model.Series?
                    .Where(s => s?.Title == "CandlePattern")
                    .ToList();

                if (existingSeries != null)
                {
                    foreach (var series in existingSeries)
                    {
                        if (series != null)
                            targetArea.plot_model.Series.Remove(series);
                    }
                }

                targetArea.plot_model.Series.Add(patternSeries);
                targetArea.plot_model.InvalidatePlot(false);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void PaintSingleVolumePattern(List<Candle> candles)
        {
            if (candles == null || candles.Count == 0)
                return;

            var targetArea = all_areas.FirstOrDefault(x => x is CandleStickArea);
            if (targetArea?.plot_model == null)
                return;

            try
            {
                // Create a volume pattern series using bar chart
                // Создать серию паттернов объема, используя гистограмму
                var volumePatternSeries = new LinearBarSeries
                {
                    Title = "VolumePattern",
                    FillColor = OxyColor.FromArgb(128, 255, 165, 0), // Semi-transparent orange
                    StrokeColor = OxyColors.Orange,
                    StrokeThickness = 1
                };

                // Add volume pattern bars
                // Добавить столбцы паттерна объема
                foreach (var candle in candles)
                {
                    volumePatternSeries.Points.Add(new OxyPlot.DataPoint(
                        DateTimeAxis.ToDouble(candle.TimeStart),
                        (double)candle.Volume
                    ));
                }

                // Remove existing volume pattern series
                // Удалить существующие серии паттернов объема
                var existingSeries = targetArea.plot_model.Series?
                    .Where(s => s?.Title == "VolumePattern")
                    .ToList();

                if (existingSeries != null)
                {
                    foreach (var series in existingSeries)
                    {
                        if (series != null)
                            targetArea.plot_model.Series.Remove(series);
                    }
                }

                targetArea.plot_model.Series.Add(volumePatternSeries);
                targetArea.plot_model.InvalidatePlot(false);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
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
            if (element == null)
                return;

            try
            {
                // Remove chart element from all areas
                // Удалить элемент чарта из всех областей
                foreach (var area in all_areas)
                {
                    if (area?.plot_model == null)
                        continue;

                    string elementTag = $"Element_{element.GetHashCode()}";
                    
                    // Remove annotations related to this element
                    // Удалить аннотации, связанные с этим элементом
                    var annotationsToRemove = area.plot_model.Annotations?
                        .Where(a => a?.Tag != null && a.Tag.ToString().Contains(elementTag))
                        .ToList();

                    if (annotationsToRemove != null)
                    {
                        foreach (var annotation in annotationsToRemove)
                        {
                            if (annotation != null)
                                area.plot_model.Annotations.Remove(annotation);
                        }
                    }

                    // Remove series related to this element
                    // Удалить серии, связанные с этим элементом
                    var seriesToRemove = area.plot_model.Series?
                        .Where(s => s?.Title != null && s.Title.Contains(elementTag))
                        .ToList();

                    if (seriesToRemove != null)
                    {
                        foreach (var series in seriesToRemove)
                        {
                            if (series != null)
                                area.plot_model.Series.Remove(series);
                        }
                    }

                    if ((annotationsToRemove?.Count ?? 0) > 0 || (seriesToRemove?.Count ?? 0) > 0)
                    {
                        area.plot_model.InvalidatePlot(false);
                    }
                }

                // Note: mediator.ClearElement method doesn't exist, so we skip this
                // Примечание: метод mediator.ClearElement не существует, поэтому пропускаем это
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
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
            // Refresh chart colors for all areas based on current color scheme
            // Обновить цвета чарта для всех областей на основе текущей цветовой схемы
            if (color_keeper == null || all_areas == null)
                return;

            try
            {
                foreach (var area in all_areas)
                {
                    if (area?.plot_model == null)
                        continue;

                    try
                    {
                        // Update background color
                        // Обновить цвет фона
                        area.plot_model.Background = OxyColor.Parse(area.area_settings?.brush_background ?? "#111721");
                        
                        // Update axes colors without aggressive formatting
                        // Обновить цвета осей без агрессивного форматирования
                        foreach (var axis in area.plot_model.Axes)
                        {
                            // Clear any duplicate labels or formatting
                            // Очистить любые дублирующиеся метки или форматирование
                            axis.LabelFormatter = null;
                            
                            if (axis is DateTimeAxis dateAxis)
                            {
                                dateAxis.TextColor = OxyColors.White;
                                dateAxis.TicklineColor = OxyColors.Gray;
                                dateAxis.MajorGridlineColor = OxyColor.FromArgb(50, 255, 255, 255);
                                dateAxis.MinorGridlineColor = OxyColor.FromArgb(25, 255, 255, 255);
                                
                                // Prevent X-axis duplication
                                // Предотвратить дублирование оси X
                                dateAxis.StringFormat = "HH:mm"; // Simple time format
                                dateAxis.MajorStep = 1.0 / 24.0; // 1 hour steps
                                dateAxis.MinorStep = 1.0 / 96.0; // 15 minute steps
                            }
                            else if (axis is LinearAxis linearAxis)
                            {
                                linearAxis.TextColor = OxyColors.White;
                                linearAxis.TicklineColor = OxyColors.Gray;
                                linearAxis.MajorGridlineColor = OxyColor.FromArgb(50, 255, 255, 255);
                                linearAxis.MinorGridlineColor = OxyColor.FromArgb(25, 255, 255, 255);
                                
                                // Prevent Y-axis duplication
                                // Предотвратить дублирование оси Y
                                linearAxis.MajorStep = double.NaN; // Auto-calculate steps
                                linearAxis.MinorStep = double.NaN;
                            }
                        }

                        // Clear any duplicate annotations or labels
                        // Очистить любые дублирующиеся аннотации или метки
                        ClearDuplicateLabels(area);

                        // Refresh plot
                        // Обновить график
                        area.plot_model.InvalidatePlot(true);
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        /// <summary>
        /// Clear duplicate labels and annotations to prevent X-axis duplication
        /// Очистить дублирующиеся метки и аннотации для предотвращения дублирования оси X
        /// </summary>
        private void ClearDuplicateLabels(OxyArea area)
        {
            if (area?.plot_model == null)
                return;

            try
            {
                // Only remove annotations that are explicitly marked as duplicates
                // Удалять только аннотации, явно помеченные как дублирующиеся
                var duplicateAnnotations = area.plot_model.Annotations?
                    .Where(a => a?.Tag != null && a.Tag.ToString().Contains("Duplicate"))
                    .ToList();

                if (duplicateAnnotations != null)
                {
                    foreach (var annotation in duplicateAnnotations)
                    {
                        if (annotation != null)
                            area.plot_model.Annotations.Remove(annotation);
                    }
                }

                // Ensure axes have proper formatting without aggressive step settings
                // Обеспечить правильное форматирование осей без агрессивных настроек шагов
                foreach (var axis in area.plot_model.Axes)
                {
                    if (axis is DateTimeAxis dateAxis)
                    {
                        // Use auto-calculated steps to prevent overlap
                        // Использовать автоматически рассчитанные шаги для предотвращения перекрытия
                        dateAxis.MajorStep = double.NaN;
                        dateAxis.MinorStep = double.NaN;
                        dateAxis.LabelFormatter = null;
                    }
                    else if (axis is LinearAxis linearAxis)
                    {
                        // Use auto-calculated steps
                        // Использовать автоматически рассчитанные шаги
                        linearAxis.MajorStep = double.NaN;
                        linearAxis.MinorStep = double.NaN;
                        linearAxis.LabelFormatter = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void RemoveCursor()
        {
            // Remove cursor elements from all areas
            // Удалить элементы курсора из всех областей
            try
            {
                foreach (var area in all_areas)
                {
                    if (area?.plot_model == null)
                        continue;

                    // Remove cursor annotations
                    // Удалить аннотации курсора
                    var cursorAnnotations = area.plot_model.Annotations?
                        .Where(a => a?.Tag != null && (a.Tag.ToString().Contains("Cursor") || a == area.cursor_X || a == area.cursor_Y))
                        .ToList();

                    if (cursorAnnotations != null)
                    {
                        foreach (var annotation in cursorAnnotations)
                        {
                            if (annotation != null)
                                area.plot_model.Annotations.Remove(annotation);
                        }
                    }

                    // Clear cursor references
                    // Очистить ссылки на курсор
                    area.cursor_X = null;
                    area.cursor_Y = null;

                    area.plot_model.InvalidatePlot(false);
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
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
            // Set dark/black color scheme for all chart areas
            // Установить темную/черную цветовую схему для всех областей чарта
            try
            {
                foreach (var area in all_areas)
                {
                    if (area?.plot_model == null)
                        continue;

                    try
                    {
                        // Set dark background
                        // Установить темный фон
                        area.plot_model.Background = OxyColor.Parse("#111721");
                        area.plot_model.PlotAreaBackground = OxyColor.Parse("#111721");
                        
                        // Set dark theme for axes
                        // Установить темную тему для осей
                        foreach (var axis in area.plot_model.Axes)
                        {
                            // Clear any duplicate labels or formatting
                            // Очистить любые дублирующиеся метки или форматирование
                            axis.LabelFormatter = null;
                            
                            axis.TextColor = OxyColors.White;
                            axis.TicklineColor = OxyColor.Parse("#404040");
                            axis.MajorGridlineColor = OxyColor.FromArgb(50, 255, 255, 255);
                            axis.MinorGridlineColor = OxyColor.FromArgb(25, 255, 255, 255);
                            axis.AxislineColor = OxyColor.Parse("#404040");
                        }

                        // Update candle series colors for dark theme
                        // Обновить цвета серий свечей для темной темы
                        foreach (var series in area.plot_model.Series)
                        {
                            if (series is CandleStickSeries candleSeries)
                            {
                                candleSeries.IncreasingColor = OxyColors.LimeGreen;
                                candleSeries.DecreasingColor = OxyColors.Red;
                                candleSeries.Color = OxyColors.White;
                            }
                            else if (series is LineSeries lineSeries)
                            {
                                // Keep existing colors or set default white
                                // Сохранить существующие цвета или установить белый по умолчанию
                                if (lineSeries.Color == OxyColors.Undefined)
                                    lineSeries.Color = OxyColors.White;
                            }
                        }

                        area.plot_model.InvalidatePlot(true);
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
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
            // Set the size of position/trade points on the chart
            // Установить размер точек позиций/сделок на чарте
            try
            {
                int markerSize = 4; // Default size / Размер по умолчанию
                
                switch (pointSize)
                {
                    case ChartPositionTradeSize.Size4:
                        markerSize = 3;
                        break;
                    case ChartPositionTradeSize.Size3:
                        markerSize = 4;
                        break;
                    case ChartPositionTradeSize.Size2:
                        markerSize = 6;
                        break;
                    case ChartPositionTradeSize.Size1:
                        markerSize = 8;
                        break;
                }

                // Update all scatter series (used for trade points)
                // Обновить все серии точек (используемые для точек сделок)
                foreach (var area in all_areas)
                {
                    if (area?.plot_model == null)
                        continue;

                    try
                    {
                        foreach (var series in area.plot_model.Series)
                        {
                            if (series is ScatterSeries scatterSeries)
                            {
                                scatterSeries.MarkerSize = markerSize;
                            }
                        }

                        area.plot_model.InvalidatePlot(false);
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void SetPointType(PointType type)
        {
            // Set the type/shape of position/trade points on the chart
            // Установить тип/форму точек позиций/сделок на чарте
            try
            {
                MarkerType markerType = MarkerType.Circle; // Default type / Тип по умолчанию
                
                switch (type)
                {
                    case PointType.Circle:
                        markerType = MarkerType.Circle;
                        break;
                    case PointType.Cross:
                        markerType = MarkerType.Cross;
                        break;
                    case PointType.TriAngle:
                        markerType = MarkerType.Triangle;
                        break;
                    case PointType.Romb:
                        markerType = MarkerType.Diamond;
                        break;
                    case PointType.Auto:
                        markerType = MarkerType.Circle;
                        break;
                }

                // Update all scatter series (used for trade points)
                // Обновить все серии точек (используемые для точек сделок)
                foreach (var area in all_areas)
                {
                    if (area?.plot_model == null)
                        continue;

                    try
                    {
                        foreach (var series in area.plot_model.Series)
                        {
                            if (series is ScatterSeries scatterSeries)
                            {
                                scatterSeries.MarkerType = markerType;
                            }
                        }

                        area.plot_model.InvalidatePlot(false);
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void SetAxisXSize(int size)
        {
            // Set the number of visible candles/data points on the X-axis
            // Установить количество видимых свечей/точек данных на оси X
            if (OxyArea.my_candles == null || OxyArea.my_candles.Count == 0 || size <= 0)
                return;

            try
            {
                foreach (var area in all_areas.Where(x => x.Tag != (object)"ScrollChart" && x.Tag != (object)"ControlPanel"))
                {
                    if (area?.date_time_axis_X == null)
                        continue;

                    try
                    {
                        // Calculate the time range based on the requested size
                        // Вычислить диапазон времени на основе запрошенного размера
                        int startIndex = Math.Max(0, OxyArea.my_candles.Count - size);
                        int endIndex = OxyArea.my_candles.Count - 1;
                        
                        if (startIndex < endIndex)
                        {
                            double startTime = DateTimeAxis.ToDouble(OxyArea.my_candles[startIndex].TimeStart);
                            double endTime = DateTimeAxis.ToDouble(OxyArea.my_candles[endIndex].TimeStart);
                            
                            // Add some padding for better visualization
                            // Добавить некоторое заполнение для лучшей визуализации
                            double timeRange = endTime - startTime;
                            double padding = timeRange * 0.02; // 2% padding
                            
                            area.date_time_axis_X.Zoom(startTime - padding, endTime + padding);
                            
                            // Update Y axis to fit the visible data
                            // Обновить ось Y для соответствия видимым данным
                            if (area is CandleStickArea)
                            {
                                var minMax = area.GetHighLow(true, startTime - padding, endTime + padding);
                                if (minMax?.Count >= 2)
                                {
                                    area.linear_axis_Y?.Zoom(minMax[0], minMax[1]);
                                }
                            }
                            else
                            {
                                var minMax = area.GetHighLow(false, startTime - padding, endTime + padding);
                                if (minMax?.Count >= 2)
                                {
                                    area.linear_axis_Y?.Zoom(minMax[0], minMax[1]);
                                }
                            }
                            
                            area.plot_model?.InvalidatePlot(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                        continue;
                    }
                }
                
                // Trigger size change event
                // Запустить событие изменения размера
                SizeAxisXChangeEvent?.Invoke(size);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void SetAxisXPositionFromRight(int xPosition)
        {
            // Set the X-axis position relative to the right edge of the chart
            // Установить позицию оси X относительно правого края чарта
            if (OxyArea.my_candles == null || OxyArea.my_candles.Count == 0 || xPosition < 0)
                return;

            try
            {
                foreach (var area in all_areas.Where(x => x.Tag != (object)"ScrollChart" && x.Tag != (object)"ControlPanel"))
                {
                    if (area?.date_time_axis_X == null)
                        continue;

                    try
                    {
                        // Calculate the position from the right edge
                        // Вычислить позицию от правого края
                        int endIndex = OxyArea.my_candles.Count - 1;
                        int startIndex = Math.Max(0, endIndex - xPosition);
                        
                        if (startIndex < endIndex)
                        {
                            double startTime = DateTimeAxis.ToDouble(OxyArea.my_candles[startIndex].TimeStart);
                            double endTime = DateTimeAxis.ToDouble(OxyArea.my_candles[endIndex].TimeStart);
                            
                            // Set the view to show data from the calculated position
                            // Установить вид для отображения данных с вычисленной позиции
                            double timeRange = endTime - startTime;
                            double padding = timeRange * 0.05; // 5% padding
                            
                            area.date_time_axis_X.Zoom(startTime - padding, endTime + padding);
                            
                            // Update Y axis to fit the visible data
                            // Обновить ось Y для соответствия видимым данным
                            if (area is CandleStickArea)
                            {
                                var minMax = area.GetHighLow(true, startTime - padding, endTime + padding);
                                if (minMax?.Count >= 2)
                                {
                                    area.linear_axis_Y?.Zoom(minMax[0], minMax[1]);
                                }
                            }
                            else
                            {
                                var minMax = area.GetHighLow(false, startTime - padding, endTime + padding);
                                if (minMax?.Count >= 2)
                                {
                                    area.linear_axis_Y?.Zoom(minMax[0], minMax[1]);
                                }
                            }
                            
                            area.plot_model?.InvalidatePlot(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                        continue;
                    }
                }
                
                // Trigger position change event
                // Запустить событие изменения позиции
                LastXIndexChangeEvent?.Invoke(xPosition);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
        }

        public void SetWhiteScheme()
        {
            // Set light/white color scheme for all chart areas with improved contrast
            // Установить светлую/белую цветовую схему для всех областей чарта с улучшенным контрастом
            try
            {
                foreach (var area in all_areas)
                {
                    if (area?.plot_model == null)
                        continue;

                    try
                    {
                        // Set light background
                        // Установить светлый фон
                        area.plot_model.Background = OxyColors.White;
                        area.plot_model.PlotAreaBackground = OxyColors.White;
                        
                        // Set light theme for axes with better contrast but no aggressive formatting
                        // Установить светлую тему для осей с лучшим контрастом, но без агрессивного форматирования
                        foreach (var axis in area.plot_model.Axes)
                        {
                            axis.TextColor = OxyColors.Black;
                            axis.TicklineColor = OxyColor.Parse("#808080"); // Darker gray for better contrast
                            axis.MajorGridlineColor = OxyColor.FromArgb(80, 0, 0, 0); // Stronger grid lines
                            axis.MinorGridlineColor = OxyColor.FromArgb(40, 0, 0, 0);
                            axis.AxislineColor = OxyColor.Parse("#404040"); // Darker axis lines
                            
                            // Use auto-calculated steps to prevent overlap
                            // Использовать автоматически рассчитанные шаги для предотвращения перекрытия
                            if (axis is DateTimeAxis dateAxis)
                            {
                                dateAxis.MajorStep = double.NaN;
                                dateAxis.MinorStep = double.NaN;
                                dateAxis.LabelFormatter = null;
                            }
                            else if (axis is LinearAxis linearAxis)
                            {
                                linearAxis.MajorStep = double.NaN;
                                linearAxis.MinorStep = double.NaN;
                                linearAxis.LabelFormatter = null;
                            }
                        }

                        // Update candle series colors for light theme with better contrast
                        // Обновить цвета серий свечей для светлой темы с лучшим контрастом
                        foreach (var series in area.plot_model.Series)
                        {
                            if (series is CandleStickSeries candleSeries)
                            {
                                candleSeries.IncreasingColor = OxyColor.Parse("#006400"); // Dark green
                                candleSeries.DecreasingColor = OxyColor.Parse("#8B0000"); // Dark red
                                candleSeries.Color = OxyColors.Black;
                            }
                            else if (series is LineSeries lineSeries)
                            {
                                // Keep existing colors or set default black
                                // Сохранить существующие цвета или установить черный по умолчанию
                                if (lineSeries.Color == OxyColors.Undefined)
                                    lineSeries.Color = OxyColors.Black;
                            }
                            else if (series is ScatterSeries scatterSeries)
                            {
                                // Ensure scatter points have good contrast
                                // Обеспечить хороший контраст для точечных элементов
                                if (scatterSeries.MarkerFill == OxyColors.Undefined)
                                    scatterSeries.MarkerFill = OxyColors.Black;
                                if (scatterSeries.MarkerStroke == OxyColors.Undefined)
                                    scatterSeries.MarkerStroke = OxyColors.Black;
                            }
                        }

                        // Clear any duplicate annotations or labels
                        // Очистить любые дублирующиеся аннотации или метки
                        var duplicateAnnotations = area.plot_model.Annotations?
                            .Where(a => a?.Tag != null && a.Tag.ToString().Contains("Duplicate"))
                            .ToList();

                        if (duplicateAnnotations != null)
                        {
                            foreach (var annotation in duplicateAnnotations)
                            {
                                if (annotation != null)
                                    area.plot_model.Annotations.Remove(annotation);
                            }
                        }

                        area.plot_model.InvalidatePlot(true);
                    }
                    catch (Exception ex)
                    {
                        // Log error if logging is available
                        // Записать ошибку, если доступно журналирование
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                // Записать ошибку, если доступно журналирование
            }
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

