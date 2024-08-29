using System;
using System.Linq;
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
        private Rectangle _lastEntityPoint;
        private Rectangle _pickupEntityPoint;
        private double _pickupOffset;

        private IAnimationController _controller;
        private int _duration;
        private int? _selectedEntity;

        public float Scale { get; set; } = 30;

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
            entityList.ItemsSource = Enumerable.Range(0, _controller.EntityCount)
                .Select(i => _controller.GetEntityName(i))
                .ToArray();

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

            CreateTransformPoints();
            CreateCurrentTimeVerticalLine();

            canvas.Width = TimeToX(_controller.Duration + 1);
            canvas.Height = _controller.EntityCount * 16 + 64;
        }

        private void RefreshTime()
        {
            var line = _currentTimeVerticalLine;
            if (line == null)
                return;

            line.X1 = TimeToX(_controller.Time);
            line.X2 = line.X1;
        }

        private void CreateCurrentTimeVerticalLine()
        {
            var line = new Line();
            line.IsHitTestVisible = false;
            line.Stroke = Brushes.Black;
            line.Y1 = 0;
            line.Y2 = 1000;
            canvas.Children.Add(line);
            _currentTimeVerticalLine = line;
        }

        private void CreateTimeBorder()
        {
            var duration = _controller.Duration;
            var left = TimeToX(-0.25f);
            var right = TimeToX((duration - 1) + 0.25f);

            var border = new Rectangle();
            border.Stroke = Brushes.Black;
            border.Fill = Brushes.Gray;
            border.Width = right - left;
            border.Height = 18;
            Canvas.SetLeft(border, left);
            canvas.Children.Add(border);
        }

        private void CreateTextBlock(int time)
        {
            var textBlock = new TextBlock();
            textBlock.Text = time.ToString();
            textBlock.Width = Scale;
            textBlock.TextAlignment = TextAlignment.Center;
            Canvas.SetLeft(textBlock, TimeToX(time - 0.5f));
            Canvas.SetTop(textBlock, 0);
            canvas.Children.Add(textBlock);
        }

        private void CreateTransformPoints()
        {
            if (_selectedEntity == null)
            {
                var duration = _controller.Duration;
                for (var i = 0; i < _controller.EntityCount; i++)
                {
                    object lastEntity = null;
                    for (var t = 0; t < duration; t++)
                    {
                        var entity = _controller.GetEntity(i, t);
                        var different = lastEntity == null || !lastEntity.Equals(entity);
                        CreateTransformPoint(i, t, different);
                        lastEntity = entity;
                    }

                    if (i % 3 == 2)
                    {
                        var hLine = new Line();
                        hLine.IsHitTestVisible = false;
                        hLine.Stroke = Brushes.LightGray;
                        hLine.X1 = TimeToX(-0.25f);
                        hLine.Y1 = 24 + i * 16 + 14;
                        hLine.X2 = 2000;
                        hLine.Y2 = hLine.Y1;
                        canvas.Children.Add(hLine);
                    }
                }
            }
            else
            {
                var startIndex = canvas.Children.Count;

                var i = _selectedEntity.Value;
                var duration = _controller.Duration;
                Rectangle lastRect = null;
                for (var t = 0; t < duration; t++)
                {
                    var entity = _controller.GetEntity(i, t);
                    if (entity == null)
                        continue;

                    var maxWidth = Scale / 2;
                    var maxHeight = 8;
                    var size = Math.Min(maxWidth, maxHeight);
                    var diamond = new Rectangle();
                    diamond.Stroke = Brushes.Black;
                    diamond.Fill = Brushes.White;
                    Canvas.SetLeft(diamond, TimeToX(t));
                    Canvas.SetTop(diamond, ValueToY(entity.Value));
                    diamond.Width = size;
                    diamond.Height = size;
                    diamond.RenderTransform = new TransformGroup()
                    {
                        Children = new TransformCollection()
                            {
                                new TranslateTransform(-size / 2, -size / 2),
                                new RotateTransform(45)
                            }
                    };
                    diamond.Tag = t;
                    canvas.Children.Add(diamond);

                    if (lastRect != null)
                    {
                        var cline = new Line();
                        cline.IsHitTestVisible = false;
                        cline.Stroke = Brushes.LightBlue;
                        cline.X1 = Canvas.GetLeft(lastRect);
                        cline.Y1 = Canvas.GetTop(lastRect);
                        cline.X2 = Canvas.GetLeft(diamond);
                        cline.Y2 = Canvas.GetTop(diamond);
                        canvas.Children.Insert(startIndex, cline);
                    }
                    lastRect = diamond;
                }

                for (double value = 0; value <= 1; value += 0.5)
                {
                    var hLine = new Line();
                    hLine.IsHitTestVisible = false;
                    hLine.Stroke = Brushes.LightGray;
                    hLine.X1 = TimeToX(-0.25f);
                    hLine.Y1 = ValueToY(value);
                    hLine.X2 = 2000;
                    hLine.Y2 = hLine.Y1;
                    canvas.Children.Add(hLine);
                }
            }
        }

        private void CreateTransformPoint(int entity, int time, bool different)
        {
            var maxWidth = Scale / 2;
            var maxHeight = 8;
            var size = Math.Min(maxWidth, maxHeight);

            var diamond = new Rectangle();
            diamond.Stroke = Brushes.Black;
            diamond.Fill = different ? Brushes.Aqua : Brushes.White;
            Canvas.SetLeft(diamond, TimeToX(time));
            Canvas.SetTop(diamond, 24 + entity * 16);
            diamond.Width = size;
            diamond.Height = size;
            diamond.RenderTransform = new RotateTransform(45);
            diamond.Tag = (entity, time);
            canvas.Children.Add(diamond);
        }

        private void CreateKeyFrameVerticalLine(int time)
        {
            var line = new Line();
            line.Stroke = Brushes.LightGray;
            line.X1 = TimeToX(time);
            line.Y1 = 0;
            line.X2 = line.X1;
            line.Y2 = 1000;
            canvas.Children.Add(line);
        }

        private double TimeToX(double time) => (time + 0.25) * Scale;
        private double XToTime(double x) => (x / Scale) - 0.25;

        private static double Wrap(double x)
        {
            while (x < 0)
                x += 1;
            while (x > 1)
                x -= 1;
            return x;
        }

        private double ValueToY(double v)
        {
            var normalized = (Wrap(v + 0.5)) * 2 - 1;
            var result = 24 + ((1 + normalized) * 128);
            return result;
        }

        private double YToValue(double y)
        {
            var result = y;
            result -= 24;
            result /= 128;
            result -= 1;
            result += 1;
            result /= 2;
            result = Wrap(result - 0.5);
            return result;
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

        private void canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsOrDescendantOf(e.OriginalSource, canvas))
                return;

            if (e.OriginalSource is Rectangle rectangle && rectangle.Tag is int t)
            {
                _pickupEntityPoint = rectangle;
                _pickupOffset = e.GetPosition(canvas).Y - Canvas.GetTop(rectangle);
            }

            canvas_PreviewMouseMove(sender, e);
            Focus();
        }

        private void canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_lastEntityPoint != null)
            {
                _lastEntityPoint.Fill = Brushes.Aqua;
                _lastEntityPoint = null;
            }

            if (!IsOrDescendantOf(e.OriginalSource, canvas))
                return;

            if (_pickupEntityPoint != null)
            {
                var pos = e.GetPosition(canvas);
                pos.Offset(0, _pickupOffset);
                Canvas.SetTop(_pickupEntityPoint, pos.Y);

                var newValue = YToValue(pos.Y);
                _controller.SetEntity(_selectedEntity.Value, (int)_pickupEntityPoint.Tag, newValue);

                if (_controller.Playing)
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(canvas);
                var keyFrame = (int)Math.Round(XToTime(pos.X));
                _controller.KeyFrame = keyFrame;
                _controller.Playing = false;
            }

            if (e.OriginalSource is Rectangle rect)
            {
                if (rect.Tag is ValueTuple<int, int> tag)
                {
                    _lastEntityPoint = rect;
                    _lastEntityPoint.Fill = Brushes.Red;
                }
            }

            e.Handled = true;
        }

        private void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsOrDescendantOf(e.OriginalSource, canvas))
                return;

            if (_pickupEntityPoint != null)
            {
                _pickupEntityPoint = null;
                e.Handled = true;
            }
        }

        private void canvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsOrDescendantOf(e.OriginalSource, canvas))
                return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                var diff = (e.Delta / 120.0f) * 2;
                var newScale = Math.Min(50, Math.Max(10, Scale + diff));
                if (newScale != Scale)
                {
                    Scale = newScale;
                    Refresh();
                }
                e.Handled = true;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
                return;

            if (_lastEntityPoint != null)
            {
                var tag = (ValueTuple<int, int>)_lastEntityPoint.Tag;
                var entity = tag.Item1;
                var time = tag.Item2;
                var value = _controller.GetEntity(entity, time);
                if (value != null)
                {
                    var diff = e.Delta / 120.0f / 16;
                    _controller.SetEntity(entity, time, value.Value + diff);
                }
            }
            e.Handled = true;
        }

        private static bool IsOrDescendantOf(object start, object parent)
        {
            var curr = start as DependencyObject;
            while (curr != null)
            {
                if (curr == parent)
                    return true;
                curr = VisualTreeHelper.GetParent(curr);
            }
            return false;
        }

        private void entityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (entityList.SelectedIndex == -1)
                _selectedEntity = null;
            else
                _selectedEntity = entityList.SelectedIndex;

            Refresh();
            RefreshTime();
        }
    }
}
