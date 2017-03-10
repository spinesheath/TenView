﻿// Spines.Mahjong.Analysis.InternalTests.ShantenCalculatorTests.cs
// 
// Copyright (C) 2017  Johannes Heckl
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Spines.Mahjong.Analysis.Classification;
using Spines.Mahjong.Analysis.Combinations;
using Spines.Utility;

namespace Spines.Mahjong.Analysis.InternalTests
{
  [TestFixture]
  public class ShantenCalculatorTests
  {
    [TestCase("123456789m12344p", -1)]
    [TestCase("123456789m1234p", 0)]
    [TestCase("123456789m1245p", 1)]
    [TestCase("123456789m147p1s", 2)]
    [TestCase("12345679m147p14s", 3)]
    [TestCase("1345679m147p147s", 4)]
    [TestCase("145679m147p147s1z", 5)]
    [TestCase("14679m147p147s12z", 6)]
    [TestCase("1479m147p147s123z", 7)]
    [TestCase("147m147p147s1234z", 8)]
    [TestCase("123456789m44p123S", -1)]
    [TestCase("1245p112z333P6666P", 2)]
    public void Calculate2ShouldBeCorrect(string hand, int expected)
    {
      var c = new ShantenCalculator();

      var actual = c.Calculate(hand);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CalculateShouldBeFast()
    {
      var emptyMelds = new int[0];
      var emptySuit = new int[9];
      var emptyHonor = new int[15];
      var classifier = new Classifier();
      foreach (var hand in GetChinitsuHands().Take(1000))
      {
        var shanten = classifier.ClassifyArrangements(
          classifier.ClassifySuits(hand.Item1, hand.Item2),
          classifier.ClassifySuits(emptyMelds, emptySuit),
          classifier.ClassifySuits(emptyMelds, emptySuit),
          classifier.ClassifyHonors(emptyHonor));
        Assert.That(shanten, Is.GreaterThan(-2));
      }
    }

    [Test]
    public void ProgressiveCalculationShouldWork()
    {
      var rand = new Random(1);
      var classifier = new Classifier();

      var concealed = new int[34];
      var melded = new int[34];
      var discarded = new int[34];
      const int meldCount = 0;
      var shanten = 10;
      var emptyMelds = new int[0];
      var actualDraws = 0;
      for (var i = 0; i < 10000; ++i)
      {
        if (shanten == -1 || Enumerable.Range(0, 34).All(a => concealed[a] + melded[a] + discarded[a] == 4))
        {
          concealed.Populate(0);
          melded.Populate(0);
          discarded.Populate(0);
          shanten = 10;
        }

        var t = rand.Next(34);
        var draw = ToDiscard(t);
        if (concealed[t] + melded[t] + discarded[t] == 4)
        {
          continue;
        }
        concealed[t] += 1;
        actualDraws += 1;
        if (concealed.Sum() + meldCount * 3 < 14)
        {
          continue;
        }

        var curHand = ToHand(concealed);

        shanten = classifier.ClassifyArrangements(
            classifier.ClassifySuits(emptyMelds, concealed.Slice(0, 9)),
            classifier.ClassifySuits(emptyMelds, concealed.Slice(9, 9)),
            classifier.ClassifySuits(emptyMelds, concealed.Slice(18, 9)),
            classifier.ClassifyHonors(Enumerable.Repeat(0, 8).Concat(concealed.Slice(27, 7).OrderByDescending(x => x)).ToList()));

        if (shanten == -1)
        {
          continue;
        }

        var ukeIreCount = -1;
        var bestDiscard = -1;
        var bestShanten = 10;
        var bestUkeIre = new int[34];
        for (var j = 0; j < 34; ++j)
        {
          if (concealed[j] == 0)
          {
            continue;
          }
          concealed[j] -= 1;
          discarded[j] += 1;

          var handAfterDiscard = ToHand(concealed);

          var s1 = classifier.ClassifyArrangements(
            classifier.ClassifySuits(emptyMelds, concealed.Slice(0, 9)),
            classifier.ClassifySuits(emptyMelds, concealed.Slice(9, 9)),
            classifier.ClassifySuits(emptyMelds, concealed.Slice(18, 9)),
            classifier.ClassifyHonors(Enumerable.Repeat(0, 8).Concat(concealed.Slice(27, 7).OrderByDescending(x => x)).ToList()));

          if (bestShanten < 10)
          {
            Assert.That(s1, Is.AtLeast(shanten));
            Assert.That(s1, Is.AtMost(shanten + 1));
          }

          if (s1 > bestShanten)
          {
            concealed[j] += 1;
            discarded[j] -= 1;
            continue;
          }

          var ukeIre = new int[34];
          var u = 0;
          for (var k = 0; k < 34; ++k)
          {
            var used = concealed[k] + melded[k] + discarded[k];
            if (used == 4)
            {
              continue;
            }
            concealed[k] += 1;

            var handAfterDraw = ToHand(concealed);

            var s2 = classifier.ClassifyArrangements(
              classifier.ClassifySuits(emptyMelds, concealed.Slice(0, 9)),
              classifier.ClassifySuits(emptyMelds, concealed.Slice(9, 9)),
              classifier.ClassifySuits(emptyMelds, concealed.Slice(18, 9)),
              classifier.ClassifyHonors(Enumerable.Repeat(0, 8).Concat(concealed.Slice(27, 7).OrderByDescending(x => x)).ToList()));

            if (bestShanten < 10)
            {
              Assert.That(s2, Is.AtLeast(s1 - 1));
              Assert.That(s2, Is.AtMost(s1));
            }

            concealed[k] -= 1;

            if (s2 < shanten)
            {
              ukeIre[k] = 1;
              u += 4 - used;
            }
          }

          if (u > ukeIreCount || s1 < bestShanten)
          {
            Assert.That(s1, Is.AtMost(bestShanten));

            bestUkeIre = ukeIre;
            ukeIreCount = u;
            bestDiscard = j;
            bestShanten = s1;
          }

          concealed[j] += 1;
          discarded[j] -= 1;
        }

        var discard = ToDiscard(bestDiscard);
        var ukeIreTiles = ToHand(bestUkeIre);

        concealed[bestDiscard] -= 1;
        discarded[bestDiscard] += 1;
        shanten = bestShanten;
      }
      Assert.That(actualDraws, Is.Not.Zero);
    }

    private static string ToDiscard(int discard)
    {
      var i = discard / 9;
      var c = discard % 9;
      return (char)('1' + c) + "mpsz".Substring(i, 1);
    }

    private static string ToHand(IReadOnlyList<int> concealed)
    {
      return ToHand(concealed.Slice(0, 9), 'm') +
             ToHand(concealed.Slice(9, 9), 'p') +
             ToHand(concealed.Slice(18, 9), 's') +
             ToHand(concealed.Slice(27, 7), 'z');
    }

    private static string ToHand(IReadOnlyList<int> slice, char suit)
    {
      var sb = new StringBuilder();
      for (var i = 0; i < slice.Count; ++i)
      {
        for (var j = 0; j < slice[i]; ++j)
        {
          sb.Append((char)('1' + i));
        }
      }
      sb.Append(suit);
      return sb.ToString();
    }

    private static IEnumerable<Tuple<int[], int[]>> GetChinitsuHands()
    {
      foreach (var meldCount in Enumerable.Range(0, 5))
      {
        var baseLanguage = Enumerable.Repeat(Enumerable.Range(0, 25), meldCount).CartesianProduct();
        foreach (var w in baseLanguage)
        {
          var meldWord = w.ToArray();
          var oldWord = new int[9];
          foreach (var c in meldWord)
          {
            if (c < 7)
            {
              oldWord[c + 0] += 1;
              oldWord[c + 1] += 1;
              oldWord[c + 2] += 1;
            }
            else if (c < 16)
            {
              oldWord[c - 7] += 3;
            }
            else if (c < 25)
            {
              oldWord[c - 16] += 4;
            }
          }
          if (oldWord.Any(c => c > 4))
          {
            continue;
          }

          var concealeds = ConcealedCombinationCreator.ForSuits().Create(13 - meldCount * 3, new Combination(oldWord));
          foreach (var concealed in concealeds)
          {
            yield return Tuple.Create(meldWord, concealed.Counts.ToArray());
          }
        }
      }
    }
  }
}