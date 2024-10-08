﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using IntelOrca.Biohazard.Model;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TimelineView.xaml
    /// </summary>
    public partial class TimelineView : UserControl
    {
        private Button _playButton;
        private Button _stopButton;
        private IAnimationController _controller;
        private int? _selectedEntity;

        public IAnimationController Controller
        {
            get => _controller;
            set
            {
                if (_controller != value)
                {
                    if (_controller != null)
                    {
                        _controller.DataChanged -= OnControllerDataChanged;
                        _controller.TimeChanged -= OnControllerTimeChanged;
                    }
                    _controller = value;
                    _controller.DataChanged += OnControllerDataChanged;
                    _controller.TimeChanged += OnControllerTimeChanged;
                    OnControllerDataChanged(this, EventArgs.Empty);
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
            AddToolbarButton("IconAdd", "Insert keyframe", () => _controller.Insert());
            AddToolbarButton("IconContentDuplicate", "Duplicate current keyframe", () => _controller.Duplicate());
            AddToolbarButton("IconDelete", "Delete current keyframe", () => _controller.Delete());
            AddToolbarSeparator();
            AddToolbarButton("IconFunction", "Keyframe function", () => EditKeyframeFunction());
            AddToolbarButton("IconHumanMaleHeightVariant", "Keyframe offset", () => EditKeyframeOffset());
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

        private void EditKeyframeFunction()
        {
            var time = _controller.KeyFrame;
            var f = _controller.GetFunction(time);
            var result = InputWindow.Show(
                "Keyframe Function",
                "Type in the function number for the keyframe.\nThis can control when the floor sound plays, or when the ammo count is set.",
                $"0x{f:X}",
                s => ParseFunctionNumber(s) != null);
            if (result != null && ParseFunctionNumber(result) is int newValue && newValue != f)
            {
                _controller.SetFunction(time, newValue);
            }
        }

        private void EditKeyframeOffset()
        {
            var time = _controller.KeyFrame;
            var f = _controller.GetOffset(time);
            var result = InputWindow.Show(
                "Keyframe Offset",
                "Type in the offset for this keyframe.\nUse this to place the mesh at the correct height off the floor.",
                $"{f.x},{f.y},{f.z}",
                s => ParseOffset(s) != null);
            if (result != null && ParseOffset(result) is Emr.Vector newValue && newValue != f)
            {
                _controller.SetOffset(time, newValue);
            }
        }

        private void OnControllerDataChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var entityNames = entityList.ItemsSource as string[];
                if (entityNames == null || entityNames.Length != _controller.EntityCount)
                {
                    entityList.ItemsSource = Enumerable.Range(0, _controller.EntityCount)
                        .Select(i => _controller.GetEntityName(i))
                        .ToArray();
                }

                timeline.Duration = _controller.Duration;
                timeline.Time = _controller.Time;
                UpdateToolbarVisibility();
                UpdateFlags();
                UpdateSeries();
            }, DispatcherPriority.Render);
        }

        private void OnControllerTimeChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                timeline.Time = _controller.Time;
                UpdateToolbarVisibility();
            }, DispatcherPriority.Render);
        }

        private void UpdateToolbarVisibility()
        {
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
            timeline.Playing = _controller.Playing;
        }

        private void UpdateFlags()
        {
            var duration = _controller.Duration;
            var allFlags = new int[duration];
            for (var t = 0; t < duration; t++)
            {
                var flags = _controller.GetFunction(t);
                allFlags[t] = flags;
            }
            timeline.Flags = allFlags;
        }

        private void UpdateSeries()
        {
            if (entityList.SelectedIndex == -1)
            {
                _selectedEntity = null;
                timeline.Series = new TimelineSeries[0];
            }
            else
            {
                var entity = entityList.SelectedIndex;

                _selectedEntity = entity;

                var series = timeline.Series.FirstOrDefault();
                if (series == null)
                {
                    series = new TimelineSeries();
                    series.PointChanged += (s, e2) =>
                    {
                        _controller.SetEntity(entityList.SelectedIndex, e2.Time, e2.NewValue);
                    };
                    series.PointDoubleClicked += (s, e2) =>
                    {
                        var value = _controller.GetEntityRaw(entityList.SelectedIndex, e2.Time);
                        var result = InputWindow.Show(
                            "Edit point",
                            "Edit the raw value of this point.",
                            $"{value}",
                            ss => ParseFunctionNumber(ss) != null);
                        if (result != null && ParseFunctionNumber(result) is int newValue)
                        {
                            _controller.SetEntityRaw(entityList.SelectedIndex, e2.Time, newValue);
                        }
                    };
                    timeline.Series = new[] { series };
                }

                series.Points = Enumerable
                    .Range(0, _controller.Duration)
                    .Select(x => _controller.GetEntity(entity, x) ?? 0)
                    .ToArray();
            }
        }

        private void entityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSeries();
        }

        private void timeline_TimeChanged(object sender, EventArgs e)
        {
            _controller.Playing = false;
            _controller.Time = timeline.Time;
        }

        private void timeline_PlayToggled(object sender, EventArgs e)
        {
            _controller.Playing = !_controller.Playing;
        }

        private static int? ParseNumber(string s, int min, int max)
        {
            s = s.Trim();
            int result;
            if (s.StartsWith("0x"))
            {
                if (!int.TryParse(s.Substring(2), NumberStyles.HexNumber, null, out result))
                    return null;
            }
            else
            {
                if (!int.TryParse(s, out result))
                    return null;
            }
            if (result < min || result > max)
                return null;
            return (int)result;
        }

        private static int? ParseFunctionNumber(string s) => ParseNumber(s, 0, 0xFFFFF);

        private static Emr.Vector? ParseOffset(string s)
        {
            var values = s
                .Split(',')
                .Select(ss => ParseNumber(ss, short.MinValue, short.MaxValue))
                .ToArray();
            if (values.Length != 3)
                return null;

            if (values.Any(x => x == null))
                return null;

            return new Emr.Vector(
                (short)values[0],
                (short)values[1],
                (short)values[2]);
        }
    }
}
