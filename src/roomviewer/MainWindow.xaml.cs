using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
        private string _enemyJsonPath = @"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\enemy.json";

        private readonly Dictionary<RdtId, CutsceneRoomInfo> _cutsceneRoomInfoMap = new Dictionary<RdtId, CutsceneRoomInfo>();
        private Point _origin;
        private Rdt2 _rdt;

        public MainWindow()
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            LoadCutsceneRoomInfo();

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
            AddPointsOfInterest(id);
            AddAots();
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

                    var node = new Ellipse();
                    node.Fill = GetNodeColor(poi.Kind);
                    node.Width = 1000;
                    node.Height = 1000;

                    Canvas.SetLeft(node, poi.X - (node.Width / 2));
                    Canvas.SetTop(node, poi.Z - (node.Height / 2));

                    canvas.Children.Add(node);

                    if (poi.Edges != null)
                    {
                        foreach (var edge in poi.Edges)
                        {
                            var connection = info.Poi.FirstOrDefault(x => x.Id == edge);
                            if (connection != null && !(connection.X == 0 && connection.Y == 0))
                            {
                                var line = new Line();
                                line.Stroke = Brushes.Black;
                                line.StrokeThickness = 8;
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
                case PoiKind.Waypoint:
                    return Brushes.Aqua;
                default:
                    return Brushes.Black;
            }
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

            var json = File.ReadAllText(_cutsceneJsonPath);
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

        private void canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var zoomBorder = sender as ZoomBorder;
            var mousePos = e.GetPosition(sender as IInputElement);
            var pos = zoomBorder.GetPosition(mousePos);
            pos.X = (((int)pos.X) / 10) * 10;
            pos.Y = (((int)pos.Y) / 10) * 10;
            positionStatusBarItem.Content = $"{pos.X}, {pos.Y}";
        }
    }
}
