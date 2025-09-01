using CustomAnnotations;
using OsEngine.Entity;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OsEngine.Charts.CandleChart.OxyAreas
{
    public class ControlPanelArea : OxyArea
    {
        public CustomTextAnnotation speed_lable;
        public CustomTextAnnotation minus_button;
        public CustomTextAnnotation plus_button;
        public CustomTextAnnotation speed_state_lable;

        public ControlPanelArea(OxyAreaSettings settings, List<OxyArea> all_areas, OxyChartPainter owner) : base(settings, owner)
        {
            // Don't display control panel if StartProgram.IsOsTrader
            // Не отображать панель управления, если StartProgram.IsOsTrader
            if (owner.start_program == StartProgram.IsOsTrader)
            {
                return;
            }

            area_settings = settings;
            this.all_areas = all_areas;

            if (owner.start_program == StartProgram.IsTester)
            {
                speed_lable = new CustomTextAnnotation()
                {
                    Background = OxyColors.Transparent,
                    Stroke = OxyColors.Transparent,
                    StrokeThickness = 0,
                    Text = "Speed: ",
                    TextPosition = new ScreenPoint(20, 12),
                    TextVerticalAlignment = VerticalAlignment.Bottom,
                    TextHorizontalAlignment = HorizontalAlignment.Left,
                    Layer = AnnotationLayer.AboveSeries,

                };

                minus_button = new CustomTextAnnotation()
                {
                    Background = OxyColors.Transparent,
                    Stroke = OxyColor.Parse("#FF5500"),
                    StrokeThickness = 1,
                    Text = "-",
                    TextPosition = new ScreenPoint(65, 12),
                    TextVerticalAlignment = VerticalAlignment.Bottom,
                    TextHorizontalAlignment = HorizontalAlignment.Left,
                    Layer = AnnotationLayer.AboveSeries,
                    Padding = new OxyThickness(4.5, -1, 4.5, 1),
                };

                plus_button = new CustomTextAnnotation()
                {
                    Background = OxyColors.Transparent,
                    Stroke = OxyColor.Parse("#FF5500"),
                    StrokeThickness = 1,
                    Text = "+",
                    TextPosition = new ScreenPoint(80, 12),
                    TextVerticalAlignment = VerticalAlignment.Bottom,
                    TextHorizontalAlignment = HorizontalAlignment.Left,
                    Layer = AnnotationLayer.AboveSeries,
                    Padding = new OxyThickness(3, -1, 2.5, 1),
                };

                speed_state_lable = new CustomTextAnnotation()
                {
                    Background = OxyColors.Transparent,
                    Stroke = OxyColors.Transparent,
                    StrokeThickness = 0,
                    Text = "x1",
                    TextPosition = new ScreenPoint(100, 12),
                    TextVerticalAlignment = VerticalAlignment.Bottom,
                    TextHorizontalAlignment = HorizontalAlignment.Left,
                    Layer = AnnotationLayer.AboveSeries,

                };
            }

            if (owner.start_program == StartProgram.IsTester)
            {
                plot_model.Annotations.Add(speed_lable);
                plot_model.Annotations.Add(minus_button);
                plot_model.Annotations.Add(plus_button);
                plot_model.Annotations.Add(speed_state_lable);

                minus_button.MouseDown += Minus_button_MouseDown;
                minus_button.MouseUp += Minus_button_MouseUp;

                plus_button.MouseDown += Plus_button_MouseDown;
                plus_button.MouseUp += Plus_button_MouseUp;
            }

            plot_model.Updated += Plot_model_Updated;
            plot_model.MouseDown += Plot_model_MouseDown;
        }

        private void Plot_model_MouseDown(object sender, OxyMouseDownEventArgs e)
        {
            owner.mediator.RedrawControlPanel( false);

            e.Handled = true;
        }

        private void Plot_model_Updated(object sender, EventArgs e)
        {
            UpdateButtonsPos();
        }

        private void Plus_button_MouseUp(object sender, OxyMouseEventArgs e)
        {
            if (sender != null)
            {
                plus_button.Background = OxyColors.Transparent;
            }

            owner.mediator.RedrawControlPanel(false);
            e.Handled = true;
        }

        private void Minus_button_MouseUp(object sender, OxyMouseEventArgs e)
        {
            if (sender != null)
            {
                minus_button.Background = OxyColors.Transparent;
            }

            owner.mediator.RedrawControlPanel(false);
            e.Handled = true;
        }

        private void Plus_button_MouseDown(object sender, OxyMouseDownEventArgs e)
        {
            if (sender != null)
            {
                plus_button.Background = OxyColor.Parse("#FF5500");

                var main_chart = (CandleStickArea)all_areas.Find(x => x.Tag == (object)"Prime");

                if (owner.mediator.factor_selector < 16)
                    owner.mediator.factor_selector += 1;

                owner.mediator.factor = owner.mediator.factor_array[owner.mediator.factor_selector];

                speed_state_lable.Text = "x" + owner.mediator.factor_array[owner.mediator.factor_selector].ToString();
            }
            owner.mediator.RedrawControlPanel(false);
            e.Handled = true;
        }

        private void Minus_button_MouseDown(object sender, OxyMouseDownEventArgs e)
        {
            if (sender != null)
            {
                minus_button.Background = OxyColor.Parse("#FF5500");

                var main_chart = (CandleStickArea)all_areas.Find(x => x.Tag == (object)"Prime");

                if (owner.mediator.factor_selector > 0)
                    owner.mediator.factor_selector -= 1;

                owner.mediator.factor = owner.mediator.factor_array[owner.mediator.factor_selector];

                speed_state_lable.Text = "x" + owner.mediator.factor_array[owner.mediator.factor_selector].ToString();
            }
            owner.mediator.RedrawControlPanel( false);
            e.Handled = true;
        }

        public void UpdateButtonsPos()
        {

            if (owner.start_program == StartProgram.IsTester)
            {
                if (speed_lable == null || minus_button == null || plus_button == null || speed_state_lable == null)
                    return;

                speed_lable.TextPosition = new ScreenPoint(20, plot_view.ActualHeight - 20);
                minus_button.TextPosition = new ScreenPoint(65, plot_view.ActualHeight - 20);
                plus_button.TextPosition = new ScreenPoint(80, plot_view.ActualHeight - 20);
                speed_state_lable.TextPosition = new ScreenPoint(100, plot_view.ActualHeight - 20);
            }
        }

        public override void Calculate(TimeSpan time_frame_span, TimeFrame time_frame)
        {
            for (int i = 0; i < actions_to_calculate.Count; i++)
            {
                Action action = new Action(() => { });

                bool result = actions_to_calculate.TryDequeue(out action);

                if (result)
                    plot_view.Dispatcher.Invoke(action);
            }

            UpdateButtonsPos();
        }

        public override void Redraw()
        {
            Action action = () =>
            {
                plot_view.InvalidatePlot(true);
            };

            plot_view.Dispatcher.Invoke(action);
        }

        public override void Dispose()
        {
            my_candles = new List<Candle>();
            items_oxy_candles = new List<HighLowItem>();
        }
    }
}
