﻿// 
// BoardMetrics.cs
//  
// Author:
//       Jon Thysell <thysell@gmail.com>
// 
// Copyright (c) 2016, 2017, 2018 Jon Thysell <http://jonthysell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace Mzinga.Core
{
    public class BoardMetrics
    {
        public BoardState BoardState;

        public int PiecesInPlay = 0;
        public int PiecesInHand = 0;

        public PieceMetrics this[PieceName pieceName]
        {
            get
            {
                return _pieceMetrics[(int)pieceName];
            }
        }

        private PieceMetrics[] _pieceMetrics = new PieceMetrics[EnumUtils.NumPieceNames];

        public BoardMetrics()
        {
            for (int i = 0; i < _pieceMetrics.Length; i++)
            {
                _pieceMetrics[i] = new PieceMetrics();
            }
        }

        public void Reset()
        {
            BoardState = BoardState.NotStarted;
            PiecesInPlay = 0;
            PiecesInHand = 0;

            for (int i = 0; i < _pieceMetrics.Length; i++)
            {
                _pieceMetrics[i].InPlay = 0;
                _pieceMetrics[i].IsPinned = 0;
                _pieceMetrics[i].IsCovered = 0;
                _pieceMetrics[i].NoisyMoveCount = 0;
                _pieceMetrics[i].QuietMoveCount = 0;
                _pieceMetrics[i].FriendlyNeighborCount = 0;
                _pieceMetrics[i].EnemyNeighborCount = 0;
            }
        }
    }
}
