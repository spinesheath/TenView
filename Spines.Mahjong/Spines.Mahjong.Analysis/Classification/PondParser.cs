﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Spines.Mahjong.Analysis.Classification
{
  /// <summary>
  /// Parses a string that describes a pond.
  /// </summary>
  public static class PondParser
  {
    /// <summary>
    /// Parses a string that describes a pond.
    /// </summary>
    /// <param name="shorthand">The string describing the pond.</param>
    /// <returns>The Tiles in the pond.</returns>
    public static IEnumerable<Tile> Parse(string shorthand)
    {
      var matches = TileRegex.Matches(shorthand).OfType<Match>();
      var groups = matches.SelectMany(m => m.Groups.OfType<Group>().Skip(1));
      var blocks = groups.Select(g => g.Value);
      var tiles = new List<Tile>();
      foreach (var block in blocks)
      {
        tiles.Add(CreateTile(block));
      }

      var bySuit = tiles.GroupBy(t => t.Suit).ToList();
      if (bySuit.Any(g => g.GroupBy(t => t.Index).Any(gg => gg.Count() > 4)))
      {
        throw new FormatException("Too many of the same tile type.");
      }
      if (bySuit.Any(g => g.Count(t => t.Aka) > 1))
      {
        throw new FormatException("Too many aka dora.");
      }
      if (tiles.Count(t => t.Location == TileLocation.Riichi) > 2)
      {
        throw new FormatException("Multiple riichi tiles.");
      }

      return tiles;
    }

    private static readonly Regex TileRegex = new Regex("([0-9][mpsMPS]R?[ABCD]?|[1-7][zZ]R?[ABCD]?)");

    private static readonly Dictionary<char, Suit> CharToSuit = new Dictionary<char, Suit>
    {
      {'m', Suit.Manzu},
      {'p', Suit.Pinzu},
      {'s', Suit.Souzu},
      {'z', Suit.Jihai},
      {'M', Suit.Manzu},
      {'P', Suit.Pinzu},
      {'S', Suit.Souzu},
      {'Z', Suit.Jihai}
    };

    private static Tile CreateTile(string block)
    {
      var suit = CharToSuit[block[1]];
      var riichi = block.Length > 2 && block[2] == 'R';
      var ghost = block.Length > 2 && block.Last() != 'R';
      var calledBy = ghost ? block.Last() - 'A' : 0;
      var tsumokiri = char.IsUpper(block[1]);
      var location = riichi ? TileLocation.Riichi : TileLocation.Discarded;
      var numericValue = (int) char.GetNumericValue(block[0]);
      var aka = numericValue == 0;
      var index = aka ? 4 : numericValue - 1;
      return new Tile {Aka = aka, Index = index, Location = location, Suit = suit, IsGhost = ghost, CalledBy = calledBy, IsTsumokiri = tsumokiri};
    }
  }
}