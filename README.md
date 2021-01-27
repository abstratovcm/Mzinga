# Mzinga #

Mzinga is an open-source software project to play the board game [Hive](https://gen42.com/games/hive), with the primary goal of building a community of developers who create Hive-playing AIs.

To that end, Mzinga proposes a [Universal Hive Protocol](https://github.com/jonthysell/Mzinga/wiki/UniversalHiveProtocol) to support interoperability for Hive-playing software.

For more information, please check out the [Mzinga Wiki](https://github.com/jonthysell/Mzinga/wiki).

## Projects ##

Mzinga is developed in C# for .NET Core 3.1.

### Mzinga.Engine ###

Mzinga.Engine.exe is Mzinga's engine, a command-line application through which you can play a game of Hive. It accepts input commands and outputs results according to the specifications of the Universal Hive Protocol.

### Mzinga.Viewer ###

Mzinga.Viewer.exe is Mzinga's viewer, a graphical application which can drive Mzinga.Engine (or any engine that implements the specifications of the Universal Hive Protocol).

Mzinga.Viewer is not meant to be graphically impressive or compete with commercial versions of Hive, but rather be a ready-made UI for developers who'd rather focus their time on building a compatible engine and AI.

## Other Components ##

### Mzinga.Test ###

Mzinga.Test.dll contains unit tests for Mzinga.

### Mzinga.Trainer ###

Mzinga.Trainer.exe is a command-line utility with the goal to improve Mzinga's AI. Through it you can generate randomized AI profiles and execute AI vs. AI battles.

## Copyright ##

Hive Copyright (c) 2016 Gen42 Games. Mzinga is in no way associated with or endorsed by Gen42 Games.

Mzinga Copyright (c) 2015-2021 Jon Thysell.

MVVM Light Toolkit Copyright (c) 2009-2018 Laurent Bugnion.

Extended WPF Toolkit Copyright (c) 2010-2019 Xceed Software Inc.
