﻿// Spines.Utility.EnumerableExtensions.cs
// 
// Copyright (C) 2016  Johannes Heckl
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Spines.Utility
{
  /// <summary>
  /// Extension Methods foer IEnumerable.
  /// </summary>
  public static class EnumerableExtensions
  {
    /// <summary>
    /// Returns a sequence containing value as the only element.
    /// </summary>
    public static IEnumerable<T> Yield<T>(this T value)
    {
      yield return value;
    }

    /// <summary>
    /// Generates all permutations of the input sequence.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public static IEnumerable<IEnumerable<T>> Permute<T>(this IEnumerable<T> sequence)
    {
      var list = sequence.ToList();
      var length = list.Count;
      var indices = new int[length];

      yield return list.ToList();

      for (var i = 1; i < length; /**/)
      {
        var index = indices[i];
        if (index < i)
        {
          if (i % 2 == 1)
          {
            var temp = list[i];
            list[i] = list[index];
            list[index] = temp;
          }
          else
          {
            var temp = list[i];
            list[i] = list[0];
            list[0] = temp;
          }

          yield return list.ToList();

          indices[i] += 1;
          i = 1;
        }
        else
        {
          indices[i] = 0;
          i += 1;
        }
      }
    }

    /// <summary>
    /// Generates the cartesian product for a sequence of sequences.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
      // Start with a single empty sequence as the result of the cartesian product of 0 sequences.
      return sequences.Aggregate(Enumerable.Empty<T>().Yield(), AccumulateCartesian);
    }

    /// <summary>
    /// Appends a new sequence to the current accumulator for a cartesian product of multiple sequences.
    /// </summary>
    private static IEnumerable<IEnumerable<T>> AccumulateCartesian<T>(IEnumerable<IEnumerable<T>> accumulator,
      IEnumerable<T> sequence)
    {
      var list = sequence.ToList();
      // For each IEnumerable in the current accumulator, iterates over the next sequence.
      // The second parameter of SelectMany is called for each element in the CartesianProduct of the current accumulator and the new sequence.
      // Each of these calls returns a new element for the new accumulator.
      return accumulator.SelectMany(accumulatorSequence => list,
        (accumulatorSequence, item) => accumulatorSequence.Concat(item.Yield()));
    }
  }
}