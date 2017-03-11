﻿// Spines.Mahjong.Analysis.ArrangementGroupCompacter.cs
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
using System.Linq;

namespace Spines.Tools.AnalyzerBuilder.Combinations
{
  /// <summary>
  /// Removes from a list of arrangements all redundant arrangements.
  /// </summary>
  internal class ArrangementGroupCompacter
  {
    /// <summary>
    /// If a list of arrangements matches one of these sequences by their Ids, one of these arrangements is redundant
    /// in this combination. The value in the dictionary is the Id of the redundant arrangement in the sequence.
    /// This list is the result of testing all arrangement combinations that can appear in a suit or honors.
    /// If one arrangement could be left out without changing the shanten value in any combination,
    /// it was considered redundant.
    /// </summary>
    private static readonly Dictionary<string, int> RedundantIndices = new Dictionary<string, int>
    {
      {"7,12,28,50", 3},
      {"7,12,28,50,56", 1},
      {"7,12,53,56", 1},
      {"7,28,50,56", 1},
      {"7,50,56", 1},
      {"8,13,20,53", 3},
      {"8,13,20,53,57,62", 1},
      {"8,13,28", 2},
      {"8,13,53", 2},
      {"8,13,53,57", 1},
      {"8,14,53,57", 0},
      {"8,20,38,53,57", 1},
      {"8,20,53,57,62", 1},
      {"8,25,28", 1},
      {"8,28", 1},
      {"8,28,50", 1},
      {"8,38,53,57", 2},
      {"8,50", 1},
      {"8,53,57", 1},
      {"8,53,57,62", 1},
      {"9,25,26,29", 0},
      {"10,25,27,30", 0},
      {"10,27,30,50", 0},
      {"11,25,27,31", 0},
      {"11,25,28,31", 0},
      {"11,27,31,50", 0},
      {"11,28,31", 0},
      {"11,28,31,50", 0},
      {"11,31,50,52", 0},
      {"12,25,27,31", 1},
      {"12,25,28,32", 0},
      {"12,27,31", 1},
      {"12,28,32", 0},
      {"12,28,32,50", 0},
      {"12,28,50,56", 0},
      {"12,31,50,52", 2},
      {"12,32,53", 0},
      {"12,50,52,56", 0},
      {"13,20,28,32", 2},
      {"13,20,28,32,50", 2},
      {"13,20,28,32,50,62", 1},
      {"13,20,32,50", 3},
      {"13,20,32,53,62", 1},
      {"13,20,53,57,62", 0},
      {"13,20,58", 0},
      {"13,25,28,32", 1},
      {"13,25,28,33", 0},
      {"13,28,32", 1},
      {"13,28,32,50", 1},
      {"13,28,32,50,62", 1},
      {"13,28,33", 0},
      {"13,28,33,50", 0},
      {"13,32,50", 2},
      {"13,32,50,62", 2},
      {"13,33", 0},
      {"13,33,53", 0},
      {"13,53,57", 0},
      {"14,22,58,63", 0},
      {"14,21,28,33", 2},
      {"14,21,33", 2},
      {"14,21,33,53", 2},
      {"14,21,33,53,63", 1},
      {"14,21,33,63,70", 1},
      {"14,21,53", 2},
      {"14,21,58", 0},
      {"14,21,58,63", 0},
      {"14,21,58,63,70", 0},
      {"14,28,33", 1},
      {"14,28,33,50", 1},
      {"14,33,46,63", 1},
      {"14,33,50", 2},
      {"14,33,53", 2},
      {"14,33,53,63", 1},
      {"14,33,63,70", 1},
      {"14,46,58,63", 0},
      {"14,53,57", 1},
      {"14,53,63", 1},
      {"14,58", 0},
      {"15,22,33", 2},
      {"15,22,58", 2},
      {"15,22,58,64", 1},
      {"15,22,58,64,71", 0},
      {"15,22,64,71", 0},
      {"15,23,58", 2},
      {"15,23,58,64", 0},
      {"15,23,64", 0},
      {"15,28,33", 1},
      {"15,33", 1},
      {"15,33,38", 1},
      {"15,33,53", 1},
      {"15,38", 1},
      {"15,47,58", 2},
      {"15,47,58,64", 0},
      {"15,47,64", 0},
      {"15,53", 1},
      {"15,58", 1},
      {"15,58,64", 1},
      {"22,33,39", 1},
      {"22,33,40", 0},
      {"22,39,58,71", 2},
      {"22,40", 0},
      {"22,40,58", 0},
      {"22,40,58,71", 0},
      {"22,58,64", 0},
      {"22,58,64,71", 0},
      {"22,64,71", 0},
      {"22,65", 0},
      {"23,40,58", 2},
      {"23,40,72", 1},
      {"23,58,64", 1},
      {"23,64,72", 1},
      {"23,65", 0},
      {"23,65,72", 0},
      {"24,40", 1},
      {"24,64", 1},
      {"24,65", 1},
      {"16,25,26,29,34", 0},
      {"17,27,30,35,50", 0},
      {"18,28,31,36", 0},
      {"18,28,31,36,50", 0},
      {"18,31,36,50,52", 0},
      {"19,28,32,37", 0},
      {"19,28,32,37,50", 0},
      {"19,31,37,50,52", 0},
      {"19,32,37,53", 0},
      {"19,37,50,52,56", 0},
      {"20,28,32,38", 0},
      {"20,28,32,38,50", 0},
      {"20,28,33,38", 0},
      {"20,28,33,38,50", 0},
      {"20,32,38,53", 0},
      {"20,32,53,62", 0},
      {"20,33,38", 0},
      {"20,33,38,53", 0},
      {"20,37,50,52,56", 2},
      {"20,37,52,56", 2},
      {"20,38,53,57", 0},
      {"20,38,58", 0},
      {"20,50,52,56,62", 0},
      {"20,53,57,62", 0},
      {"20,58", 0},
      {"21,28,32,38", 1},
      {"21,28,33,39", 0},
      {"21,32,38", 1},
      {"21,33,38,53", 3},
      {"21,33,39", 0},
      {"21,33,39,53", 0},
      {"21,33,53,63", 0},
      {"21,38,53,57", 2},
      {"21,39,58", 0},
      {"21,53,57,63", 0},
      {"21,57,63,70", 0},
      {"21,58,63", 0},
      {"21,58,63,70", 0},
      {"21,58,64", 0},
      {"25,28,32", 0},
      {"25,28,33", 0},
      {"28,31,36,43,50", 4},
      {"28,31,36,50", 3},
      {"28,32", 0},
      {"28,32,37", 0},
      {"28,32,37,44", 0},
      {"28,32,37,50", 0},
      {"28,32,38", 0},
      {"28,32,38,45", 0},
      {"28,32,38,50", 0},
      {"28,32,50", 0},
      {"28,33", 0},
      {"28,33,38", 0},
      {"28,33,38,45", 0},
      {"28,33,38,50", 0},
      {"28,33,39", 0},
      {"28,33,50", 0},
      {"28,50,56", 1},
      {"31,36,43,50,52", 3},
      {"31,36,50,52", 2},
      {"31,37,50,52", 2},
      {"32,37,50", 2},
      {"32,38,45", 0},
      {"32,38,45,53", 0},
      {"32,38,50", 2},
      {"32,50", 1},
      {"33,38,45,53", 3},
      {"33,38,50", 2},
      {"33,38,53", 2},
      {"33,39", 0},
      {"33,39,46", 0},
      {"33,39,53", 0},
      {"33,40", 0},
      {"33,46,63", 0},
      {"33,50", 1},
      {"33,53", 1},
      {"33,53,63", 0},
      {"37,44,50,52,56", 2},
      {"37,44,52,56", 2},
      {"37,50,52,56", 1},
      {"37,52,56", 1},
      {"38,45,53", 2},
      {"38,45,53,57", 2},
      {"38,53,57", 1},
      {"38,58", 0},
      {"39,47,58", 2},
      {"39,53", 1},
      {"39,58,71", 1},
      {"40,47,58", 2},
      {"40,48", 0},
      {"40,58", 1},
      {"40,58,71", 1},
      {"40,72", 0},
      {"47,58,64", 1},
      {"47,65", 0},
      {"45,52,56,62", 1},
      {"45,56,62", 1},
      {"45,58,63", 0},
      {"46,57,63", 1},
      {"46,58,64", 0},
      {"50,52,56", 0},
      {"50,52,56,62", 0},
      {"52,56,62", 0},
      {"53,57", 0},
      {"53,57,62", 0},
      {"53,57,63", 0},
      {"53,63", 0},
      {"56,62,70", 0},
      {"57,63", 0},
      {"57,63,70", 0},
      {"58,64", 0},
      {"58,64,71", 0},
      {"64,72", 0}
    };

    /// <summary>
    /// Returns the non-redundant arrangements.
    /// </summary>
    public IEnumerable<Arrangement> GetCompacted(IEnumerable<Arrangement> arrangements)
    {
      var list = arrangements.ToList();
      var key = string.Join(",", list.Select(a => a.Id));
      if (!RedundantIndices.ContainsKey(key))
      {
        return list;
      }
      var toRemove = RedundantIndices[key];
      return GetCompacted(list.Where((a, i) => i != toRemove));
    }
  }
}