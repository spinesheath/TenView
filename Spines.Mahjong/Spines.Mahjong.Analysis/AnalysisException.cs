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

namespace Spines.Mahjong.Analysis
{
  /// <summary>
  /// General Analysis Exception.
  /// </summary>
  [Serializable]
  public class AnalysisException : Exception
  {
    /// <summary>
    /// Creates a new Instance of AnalysisException.
    /// </summary>
    public AnalysisException()
    {
    }

    /// <summary>
    /// Creates a new Instance of AnalysisException with a message.
    /// </summary>
    public AnalysisException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new Instance of AnalysisException with a message and inner Exception.
    /// </summary>
    public AnalysisException(string message, Exception inner)
      : base(message, inner)
    {
    }

    /// <summary>
    /// Creates a new Instance of AnalysisException from a SerializationInfo and StreamingContext.
    /// </summary>
    protected AnalysisException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}