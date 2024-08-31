using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TimeCodeBar.xaml
    /// </summary>
    public partial class TimeCodeBar : UserControl
    {
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(TimeCodeBar),
            new PropertyMetadata(0.0, (s, e) => ((TimeCodeBar)s).Refresh()));

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(TimeCodeBar),
            new PropertyMetadata(30.0, (s, e) => ((TimeCodeBar)s).Refresh()));

        private readonly List<TextBlock> _textBlocks = new List<TextBlock>();

        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public TimeCodeBar()
        {
            InitializeComponent();

            SizeChanged += TimeCodeBar_SizeChanged;
        }

        private void TimeCodeBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Refresh(e.NewSize);
        }

        private void Refresh()
        {
            Refresh(new Size(ActualWidth, ActualHeight));
        }

        private void Refresh(Size size)
        {
            if (double.IsNaN(size.Width))
                return;

            UpdateTimeBorder();
            UpdateTimeCodes(size);
        }

        private void UpdateTimeBorder()
        {
            var duration = Duration;
            var left = TimeToX(-0.5f);
            var right = TimeToX((duration - 1) + 0.5f);

            Canvas.SetLeft(durationBar, left);
            durationBar.Width = Math.Max(0, right - left);
        }

        private void UpdateTimeCodes(Size size)
        {
            var i = 0;
            while (i < 1024)
            {
                var textBlock = CreateTextBlock(i);
                if (Canvas.GetLeft(textBlock) + textBlock.Width >= size.Width)
                    break;

                i++;
            }
        }

        private TextBlock CreateTextBlock(int time)
        {
            while (time >= _textBlocks.Count)
            {
                _textBlocks.Add(null);
            }

            var textBlock = _textBlocks[time];
            if (textBlock == null)
            {
                textBlock = new TextBlock();
                textBlock.FontSize = 10;
                textBlock.Text = time.ToString();
                textBlock.TextAlignment = TextAlignment.Center;
                Canvas.SetTop(textBlock, 0);
                canvas.Children.Add(textBlock);
                _textBlocks[time] = textBlock;
            }

            Canvas.SetLeft(textBlock, TimeToX(time - 0.5f));
            textBlock.Width = Scale;
            textBlock.Height = canvas.ActualHeight;
            return textBlock;
        }

        private double TimeToX(double time) => Timeline.TimeToX(Scale, time);
    }
}
