﻿// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

using Mzinga.Core;
using Mzinga.Viewer.ViewModels;

namespace Mzinga.Viewer
{
    public class XamlBoardRenderer
    {
        public MainViewModel VM { get; private set; }

        public Canvas BoardCanvas { get; private set; }
        public StackPanel WhiteHandStackPanel { get; private set; }
        public StackPanel BlackHandStackPanel { get; private set; }

        private Point? DragStartPoint = null;
        private PieceName DragStartPieceName = PieceName.INVALID;
        private double DragStartCanvasOffsetX = 0.0;
        private double DragStartCanvasOffsetY = 0.0;
        private bool MoveAfterDragStart = false;

        private double MinDragDistanceToStartPan => BoardPieceSize / 3.0;

        private readonly double PieceCanvasMargin = 3.0;

        private double CanvasOffsetX = 0.0;
        private double CanvasOffsetY = 0.0;

        private double CurrentZoomFactor = 0.0;

        private double BoardPieceSize => GetDefaultPieceSize() * Math.Pow(2.0, CurrentZoomFactor);

        private double HandPieceSize => GetDefaultPieceSize();

        public bool RaiseStackedPieces
        {
            get
            {
                return _raiseStackedPieces;
            }
            set
            {
                bool oldValue = _raiseStackedPieces;
                if (oldValue != value)
                {
                    _raiseStackedPieces = value;
                    DrawBoard(LastBoard);
                }
            }
        }
        private bool _raiseStackedPieces;

        private double StackShiftRatio
        {
            get
            {
                return RaiseStackedPieces ? RaisedStackShiftLevel : BaseStackShiftLevel;
            }
        }

        private const double BaseStackShiftLevel = 0.1;
        private const double RaisedStackShiftLevel = 0.5;

        private const double GraphicalBugSizeRatio = 1.25;

        private Board LastBoard;

        private readonly SolidColorBrush WhiteBrush;
        private readonly SolidColorBrush BlackBrush;

        private readonly SolidColorBrush PieceOutlineBrush;

        private readonly SolidColorBrush SelectedMoveEdgeBrush;
        private readonly SolidColorBrush SelectedMoveBodyBrush;

        private readonly SolidColorBrush LastMoveEdgeBrush;

        private readonly SolidColorBrush DisabledPieceBrush;

        private readonly SolidColorBrush[] BugBrushes;

        public XamlBoardRenderer(MainViewModel vm, Canvas boardCanvas, StackPanel whiteHandStackPanel, StackPanel blackHandStackPanel)
        {
            VM = vm ?? throw new ArgumentNullException(nameof(vm));
            BoardCanvas = boardCanvas ?? throw new ArgumentNullException(nameof(boardCanvas));
            WhiteHandStackPanel = whiteHandStackPanel ?? throw new ArgumentNullException(nameof(whiteHandStackPanel));
            BlackHandStackPanel = blackHandStackPanel ?? throw new ArgumentNullException(nameof(blackHandStackPanel));

            // Init brushes
            WhiteBrush = new SolidColorBrush(Colors.White);
            BlackBrush = new SolidColorBrush(Colors.Black);

            PieceOutlineBrush = new SolidColorBrush(Color.Parse("#333333"));

            SelectedMoveEdgeBrush = new SolidColorBrush(Colors.Orange);
            SelectedMoveBodyBrush = new SolidColorBrush(Colors.Aqua)
            {
                Opacity = 0.25
            };

            LastMoveEdgeBrush = new SolidColorBrush(Colors.SeaGreen);

            DisabledPieceBrush = new SolidColorBrush(Colors.LightGray);

            BugBrushes = new SolidColorBrush[]
            {
                new SolidColorBrush(Color.FromArgb(255, 250, 167, 29)), // Bee
                new SolidColorBrush(Color.FromArgb(255, 139, 63, 27)), // Spider
                new SolidColorBrush(Color.FromArgb(255, 149, 101, 194)), // Beetle
                new SolidColorBrush(Color.FromArgb(255, 65, 157, 70)), // Grasshopper
                new SolidColorBrush(Color.FromArgb(255, 37, 141, 193)), // Ant
                new SolidColorBrush(Color.FromArgb(255, 111, 111, 97)), // Mosquito
                new SolidColorBrush(Color.FromArgb(255, 209, 32, 32)), // Ladybug
                new SolidColorBrush(Color.FromArgb(255, 37, 153, 102)), // Pillbug
            };

            // Bind board updates to VM
            if (VM is not null)
            {
                VM.PropertyChanged += VM_PropertyChanged;
            }

            // Attach events
            BoardCanvas.PropertyChanged += BoardCanvas_SizeChanged;
            BoardCanvas.PointerPressed += BoardCanvas_PointerPressed;
            BoardCanvas.PointerMoved += BoardCanvas_PointerMoved;
            BoardCanvas.PointerReleased += BoardCanvas_PointerReleased;
            BoardCanvas.PointerWheelChanged += BoardCanvas_PointerWheelChanged;
            WhiteHandStackPanel.PointerReleased += CancelClick;
            BlackHandStackPanel.PointerReleased += CancelClick;
        }

        public void TryRedraw(bool forceAutoCenter = false, bool forceAutoZoom = false)
        {
            if (DateTime.Now - LastRedrawOnSizeChange > TimeSpan.FromMilliseconds(10))
            {
                DrawBoard(LastBoard, forceAutoCenter, forceAutoZoom);
                LastRedrawOnSizeChange = DateTime.Now;
            }
        }

        public bool TrySetZoom(double value)
        {
            var newValue = Math.Clamp(value, -1.0, 2.0);
            if (CurrentZoomFactor != newValue)
            {
                CurrentZoomFactor = newValue;
                return true;
            }
            return false;
        }

        public bool TryIncreaseZoom()
        {
            return TrySetZoom(CurrentZoomFactor + 0.1);
        }

        public bool TryDecreaseZoom()
        {
            return TrySetZoom(CurrentZoomFactor - 0.1);
        }

        private double GetDefaultPieceSize()
        {
            int numPiecesToDisplay = MainViewModel.ViewerConfig.StackPiecesInHand ? 2 + Enums.NumBugTypes(LastBoard?.GameType ?? GameType.Base) : 2 + (Enums.NumPieceNames(LastBoard?.GameType ?? GameType.Base) / 2);
            return 0.5 * ((BoardCanvas.Bounds.Height / numPiecesToDisplay) - (2 * PieceCanvasMargin));
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.Board):
                case nameof(MainViewModel.ValidMoves):
                case nameof(MainViewModel.TargetMove):
                case nameof(MainViewModel.ViewerConfig):
                case nameof(MainViewModel.AutoCenterBoard):
                case nameof(MainViewModel.AutoZoomBoard):
                    AppViewModel.Instance.DoOnUIThread(() =>
                    {
                        DrawBoard(MainViewModel.Board);
                    });
                    break;
            }
        }

        private void DrawBoard(Board board, bool forceAutoCenter = false, bool forceAutoZoom = false)
        {
            BoardCanvas.Children.Clear();
            WhiteHandStackPanel.Children.Clear();
            BlackHandStackPanel.Children.Clear();

            int z = BoardCanvas.ZIndex;

            bool autoCenter = forceAutoCenter || MainViewModel.ViewerConfig.AutoCenterBoard || LastBoard is null || LastBoard.BoardState == BoardState.NotStarted;
            bool autoZoom = forceAutoZoom || MainViewModel.ViewerConfig.AutoZoomBoard || LastBoard is null || LastBoard.BoardState == BoardState.NotStarted;

            if (board is not null)
            {
                Point minPoint = new Point(double.MaxValue, double.MaxValue);
                Point maxPoint = new Point(double.MinValue, double.MinValue);

                double boardCanvasWidth = BoardCanvas.Bounds.Width;
                double boardCanvasHeight = BoardCanvas.Bounds.Height;

                var piecesInPlay = GetPiecesOnBoard(board, out int numPieces, out int maxStack);

                int whiteHandCount = board.GetWhiteHand().Count();
                int blackHandCount = board.GetBlackHand().Count();

                int verticalPiecesMin = 3 + Math.Max(Math.Max(whiteHandCount, blackHandCount), board.GetWidth());
                int horizontalPiecesMin = 2 + Math.Min(whiteHandCount, 1) + Math.Min(blackHandCount, 1) + board.GetHeight();

                if (autoZoom)
                {
                    double desiredSize = 0.5 * Math.Min(boardCanvasHeight / verticalPiecesMin, boardCanvasWidth / horizontalPiecesMin);
                    TrySetZoom(0.0);
                }

                WhiteHandStackPanel.MinWidth = whiteHandCount > 0 ? (HandPieceSize + PieceCanvasMargin) * 2 : 0;
                BlackHandStackPanel.MinWidth = blackHandCount > 0 ? (HandPieceSize + PieceCanvasMargin) * 2 : 0;

                Position? lastMoveStart = MainViewModel.AppVM.EngineWrapper.Board?.BoardHistory.LastMove?.Source;
                Position? lastMoveEnd = MainViewModel.AppVM.EngineWrapper.Board?.BoardHistory.LastMove?.Destination;

                PieceName selectedPieceName = MainViewModel.AppVM.EngineWrapper.TargetPiece;
                Position? targetPosition = MainViewModel.AppVM.EngineWrapper.TargetPosition;

                MoveSet validMoves = MainViewModel.AppVM.EngineWrapper.ValidMoves;

                HexOrientation hexOrientation = MainViewModel.ViewerConfig.HexOrientation;

                Dictionary<BugType, Stack<Canvas>> pieceCanvasesByBugType = new Dictionary<BugType, Stack<Canvas>>();

                // Draw the pieces in white's hand
                foreach (PieceName pieceName in board.GetWhiteHand())
                {
                    if (pieceName != selectedPieceName || (pieceName == selectedPieceName && targetPosition is null))
                    {
                        BugType bugType = Enums.GetBugType(pieceName);

                        bool disabled = MainViewModel.ViewerConfig.DisablePiecesInHandWithNoMoves && !(validMoves is not null && validMoves.Any(m => m.PieceName == pieceName));
                        Canvas pieceCanvas = GetPieceInHandCanvas(pieceName, HandPieceSize, hexOrientation, disabled);

                        if (!pieceCanvasesByBugType.ContainsKey(bugType))
                        {
                            pieceCanvasesByBugType[bugType] = new Stack<Canvas>();
                        }

                        pieceCanvasesByBugType[bugType].Push(pieceCanvas);
                    }
                }

                DrawHand(WhiteHandStackPanel, pieceCanvasesByBugType);

                pieceCanvasesByBugType.Clear();

                // Draw the pieces in black's hand
                foreach (PieceName pieceName in board.GetBlackHand())
                {
                    if (pieceName != selectedPieceName || (pieceName == selectedPieceName && targetPosition is null))
                    {
                        BugType bugType = Enums.GetBugType(pieceName);

                        bool disabled = MainViewModel.ViewerConfig.DisablePiecesInHandWithNoMoves && !(validMoves is not null && validMoves.Any(m => m.PieceName == pieceName));
                        Canvas pieceCanvas = GetPieceInHandCanvas(pieceName, HandPieceSize, hexOrientation, disabled);

                        if (!pieceCanvasesByBugType.ContainsKey(bugType))
                        {
                            pieceCanvasesByBugType[bugType] = new Stack<Canvas>();
                        }

                        pieceCanvasesByBugType[bugType].Push(pieceCanvas);
                    }
                }

                DrawHand(BlackHandStackPanel, pieceCanvasesByBugType);

                // Draw the pieces in play
                z++;
                for (int stack = 0; stack <= maxStack; stack++)
                {
                    if (piecesInPlay.ContainsKey(stack))
                    {
                        foreach (var tuple in piecesInPlay[stack])
                        {
                            var pieceName = tuple.Item1;
                            var position = tuple.Item2;

                            if (pieceName == selectedPieceName && targetPosition.HasValue)
                            {
                                position = targetPosition.Value;
                            }

                            Point center = GetPoint(position, BoardPieceSize, hexOrientation, true);

                            HexType hexType = (Enums.GetColor(pieceName) == PlayerColor.White) ? HexType.WhitePiece : HexType.BlackPiece;

                            Shape hex = GetHex(center, BoardPieceSize, hexType, hexOrientation);
                            hex.ZIndex = z;
                            BoardCanvas.Children.Add(hex);

                            bool disabled = MainViewModel.ViewerConfig.DisablePiecesInPlayWithNoMoves && !(validMoves is not null && validMoves.Any(m => m.PieceName == pieceName));

                            var hexText = MainViewModel.ViewerConfig.PieceStyle == PieceStyle.Text ? GetPieceText(center, BoardPieceSize, pieceName, disabled) : GetPieceGraphics(center, BoardPieceSize, pieceName, disabled);
                            hexText.ZIndex = z + 1;
                            BoardCanvas.Children.Add(hexText);

                            minPoint = Min(center, BoardPieceSize, minPoint);
                            maxPoint = Max(center, BoardPieceSize, maxPoint);
                        }
                        z += 2;
                    }
                }

                // Highlight last move played
                if (MainViewModel.ViewerConfig.HighlightLastMovePlayed)
                {
                    z++;
                    // Highlight the lastMove start position
                    if (lastMoveStart.HasValue && lastMoveStart.Value.Stack >= 0)
                    {
                        Point center = GetPoint(lastMoveStart.Value, BoardPieceSize, hexOrientation, true);

                        Shape hex = GetHex(center, BoardPieceSize, HexType.LastMove, hexOrientation);
                        hex.ZIndex = z;
                        BoardCanvas.Children.Add(hex);

                        minPoint = Min(center, BoardPieceSize, minPoint);
                        maxPoint = Max(center, BoardPieceSize, maxPoint);
                    }

                    // Highlight the lastMove end position
                    if (lastMoveEnd.HasValue)
                    {
                        Point center = GetPoint(lastMoveEnd.Value, BoardPieceSize, hexOrientation, true);

                        Shape hex = GetHex(center, BoardPieceSize, HexType.LastMove, hexOrientation);
                        hex.ZIndex = z;
                        BoardCanvas.Children.Add(hex);

                        minPoint = Min(center, BoardPieceSize, minPoint);
                        maxPoint = Max(center, BoardPieceSize, maxPoint);
                    }
                }

                // Highlight the selected piece
                if (MainViewModel.ViewerConfig.HighlightTargetMove)
                {
                    z++;
                    if (selectedPieceName != PieceName.INVALID)
                    {
                        Position selectedPiecePosition = board.GetPosition(selectedPieceName);

                        if (selectedPiecePosition != Position.NullPosition)
                        {
                            Point center = GetPoint(selectedPiecePosition, BoardPieceSize, hexOrientation, true);

                            Shape hex = GetHex(center, BoardPieceSize, HexType.SelectedPiece, hexOrientation);
                            hex.ZIndex = z;
                            BoardCanvas.Children.Add(hex);

                            minPoint = Min(center, BoardPieceSize, minPoint);
                            maxPoint = Max(center, BoardPieceSize, maxPoint);
                        }
                    }
                }

                // Draw the valid moves for that piece
                if (MainViewModel.ViewerConfig.HighlightValidMoves)
                {
                    z++;
                    if (selectedPieceName != PieceName.INVALID && validMoves is not null)
                    {
                        foreach (Move validMove in validMoves)
                        {
                            if (validMove.PieceName == selectedPieceName)
                            {
                                Point center = GetPoint(validMove.Destination, BoardPieceSize, hexOrientation);

                                Shape hex = GetHex(center, BoardPieceSize, HexType.ValidMove, hexOrientation);
                                hex.ZIndex = z;
                                BoardCanvas.Children.Add(hex);

                                minPoint = Min(center, BoardPieceSize, minPoint);
                                maxPoint = Max(center, BoardPieceSize, maxPoint);
                            }
                        }
                    }
                }

                // Highlight the target position
                if (MainViewModel.ViewerConfig.HighlightTargetMove)
                {
                    z++;
                    if (targetPosition.HasValue)
                    {
                        Point center = GetPoint(targetPosition.Value, BoardPieceSize, hexOrientation, true);

                        Shape hex = GetHex(center, BoardPieceSize, HexType.SelectedMove, hexOrientation);
                        hex.ZIndex = z;
                        BoardCanvas.Children.Add(hex);

                        minPoint = Min(center, BoardPieceSize, minPoint);
                        maxPoint = Max(center, BoardPieceSize, maxPoint);
                    }
                }

                // Re-center the game board
                if (autoCenter)
                {
                    double boardWidth = Math.Abs(maxPoint.X - minPoint.X);
                    double boardHeight = Math.Abs(maxPoint.Y - minPoint.Y);

                    if (!double.IsInfinity(boardWidth) && !double.IsInfinity(boardHeight))
                    {
                        double boardCenterX = minPoint.X + (boardWidth / 2);
                        double boardCenterY = minPoint.Y + (boardHeight / 2);

                        double canvasCenterX = boardCanvasWidth / 2;
                        double canvasCenterY = boardCanvasHeight / 2;

                        CanvasOffsetX = canvasCenterX - boardCenterX;
                        CanvasOffsetY = canvasCenterY - boardCenterY;
                    }
                }

                TranslateBoardChildren();

                VM.CanvasHexRadius = BoardPieceSize;
                VM.CanRaiseStackedPieces = maxStack > 0;
            }

            LastBoard = board;
        }

        private void TranslateBoardChildren()
        {
            // Translate all game elements on the board
            TranslateTransform translate = new TranslateTransform()
            {
                X = CanvasOffsetX,
                Y = CanvasOffsetY
            };

            foreach (var child in BoardCanvas.Children)
            {
                child.RenderTransform = translate;
            }
        }

        private static Point Min(Point center, double size, Point minPoint)
        {
            double minX = Math.Min(minPoint.X, center.X - size);
            double minY = Math.Min(minPoint.Y, center.Y - size);

            return new Point(minX, minY);
        }

        private static Point Max(Point center, double size, Point maxPoint)
        {
            double maxX = Math.Max(maxPoint.X, center.X + size);
            double maxY = Math.Max(maxPoint.Y, center.Y + size);

            return new Point(maxX, maxY);
        }

        private Point GetPoint(Position position, double size, HexOrientation hexOrientation, bool stackShift = false)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            double x = hexOrientation == HexOrientation.FlatTop ? size * 1.5 * position.Q : size * Math.Sqrt(3.0) * (position.Q + (0.5 * position.R));
            double y = hexOrientation == HexOrientation.FlatTop ? size * Math.Sqrt(3.0) * (position.R + (0.5 * position.Q)) : size * 1.5 * position.R;

            if (stackShift && position.Stack > 0)
            {
                x += hexOrientation == HexOrientation.FlatTop ? size * 1.5 * StackShiftRatio * position.Stack : size * Math.Sqrt(3.0) * StackShiftRatio * position.Stack;
                y -= hexOrientation == HexOrientation.FlatTop ? size * Math.Sqrt(3.0) * StackShiftRatio * position.Stack : size * 1.5 * StackShiftRatio * position.Stack;
            }

            return new Point(x, y);
        }

        private static Dictionary<int, List<Tuple<PieceName, Position>>> GetPiecesOnBoard(Board board, out int numPieces, out int maxStack)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            numPieces = 0;
            maxStack = -1;

            Dictionary<int, List<Tuple<PieceName, Position>>> pieces = new Dictionary<int, List<Tuple<PieceName, Position>>>
            {
                [0] = new List<Tuple<PieceName, Position>>()
            };

            PieceName targetPieceName = MainViewModel.AppVM.EngineWrapper.TargetPiece;
            Position? targetPosition = MainViewModel.AppVM.EngineWrapper.TargetPosition;

            bool targetPieceInPlay = false;

            // Add pieces already on the board
            foreach (PieceName pieceName in board.GetPiecesInPlay())
            {
                Position position = board.GetPosition(pieceName);

                if (pieceName == targetPieceName)
                {
                    if (targetPosition.HasValue)
                    {
                        position = targetPosition.Value;
                    }
                    targetPieceInPlay = true;
                }

                int stack = position.Stack;
                maxStack = Math.Max(maxStack, stack);

                if (!pieces.ContainsKey(stack))
                {
                    pieces[stack] = new List<Tuple<PieceName, Position>>();
                }

                pieces[stack].Add(new Tuple<PieceName, Position>(pieceName, position));
                numPieces++;
            }

            // Add piece being placed on the board
            if (!targetPieceInPlay && targetPosition.HasValue)
            {
                int stack = targetPosition.Value.Stack;
                maxStack = Math.Max(maxStack, stack);

                if (!pieces.ContainsKey(stack))
                {
                    pieces[stack] = new List<Tuple<PieceName, Position>>();
                }

                pieces[stack].Add(new Tuple<PieceName, Position>(targetPieceName, targetPosition.Value));
                numPieces++;
            }

            return pieces;
        }

        private Shape GetHex(Point center, double size, HexType hexType, HexOrientation hexOrientation)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            double strokeThickness = size / 10;

            var hex = new HexShape()
            {
                StrokeThickness = strokeThickness,
                HexSize = size,
                HexOrientation = hexOrientation,
            };

            switch (hexType)
            {
                case HexType.WhitePiece:
                    hex.Fill = WhiteBrush;
                    hex.Stroke = PieceOutlineBrush;
                    break;
                case HexType.BlackPiece:
                    hex.Fill = BlackBrush;
                    hex.Stroke = PieceOutlineBrush;
                    break;
                case HexType.ValidMove:
                    hex.Fill = SelectedMoveBodyBrush;
                    hex.Stroke = SelectedMoveBodyBrush;
                    break;
                case HexType.SelectedPiece:
                    hex.Stroke = SelectedMoveEdgeBrush;
                    break;
                case HexType.SelectedMove:
                    hex.Fill = SelectedMoveBodyBrush;
                    hex.Stroke = SelectedMoveEdgeBrush;
                    break;
                case HexType.LastMove:
                    hex.Stroke = LastMoveEdgeBrush;
                    break;
            }

            Canvas.SetLeft(hex, center.X - size);
            Canvas.SetTop(hex, center.Y - size);

            return hex;
        }

        private Border GetPieceText(Point center, double size, PieceName pieceName, bool disabled)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            SolidColorBrush bugBrush = MainViewModel.ViewerConfig.PieceColors ? BugBrushes[(int)Enums.GetBugType(pieceName)] : (Enums.GetColor(pieceName) == PlayerColor.White ? BlackBrush : WhiteBrush);

            // Create text
            string text = pieceName.ToString().Substring(1);
            TextBlock bugText = new TextBlock
            {
                Text = MainViewModel.ViewerConfig.AddPieceNumbers ? text : text.TrimEnd('1', '2', '3'),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Arial Black"),
                FontSize = size * 0.75,
                Foreground = disabled ? MixSolidColorBrushes(bugBrush, DisabledPieceBrush) : bugBrush,
            };

            Canvas.SetLeft(bugText, center.X - (bugText.Text.Length * (bugText.FontSize / 3.0)));
            Canvas.SetTop(bugText, center.Y - (bugText.FontSize / 2.0));

            Border b = new Border
            {
                Height = size * 2.0,
                Width = size * 2.0,
                Child = bugText
            };

            Canvas.SetLeft(b, center.X - (b.Width / 2.0));
            Canvas.SetTop(b, center.Y - (b.Height / 2.0));

            return b;
        }

        private Border GetPieceGraphics(Point center, double size, PieceName pieceName, bool disabled)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            SolidColorBrush bugBrush = MainViewModel.ViewerConfig.PieceColors ? BugBrushes[(int)Enums.GetBugType(pieceName)] : (Enums.GetColor(pieceName) == PlayerColor.White ? BlackBrush : WhiteBrush);

            // Create bug
            var bugShape = new BugShape()
            {
                BugType = Enums.GetBugType(pieceName),
                Stretch = Stretch.Uniform,
                Fill = disabled ? MixSolidColorBrushes(bugBrush, DisabledPieceBrush) : bugBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            Grid safeGrid = new Grid() { Height = size * 2.0, Width = size * 2.0 };

            Grid bugGrid = new Grid() { Height = size * 2.0 * Math.Sin(Math.PI / 6) * GraphicalBugSizeRatio, Width = size * 2.0 * Math.Sin(Math.PI / 6) * GraphicalBugSizeRatio };
            bugGrid.Children.Add(bugShape);

            safeGrid.Children.Add(bugGrid);

            // Bug rotation
            double rotateAngle = MainViewModel.ViewerConfig.HexOrientation == HexOrientation.PointyTop ? -90.0 : 0.0;

            if (int.TryParse(pieceName.ToString().Last().ToString(), out int bugNum))
            {
                rotateAngle += (bugNum - 1) * 60.0;

                if (MainViewModel.ViewerConfig.AddPieceNumbers)
                {
                    // Add bug number
                    TextBlock bugText = new TextBlock
                    {
                        Text = bugNum.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontFamily = new FontFamily("Arial Black"),
                        FontSize = size * 0.5,
                        Foreground = Enums.GetColor(pieceName) == PlayerColor.White ? WhiteBrush : BlackBrush,
                    };

                    Ellipse bugTextEllipse = new Ellipse()
                    {
                        HorizontalAlignment = bugText.HorizontalAlignment,
                        VerticalAlignment = bugText.VerticalAlignment,
                        Width = bugText.FontSize,
                        Height = bugText.FontSize,
                        Fill = bugShape.Fill,
                    };

                    safeGrid.Children.Add(bugTextEllipse);
                    safeGrid.Children.Add(bugText);
                }
            }

            bugGrid.RenderTransform = new RotateTransform(rotateAngle);
            bugGrid.RenderTransformOrigin = RelativePoint.Center;

            Border b = new Border
            {
                Height = size * 2.0,
                Width = size * 2.0,
                Child = safeGrid
            };

            Canvas.SetLeft(b, center.X - (b.Width / 2.0));
            Canvas.SetTop(b, center.Y - (b.Height / 2.0));

            return b;
        }

        private static SolidColorBrush MixSolidColorBrushes(SolidColorBrush b1, SolidColorBrush b2)
        {
            SolidColorBrush result = new SolidColorBrush
            {
                Color = Color.FromArgb((byte)((b1.Color.A + b2.Color.A) / 2),
                                       (byte)((b1.Color.R + b2.Color.R) / 2),
                                       (byte)((b1.Color.G + b2.Color.G) / 2),
                                       (byte)((b1.Color.B + b2.Color.B) / 2))
            };
            return result;
        }

        private enum HexType
        {
            WhitePiece,
            BlackPiece,
            ValidMove,
            SelectedPiece,
            SelectedMove,
            LastMove,
        }

        private void DrawHand(StackPanel handPanel, Dictionary<BugType, Stack<Canvas>> pieceCanvases)
        {
            for (int bt = 0; bt < (int)BugType.NumBugTypes; bt++)
            {
                var bugType = (BugType)bt;
                if (pieceCanvases.ContainsKey(bugType))
                {
                    if (MainViewModel.ViewerConfig.StackPiecesInHand)
                    {
                        int startingCount = pieceCanvases[bugType].Count;

                        Canvas bugStack = new Canvas()
                        {
                            Height = pieceCanvases[bugType].Peek().Height * (1 + startingCount * BaseStackShiftLevel),
                            Width = pieceCanvases[bugType].Peek().Width * (1 + startingCount * BaseStackShiftLevel),
                            Margin = new Thickness(PieceCanvasMargin),
                            Background = new SolidColorBrush(Colors.Transparent),
                        };

                        while (pieceCanvases[bugType].Count > 0)
                        {
                            Canvas pieceCanvas = pieceCanvases[bugType].Pop();
                            Canvas.SetTop(pieceCanvas, pieceCanvas.Height * ((startingCount - pieceCanvases[bugType].Count - 1) * BaseStackShiftLevel));
                            Canvas.SetLeft(pieceCanvas, pieceCanvas.Width * ((startingCount - pieceCanvases[bugType].Count - 1) * BaseStackShiftLevel));
                            bugStack.Children.Add(pieceCanvas);
                        }

                        handPanel.Children.Add(bugStack);
                    }
                    else
                    {
                        foreach (Canvas pieceCanvas in pieceCanvases[bugType].Reverse())
                        {
                            handPanel.Children.Add(pieceCanvas);
                        }
                    }
                }
            }
        }

        private Canvas GetPieceInHandCanvas(PieceName pieceName, double size, HexOrientation hexOrientation, bool disabled)
        {
            Point center = new Point(size, size);

            HexType hexType = (Enums.GetColor(pieceName) == PlayerColor.White) ? HexType.WhitePiece : HexType.BlackPiece;

            Shape hex = GetHex(center, size, hexType, hexOrientation);
            var hexText = MainViewModel.ViewerConfig.PieceStyle == PieceStyle.Text ? GetPieceText(center, size, pieceName, disabled) : GetPieceGraphics(center, size, pieceName, disabled);

            Canvas pieceCanvas = new Canvas
            {
                Height = size * 2,
                Width = size * 2,
                Margin = new Thickness(PieceCanvasMargin),
                Background = new SolidColorBrush(Colors.Transparent),
                Name = pieceName.ToString()
            };

            pieceCanvas.Children.Add(hex);
            pieceCanvas.Children.Add(hexText);

            // Add highlight if the piece is selected
            if (MainViewModel.AppVM.EngineWrapper.TargetPiece == pieceName)
            {
                Shape highlightHex = GetHex(center, size, HexType.SelectedPiece, hexOrientation);
                pieceCanvas.Children.Add(highlightHex);
            }

            pieceCanvas.PointerReleased += PieceCanvas_Click;

            return pieceCanvas;
        }

        private void PieceCanvas_Click(object sender, PointerReleasedEventArgs e)
        {
            if (sender is Canvas pieceCanvas)
            {
                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    PieceName clickedPiece = Enum.Parse<PieceName>(pieceCanvas.Name);
                    MainViewModel.TryPieceClick(clickedPiece);
                    e.Handled = true;
                }
            }
        }

        private void BoardCanvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(BoardCanvas);
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                DragStartPoint = pointerPoint.Position;
                DragStartPieceName = MainViewModel.AppVM.EngineWrapper.GetPieceAt(pointerPoint.Position.X - CanvasOffsetX, pointerPoint.Position.Y - CanvasOffsetY, BoardPieceSize, MainViewModel.ViewerConfig.HexOrientation);
                DragStartCanvasOffsetX = CanvasOffsetX;
                DragStartCanvasOffsetY = CanvasOffsetY;
                e.Handled = true;
            }
            else
            {
                DragStartPoint = null;
                DragStartPieceName = PieceName.INVALID;
            }
            MoveAfterDragStart = false;
        }

        private void BoardCanvas_PointerMoved(object sender, PointerEventArgs e)
        {
            if (DragStartPoint is not null)
            {
                var pointerPoint = e.GetCurrentPoint(BoardCanvas);
                if (pointerPoint.Properties.IsLeftButtonPressed)
                {
                    var dX = pointerPoint.Position.X - DragStartPoint.Value.X;
                    var dY = pointerPoint.Position.X - DragStartPoint.Value.X;

                    if (MoveAfterDragStart || Math.Sqrt(dX * dX + dY * dY) >= MinDragDistanceToStartPan)
                    {
                        if (!VM.AutoCenterBoard && DragStartPieceName != PieceName.INVALID)
                        {
                            CanvasOffsetX = DragStartCanvasOffsetX + (pointerPoint.Position.X - DragStartPoint.Value.X);
                            CanvasOffsetY = DragStartCanvasOffsetY + (pointerPoint.Position.Y - DragStartPoint.Value.Y);

                            TranslateBoardChildren();
                        }

                        MoveAfterDragStart = true;
                        e.Handled = true;
                    }
                }
            }
        }

        private void BoardCanvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                Point point = e.GetPosition(BoardCanvas);
                if (!MoveAfterDragStart && VM.IsIdle)
                {
                    VM.CanvasClick(point.X - CanvasOffsetX, point.Y - CanvasOffsetY);
                    e.Handled = true;
                }
            }
            DragStartPoint = null;
            DragStartPieceName = PieceName.INVALID;
            MoveAfterDragStart = false;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            MainViewModel.TryCancelClick();
        }

        private DateTime LastRedrawOnSizeChange = DateTime.Now;

        private void BoardCanvas_SizeChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Canvas.BoundsProperty)
            {
                TryRedraw();
            }
        }

        private void BoardCanvas_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (!VM.AutoZoomBoard)
            {
                if (e.Delta.Y > 0)
                {
                    if (TryIncreaseZoom())
                    {
                        
                        TryRedraw();
                    }
                }
                else if (e.Delta.Y < 0)
                {
                    if (TryDecreaseZoom())
                    {
                        TryRedraw();
                    }
                }
            }
        }
    }
}
