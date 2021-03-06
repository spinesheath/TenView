﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Spines.Hana.Blame.Services.ReplayManager
{
  internal class MeldDecoder
  {
    public MeldDecoder(string meldCodeString)
    {
      _meldCode = int.Parse(meldCodeString);
      _baseIndex = 0;
      _koutsuUnusedTileNumber = 0;

      MeldType = GetMeldType();
      Decode();
    }

    public MeldType MeldType { get; }
    public int[] Tiles { get; private set; }

    private readonly int _meldCode;
    private int _baseIndex;
    private int _koutsuUnusedTileNumber;

    // A koutsu only uses 3 out of 4 tiles, depending on a value in the meld code one of these configurations is selected.
    // The index of the unused tile is the index of the configuration.
    private static readonly int[,] KoutsuTileOffsets = {{1, 2, 3}, {0, 2, 3}, {0, 1, 3}, {0, 1, 2}};

    private void Decode()
    {
      if (MeldType == MeldType.Shuntsu)
      {
        var t = IntFromBits(_meldCode, 6, 10);
        t = t / 3;
        t = t / 7 * 9 + t % 7;
        _baseIndex = t * 4;
      }
      else if (MeldType == MeldType.AddedKan || MeldType == MeldType.Koutsu)
      {
        var t = IntFromBits(_meldCode, 7, 9);
        t = t / 3;
        _baseIndex = t * 4;
        _koutsuUnusedTileNumber = IntFromBits(_meldCode, 2, 5);
      }
      else if (MeldType == MeldType.CalledKan || MeldType == MeldType.ClosedKan)
      {
        var hai0 = IntFromBits(_meldCode, 8, 8);
        _baseIndex = hai0 & ~3;
      }

      CalculateTiles();
    }

    private void CalculateTiles()
    {
      // Get the number of tiles in the meld
      var length = 4;
      if (MeldType == MeldType.Shuntsu || MeldType == MeldType.Koutsu)
      {
        length = 3;
      }
      var ids = GetTileIds(length);
      // finally create the MeldTiles
      Tiles = new int[length];
      for (var i = 0; i < length; ++i)
      {
        Tiles[i] = ids[i];
      }
    }

    private int[] GetTileIds(int length)
    {
      var ids = new int[length];
      for (var i = 0; i < length; ++i)
      {
        if (MeldType == MeldType.Shuntsu)
        {
          ids[i] = _baseIndex + 4 * i + IntFromBits(_meldCode, 2, 3 + 2 * i);
        }
        else if (MeldType == MeldType.Koutsu)
        {
          ids[i] = _baseIndex + KoutsuTileOffsets[_koutsuUnusedTileNumber, i];
        }
        else
        {
          ids[i] = _baseIndex + i;
        }
      }
      return ids;
    }

    private MeldType GetMeldType()
    {
      if ((_meldCode & 1 << 2) != 0)
      {
        return MeldType.Shuntsu;
      }
      if ((_meldCode & 1 << 3) != 0)
      {
        return MeldType.Koutsu;
      }
      if ((_meldCode & 1 << 4) != 0)
      {
        return MeldType.AddedKan;
      }
      if ((_meldCode & 1 << 5) != 0)
      {
        throw new FormatException("Nuki not supported");
      }
      if ((_meldCode & 3) != 0)
      {
        return MeldType.CalledKan;
      }
      return MeldType.ClosedKan;
    }

    // Apply a bitmask with all ones in a single block and then treat the selected bits as an integer
    private static int IntFromBits(int value, int numberOfBits, int leftShift)
    {
      var mask = (1 << numberOfBits) - 1;
      return (value >> leftShift) & mask;
    }
  }
}