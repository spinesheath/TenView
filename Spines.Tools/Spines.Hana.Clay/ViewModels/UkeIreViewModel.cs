﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Spines.Mahjong.Analysis.Classification;

namespace Spines.Hana.Clay.ViewModels
{
  internal class UkeIreViewModel : ViewModelBase
  {
    public UkeIreViewModel(UkeIreInfo ukeIre)
    {
      Discard = ukeIre.Discard;
      foreach (var tile in ukeIre.Outs.Keys)
      {
        Tiles.Add(tile);
      }
      Count = ukeIre.Outs.Values.DefaultIfEmpty(0).Sum();
      ImprovementRate = Math.Round(ukeIre.ImprovementRate, 5);
    }

    public double ImprovementRate { get; }

    public int Count { get; }

    public Tile? Discard { get; }

    public ICollection<Tile> Tiles { get; } = new ObservableCollection<Tile>();
  }
}