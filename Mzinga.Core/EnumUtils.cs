﻿// 
// EnumUtils.cs
//  
// Author:
//       Jon Thysell <thysell@gmail.com>
// 
// Copyright (c) 2015 Jon Thysell <http://jonthysell.com>
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

using System;
using System.Collections.Generic;

namespace Mzinga.Core
{
    public class EnumUtils
    {
        #region Directions

        public static IEnumerable<Direction> Directions
        {
            get
            {
                for (int i = 0; i < NumDirections; i++)
                {
                    yield return (Direction)i;
                }
            }
        }

        public static Direction LeftOf(Direction direction)
        {
            return (Direction)(((int)direction + NumDirections - 1) % NumDirections);
        }

        public static Direction RightOf(Direction direction)
        {
            return (Direction)(((int)direction + 1) % NumDirections);
        }

        public const int NumDirections = 6;

        #endregion

        #region PieceNames

        public static IEnumerable<PieceName> PieceNames
        {
            get
            {
                for (int i = 0; i < NumPieceNames; i++)
                {
                    yield return (PieceName)i;
                }
            }
        }

        public static string GetShortName(PieceName pieceName)
        {
            if (pieceName == PieceName.INVALID)
            {
                return "";
            }

            return PieceShortName[(int)pieceName];
        }

        public static PieceName ParseShortName(string nameString)
        {
            if (String.IsNullOrWhiteSpace(nameString))
            {
                throw new ArgumentNullException("nameString");
            }

            nameString = nameString.Trim();

            for (int i = 0; i < PieceShortName.Length; i++)
            {
                if (PieceShortName[i].Equals(nameString, StringComparison.CurrentCultureIgnoreCase))
                {
                    return (PieceName)i;
                }
            }

            throw new ArgumentOutOfRangeException("nameString");
        }

        public static string[] PieceShortName = new string[]
        {
            "WQ",
            "WS1",
            "WS2",
            "WB1",
            "WB2",
            "WG1",
            "WG2",
            "WG3",
            "WA1",
            "WA2",
            "WA3",
            "BQ",
            "BS1",
            "BS2",
            "BB1",
            "BB2",
            "BG1",
            "BG2",
            "BG3",
            "BA1",
            "BA2",
            "BA3"
        };

        public const int NumPieceNames = 22;

        #endregion

        #region Colors

        public static Color GetColor(PieceName pieceName)
        {
            switch (pieceName)
            {
                case PieceName.WhiteQueenBee:
                case PieceName.WhiteSpider1:
                case PieceName.WhiteSpider2:
                case PieceName.WhiteBeetle1:
                case PieceName.WhiteBeetle2:
                case PieceName.WhiteGrasshopper1:
                case PieceName.WhiteGrasshopper2:
                case PieceName.WhiteGrassHopper3:
                case PieceName.WhiteSoldierAnt1:
                case PieceName.WhiteSoldierAnt2:
                case PieceName.WhiteSoldierAnt3:
                    return Color.White;
                case PieceName.BlackQueenBee:
                case PieceName.BlackSpider1:
                case PieceName.BlackSpider2:
                case PieceName.BlackBeetle1:
                case PieceName.BlackBeetle2:
                case PieceName.BlackGrasshopper1:
                case PieceName.BlackGrasshopper2:
                case PieceName.BlackGrassHopper3:
                case PieceName.BlackSoldierAnt1:
                case PieceName.BlackSoldierAnt2:
                case PieceName.BlackSoldierAnt3:
                    return Color.Black;
            }

            throw new ArgumentOutOfRangeException("pieceName");
        }

        #endregion

        #region BugTypes

        public static BugType GetBugType(PieceName pieceName)
        {
            switch (pieceName)
            {
                case PieceName.WhiteQueenBee:
                case PieceName.BlackQueenBee:
                    return BugType.QueenBee;
                case PieceName.WhiteSpider1:
                case PieceName.WhiteSpider2:
                case PieceName.BlackSpider1:
                case PieceName.BlackSpider2:
                    return BugType.Spider;
                case PieceName.WhiteBeetle1:
                case PieceName.WhiteBeetle2:
                case PieceName.BlackBeetle1:
                case PieceName.BlackBeetle2:
                    return BugType.Beetle;
                case PieceName.WhiteGrasshopper1:
                case PieceName.WhiteGrasshopper2:
                case PieceName.WhiteGrassHopper3:
                case PieceName.BlackGrasshopper1:
                case PieceName.BlackGrasshopper2:
                case PieceName.BlackGrassHopper3:
                    return BugType.Grasshopper;
                case PieceName.WhiteSoldierAnt1:
                case PieceName.WhiteSoldierAnt2:
                case PieceName.WhiteSoldierAnt3:
                case PieceName.BlackSoldierAnt1:
                case PieceName.BlackSoldierAnt2:
                case PieceName.BlackSoldierAnt3:
                    return BugType.SoldierAnt;
            }

            throw new ArgumentOutOfRangeException("pieceName");
        }

        #endregion
    }

    public enum Direction
    {
        Up = 0,
        UpRight,
        DownRight,
        Down,
        DownLeft,
        UpLeft
    }

    public enum Color
    {
        White = 0,
        Black
    }

    public enum BugType
    {
        QueenBee = 0,
        Spider,
        Beetle,
        Grasshopper,
        SoldierAnt
    }

    public enum PieceName
    {
        INVALID = -1,
        WhiteQueenBee = 0,
        WhiteSpider1,
        WhiteSpider2,
        WhiteBeetle1,
        WhiteBeetle2,
        WhiteGrasshopper1,
        WhiteGrasshopper2,
        WhiteGrassHopper3,
        WhiteSoldierAnt1,
        WhiteSoldierAnt2,
        WhiteSoldierAnt3,
        BlackQueenBee,
        BlackSpider1,
        BlackSpider2,
        BlackBeetle1,
        BlackBeetle2,
        BlackGrasshopper1,
        BlackGrasshopper2,
        BlackGrassHopper3,
        BlackSoldierAnt1,
        BlackSoldierAnt2,
        BlackSoldierAnt3
    }
}
