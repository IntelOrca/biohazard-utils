using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Room;
using IntelOrca.Biohazard.Script;
using IntelOrca.Biohazard.Script.Opcodes;

namespace IntelOrca.Biohazard.RoomViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _cutsceneJsonPath = @"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\cutscene.json";
        // private string _enemyJsonPath = @"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\enemy.json";

        private readonly Dictionary<RdtId, CutsceneRoomInfo> _cutsceneRoomInfoMap = new Dictionary<RdtId, CutsceneRoomInfo>();
        private Point _origin;
        private Rdt2 _rdt;
        private PointOfInterest _lastPoi;

        public MainWindow()
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            LoadCutsceneRoomInfo();
            var fsw = new FileSystemWatcher(System.IO.Path.GetDirectoryName(_cutsceneJsonPath));
            fsw.EnableRaisingEvents = true;
            fsw.Changed += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Thread.Sleep(100);
                    LoadCutsceneRoomInfo();
                    LoadMap((RdtId)roomDropdown.SelectedItem);
                });
            };

            var rdtIds = Directory
                .GetFiles(@"F:\games\re2\data\Pl0\Rdt")
                .Select(x => x.Substring(x.Length - 8, 3))
                .Select(x => RdtId.Parse(x))
                .Where(x => x.Stage <= 7)
                .ToArray();

            roomDropdown.ItemsSource = rdtIds;
            roomDropdown.SelectedIndex = Array.IndexOf(rdtIds, RdtId.FromInteger(0x107));
        }

        private Rdt2 LoadRdt(RdtId id)
        {
            var path = $@"F:\games\re2\data\Pl0\Rdt\ROOM{id}0.RDT";
            return new Rdt2(BioVersion.Biohazard2, path);
        }

        private void LoadMap(RdtId id)
        {
            _rdt = LoadRdt(id);

            canvas.Children.Clear();

            AddCollision();
            AddAots();
            AddPointsOfInterest(id);
            SortChildren();
        }

        private void AddCollision()
        {
            var sca = _rdt.SCA;
            var entries = MemoryMarshal.Cast<byte, SCAEntry>(sca)
                .ToArray()
                .OrderBy(x => x.T1)
                .ToArray();
            var index = -1;
            foreach (var entry in entries)
            {
                index++;

                // if (entry.T1 != 0x1F && entry.T1 != 0xFF && entry.T1 != 0x3F)
                //     continue;
                // if ((entry.T1 & 0x0F) != 0x0F && (entry.T1 & 0xF0) != 0)
                //     continue;

                // var a = (byte)(entry.GG & 0xFF);
                // var b = (byte)((entry.GG & 0xFF00) >> 8);
                // if (entry.GG != 0xFC80)
                //     continue;

                // var shape = entry.GG & 0b1111;
                if ((entry.T1 & 0x01) == 0)
                {
                    continue;
                }

                var amount = (byte)Lerp(192, 0, entry.T1 / 255.0);

                var rect = new Rectangle();
                rect.Fill = new SolidColorBrush(Color.FromArgb(255, amount, amount, amount));
                rect.Opacity = 1;

                Canvas.SetLeft(rect, entry.X);
                Canvas.SetTop(rect, entry.Z);
                rect.Width = Math.Abs(entry.W);
                rect.Height = Math.Abs(entry.D);
                canvas.Children.Add(rect);
            }
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

#pragma warning disable 649
        private unsafe struct SCAEntry
        {
            public short X;
            public short Z;
            public short W;
            public short D;
            public ushort GG;
            public ushort unk_A;
            public byte T1;
            public byte unk_0D;
            public byte unk_0E;
            public byte unk_0F;
        }
#pragma warning restore 649

        private void AddAots()
        {
            var builder = new ScriptAstBuilder();
            _rdt.ReadScript(builder);
            var ast = builder.Ast;
            var aotVisitor = new AotVisitor(this);
            ast.Visit(aotVisitor);
        }

        private void AddPointsOfInterest(RdtId id)
        {
            if (!_cutsceneRoomInfoMap.TryGetValue(id, out var info))
                return;

            if (info.Poi != null)
            {
                _origin = GetOrigin(info);
                foreach (var poi in info.Poi)
                {
                    if (poi.X == 0 && poi.Z == 0)
                        continue;

                    var tags = poi.Tags ?? new string[0];

                    var node = new Ellipse();
                    node.Fill = GetNodeColor(tags.FirstOrDefault());
                    node.Width = 1000;
                    node.Height = 1000;
                    node.Tag = poi;

                    Canvas.SetLeft(node, poi.X - (node.Width / 2));
                    Canvas.SetTop(node, poi.Z - (node.Height / 2));
                    canvas.Children.Add(node);

                    if (tags.Contains(PoiKind.Door) ||
                        tags.Contains(PoiKind.Stairs) ||
                        tags.Contains(PoiKind.Meet) ||
                        tags.Contains(PoiKind.Npc))
                    {
                        var angle = GetAngle(poi);
                        var lengthX = Math.Cos(angle) * 2000;
                        var lengthZ = Math.Sin(angle) * 2000;

                        var arrow = new Line();
                        arrow.Stroke = node.Fill;
                        arrow.StrokeThickness = 256;
                        arrow.X1 = poi.X;
                        arrow.Y1 = poi.Z;
                        arrow.X2 = poi.X + lengthX;
                        arrow.Y2 = poi.Z + lengthZ;
                        arrow.Tag = poi;
                        canvas.Children.Add(arrow);
                    }

                    if (poi.Tags.Length > 1)
                    {
                        var nodeInner = new Ellipse();
                        nodeInner.Fill = GetNodeColor(poi.Tags[1]);
                        nodeInner.Width = 500;
                        nodeInner.Height = 500;
                        nodeInner.Tag = poi;

                        Canvas.SetLeft(nodeInner, poi.X - (nodeInner.Width / 2));
                        Canvas.SetTop(nodeInner, poi.Z - (nodeInner.Height / 2));
                        canvas.Children.Add(nodeInner);
                    }

                    var textBlock = new TextBlock();
                    textBlock.RenderTransform = new ScaleTransform(1, -1);
                    textBlock.FontSize = 600;
                    textBlock.Text = poi.Cut.ToString();
                    Canvas.SetLeft(textBlock, poi.X + 400);
                    Canvas.SetTop(textBlock, poi.Z + 0);
                    canvas.Children.Add(textBlock);

                    if (poi.Edges != null)
                    {
                        foreach (var edge in poi.Edges)
                        {
                            var connection = info.Poi.FirstOrDefault(x => x.Id == edge);
                            if (connection != null && !(connection.X == 0 && connection.Z == 0))
                            {
                                var line = new Line();
                                line.Stroke = Brushes.Black;
                                line.StrokeThickness = 32;
                                line.X1 = poi.X;
                                line.Y1 = poi.Z;
                                line.X2 = connection.X;
                                line.Y2 = connection.Z;
                                canvas.Children.Add(line);
                            }
                        }
                    }
                }
            }
        }

        private void SortChildren()
        {
            var children = new UIElement[canvas.Children.Count];
            for (var i = 0; i < canvas.Children.Count; i++)
            {
                children[i] = canvas.Children[i];
            }
            children = children.OrderBy(x =>
            {
                if (x is FrameworkElement fe && fe.Tag is PointOfInterest)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }).ToArray();
            canvas.Children.Clear();
            foreach (var el in children)
            {
                canvas.Children.Add(el);
            }
        }

        private Brush GetNodeColor(string kind)
        {
            switch (kind)
            {
                case PoiKind.Npc:
                    return Brushes.Red;
                case PoiKind.Door:
                    return Brushes.Brown;
                case PoiKind.Stairs:
                    return Brushes.Brown;
                case PoiKind.Meet:
                    return Brushes.Red;
                case PoiKind.Trigger:
                case PoiKind.Waypoint:
                    return Brushes.Aqua;
                default:
                    return Brushes.Black;
            }
        }

        private double GetAngle(PointOfInterest poi)
        {
            var angleNormalized = poi.D / (1024.0 * 4);
            var angle = angleNormalized * (Math.PI * 2);
            return -angle;
        }

        private Point GetDrawPos(PointOfInterest poi) => GetDrawPos(poi.X, poi.Z);

        private Point GetDrawPos(int x, int z)
        {
            var offsetX = 250;
            var offsetY = 250;
            var ratio = 1 / 100.0;
            var xx = (x - _origin.X) * ratio;
            var yy = (z - _origin.Y) * ratio;
            return new Point(offsetX + xx, offsetY - yy);
        }

        private Point GetOrigin(CutsceneRoomInfo info)
        {
            if (info.Poi == null || info.Poi.Length == 0)
                return new Point();

            var x = 0;
            var y = 0;
            foreach (var poi in info.Poi)
            {
                x += poi.X;
                y += poi.Z;
            }
            x /= info.Poi.Length;
            y /= info.Poi.Length;
            return new Point(x, y);
        }

        private void LoadCutsceneRoomInfo()
        {
            _cutsceneRoomInfoMap.Clear();

            while (true)
            {
                try
                {
                    var json = ReadAllText(_cutsceneJsonPath);

                    var map = JsonSerializer.Deserialize<Dictionary<string, CutsceneRoomInfo>>(json, new JsonSerializerOptions()
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    foreach (var kvp in map)
                    {
                        var key = RdtId.Parse(kvp.Key);
                        _cutsceneRoomInfoMap[key] = kvp.Value;
                    }
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private static string ReadAllText(string path)
        {
            Exception cachedException = null;
            for (var i = 0; i < 50; i++)
            {
                try
                {
                    var result = File.ReadAllText(path);
                    if (result.Length == 0)
                        continue;
                    return result;
                }
                catch (Exception ex)
                {
                    cachedException = ex;
                }
                Thread.Sleep(100);
            }
            if (cachedException != null)
                throw cachedException;
            else
                throw new Exception();
        }

        private void roomDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (roomDropdown.SelectedItem is RdtId roomId)
            {
                LoadMap(roomId);
            }
        }

        internal class AotVisitor : ScriptAstVisitor
        {
            private readonly MainWindow _mainWindow;

            public AotVisitor(MainWindow mainWindow)
            {
                _mainWindow = mainWindow;
            }

            public override void VisitOpcode(OpcodeAstNode node)
            {
                base.VisitOpcode(node);

                var canvas = _mainWindow.canvas;
                if (node.Opcode is DoorAotSeOpcode door)
                {
                    var rect = new Rectangle();
                    rect.Fill = Brushes.Brown;
                    rect.Opacity = 0.5;

                    Canvas.SetLeft(rect, door.X);
                    Canvas.SetTop(rect, door.Z);
                    rect.Width = Math.Abs(door.W);
                    rect.Height = Math.Abs(door.D);
                    canvas.Children.Add(rect);
                }
                else if (node.Opcode is ItemAotSetOpcode item)
                {
                    var rect = new Rectangle();
                    rect.Fill = Brushes.Blue;
                    rect.Opacity = 0.5;

                    Canvas.SetLeft(rect, item.X);
                    Canvas.SetTop(rect, item.Y);
                    rect.Width = Math.Abs(item.W);
                    rect.Height = Math.Abs(item.H);
                    canvas.Children.Add(rect);
                }
                else if (node.Opcode is AotSetOpcode aot)
                {
                    var rect = new Rectangle();
                    rect.Fill = Brushes.Gray;
                    rect.Opacity = 0.25;

                    Canvas.SetLeft(rect, aot.X);
                    Canvas.SetTop(rect, aot.Z);
                    rect.Width = Math.Abs(aot.W);
                    rect.Height = Math.Abs(aot.D);
                    canvas.Children.Add(rect);
                }
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var (pos, poi) = MapPosition(sender, e);
            positionStatusBarItem.Content = $"{pos.X}, {pos.Y}";

            poi = poi == null ? _lastPoi : poi;
            if (poi != null)
            {
                poiStatusBarItem.Content = $"POI: {poi.Id}, [{string.Join(", ", poi.Tags ?? new string[0])}]";
            }
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var (pos, poi) = MapPosition(sender, e);
            lastPositionStatusBarItem.Content = $"{pos.X}, {pos.Y}";
            _lastPoi = poi;
        }

        private Tuple<Point, PointOfInterest> MapPosition(object sender, MouseEventArgs e)
        {
            var zoomBorder = sender as ZoomBorder;
            var mousePos = e.GetPosition(sender as IInputElement);
            var pos = zoomBorder.GetPosition(mousePos);
            pos.X = (((int)pos.X) / 10) * 10;
            pos.Y = (((int)pos.Y) / 10) * 10;
            positionStatusBarItem.Content = $"{pos.X}, {pos.Y}";

            PointOfInterest poi = null;
            var hitResult = VisualTreeHelper.HitTest(zoomBorder, mousePos);
            if (hitResult != null)
            {
                var uiElement = hitResult.VisualHit as FrameworkElement;
                poi = uiElement.Tag as PointOfInterest;
            }
            return new Tuple<Point, PointOfInterest>(pos, poi);
        }
    }
}
