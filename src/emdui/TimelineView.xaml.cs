using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TimelineView.xaml
    /// </summary>
    public partial class TimelineView : UserControl
    {
        private Button _playButton;
        private Button _stopButton;
        private Line _currentTimeVerticalLine;

        private IAnimationController _controller;
        private int _duration;

        public float Scale { get; } = 30;

        public IAnimationController Controller
        {
            get => _controller;
            set
            {
                if (_controller != value)
                {
                    if (_controller != null)
                    {
                        _controller.StateChanged -= OnControllerStateChanged;
                    }
                    _controller = value;
                    _controller.StateChanged += OnControllerStateChanged;
                    OnControllerStateChanged(this, EventArgs.Empty);
                }
            }
        }

        public TimelineView()
        {
            InitializeComponent();
            InitializeToolbar();
        }

        private void InitializeToolbar()
        {
            AddToolbarButton("IconContentDuplicate", "Insert keyframe", () => _controller.Insert());
            AddToolbarButton("IconContentDuplicate", "Duplicate current keyframe", () => _controller.Duplicate());
            AddToolbarButton("IconDelete", "Delete current keyframe", () => _controller.Delete());
            AddToolbarSeparator();
            AddToolbarButton("IconSkipBackward", "Seek to first key frame", () => _controller.KeyFrame = 0);
            AddToolbarButton("IconStepBackward", "Seek to previous key frame", () => _controller.KeyFrame--);
            _playButton = AddToolbarButton("IconPlay", "Play animation", () => _controller.Playing = true);
            _stopButton = AddToolbarButton("IconStop", "Stop animation", () => _controller.Playing = false);
            AddToolbarButton("IconStepForward", "Seek to next key frame", () => _controller.KeyFrame++);
            AddToolbarButton("IconSkipNext", "Seek to last key frame", () => _controller.KeyFrame = int.MaxValue);
        }

        private void AddToolbarSeparator()
        {
            toolbar.Items.Add(new Separator());
        }

        private Button AddToolbarButton(string image, string tooltip, Action action)
        {
            var button = new Button()
            {
                ToolTip = tooltip,
                Content = new Image()
                {
                    Width = 16,
                    Height = 16,
                    Source = (ImageSource)Application.Current.Resources[image]
                }
            };
            button.Click += (s, e) => action();
            toolbar.Items.Add(button);
            return button;
        }

        private void OnControllerStateChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_duration != _controller.Duration)
                {
                    _duration = _controller.Duration;
                    Refresh();
                }
                RefreshTime();

                if (_controller.Playing)
                {
                    _playButton.Visibility = Visibility.Collapsed;
                    _stopButton.Visibility = Visibility.Visible;
                }
                else
                {
                    _playButton.Visibility = Visibility.Visible;
                    _stopButton.Visibility = Visibility.Collapsed;
                }
            }, DispatcherPriority.Render);
        }

        private void Refresh()
        {
            var canvas = this.canvas;
            canvas.Children.Clear();

            var keyFrames = 100;
            for (var i = 0; i < keyFrames; i++)
            {
                CreateKeyFrameVerticalLine(i);
            }

            CreateTimeBorder();
            for (var i = 0; i < keyFrames; i++)
            {
                CreateTextBlock(i);
            }

            CreateCurrentTimeVerticalLine();
        }

        private void RefreshTime()
        {
            var line = _currentTimeVerticalLine;
            if (line == null)
                return;

            line.X1 = _controller.Time * Scale;
            line.X2 = line.X1;
        }

        private void CreateCurrentTimeVerticalLine()
        {
            var line = new Line();
            line.Stroke = Brushes.Black;
            line.Y1 = 0;
            line.Y2 = 1000;
            canvas.Children.Add(line);
            _currentTimeVerticalLine = line;
        }

        private void CreateTimeBorder()
        {
            var duration = _controller.Duration;
            var border = new Rectangle();
            border.Stroke = Brushes.Black;
            border.Fill = Brushes.Gray;
            border.Width = (duration - 1) * Scale;
            border.Height = 18;
            canvas.Children.Add(border);
        }

        private void CreateTextBlock(int time)
        {
            var textBlock = new TextBlock();
            textBlock.Text = time.ToString();
            textBlock.Width = Scale;
            textBlock.TextAlignment = TextAlignment.Center;
            Canvas.SetLeft(textBlock, (time - 0.5f) * Scale);
            Canvas.SetTop(textBlock, 0);
            canvas.Children.Add(textBlock);
        }

        private void CreateKeyFrameVerticalLine(int time)
        {
            var line = new Line();
            line.Stroke = Brushes.LightGray;
            line.X1 = time * Scale;
            line.Y1 = 0;
            line.X2 = line.X1;
            line.Y2 = 1000;
            canvas.Children.Add(line);
        }

        private void canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            canvas_PreviewMouseMove(sender, e);
            Focus();
        }

        private void canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(canvas);
                var keyFrame = (int)Math.Round(pos.X / Scale);
                _controller.KeyFrame = keyFrame;
                _controller.Playing = false;
            }
            e.Handled = true;
        }

        private void canvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    _controller.KeyFrame--;
                    e.Handled = true;
                    break;
                case Key.Right:
                    _controller.KeyFrame++;
                    e.Handled = true;
                    break;
                case Key.Space:
                    _controller.Playing = !_controller.Playing;
                    e.Handled = true;
                    break;
            }
        }
    }
}
