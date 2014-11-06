﻿// Spines.Mahjong.Analysis.AnalysisException.cs
// 
// Copyright (C) 2014  Johannes Heckl
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Runtime.Serialization;

#pragma warning disable 1591

namespace Spines.Mahjong.Analysis
{
  /// <summary>
  /// General Analysis Exception.
  /// </summary>
  [Serializable]
  public class AnalysisException : Exception
  {
    public AnalysisException()
    {
    }

    public AnalysisException(string message) : base(message)
    {
    }

    public AnalysisException(string message, Exception inner)
      : base(message, inner)
    {
    }

    protected AnalysisException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}