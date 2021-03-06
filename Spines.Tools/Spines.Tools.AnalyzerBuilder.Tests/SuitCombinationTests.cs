﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using NUnit.Framework;
using Spines.Tools.AnalyzerBuilder.Combinations;

namespace Spines.Tools.AnalyzerBuilder.Tests
{
  [TestFixture]
  public class SuitCombinationTests
  {
    /// <summary>
    /// Number of combinations of melds in a single suit.
    /// </summary>
    [Test]
    public void TestMeldCombinations()
    {
      VerifyMeld(0, 1);
      VerifyMeld(1, 25);
      VerifyMeld(2, 284);
      VerifyMeld(3, 1914);
      VerifyMeld(4, 8439);
    }

    /// <summary>
    /// Number of combinations of tiles in a single suit, no melds.
    /// </summary>
    [Test]
    public void TestConcealedSuitCombinations()
    {
      VerifyConcealed(0, 1);
      VerifyConcealed(1, 5);
      VerifyConcealed(2, 25);
      VerifyConcealed(3, 85);
      VerifyConcealed(4, 255);
      //VerifyConcealed(5, 649);
      //VerifyConcealed(6, 1481);
      //VerifyConcealed(7, 3042);
      //VerifyConcealed(8, 5739);
      //VerifyConcealed(9, 9987);
      //VerifyConcealed(10, 16196);
      //VerifyConcealed(11, 24551);
      //VerifyConcealed(12, 34988);
      //VerifyConcealed(13, 46976);
      //VerifyConcealed(14, 59618);
      //VerifyConcealed(15, 71606);
      //VerifyConcealed(16, 81564);
      //VerifyConcealed(17, 88138);
      //VerifyConcealed(18, 90453);
      //VerifyConcealed(19, 88138);
      //VerifyConcealed(20, 81564);
    }

    /// <summary>
    /// Number of combinations in a single suit with concealed and melded tiles.
    /// </summary>
    [Test]
    public void TestMixedSuitCombinations()
    {
      VerifyMixed(1, 1, 112);
      VerifyMixed(1, 2, 531);
      VerifyMixed(1, 3, 1846);
      //VerifyMixed(1, 4, 5290);
      //VerifyMixed(1, 5, 13039);
      //VerifyMixed(1, 6, 28432);
      //VerifyMixed(1, 7, 55887);
      //VerifyMixed(1, 8, 100289);
      //VerifyMixed(1, 9, 165876);
      //VerifyMixed(1, 10, 254510);
      //VerifyMixed(1, 11, 364253);
      //VerifyMixed(1, 12, 488084);

      VerifyMixed(2, 1, 1195);
      VerifyMixed(2, 2, 5432);
      //VerifyMixed(2, 3, 18145);
      //VerifyMixed(2, 4, 49662);
      //VerifyMixed(2, 5, 116643);
      //VerifyMixed(2, 6, 241712);
      //VerifyMixed(2, 7, 449994);
      //VerifyMixed(2, 8, 762682);
      //VerifyMixed(2, 9, 1186998);

      VerifyMixed(3, 1, 7612);
      //VerifyMixed(3, 2, 32935);
      //VerifyMixed(3, 3, 104636);
      //VerifyMixed(3, 4, 271414);
      //VerifyMixed(3, 5, 602474);
      //VerifyMixed(3, 6, 1175440);

      //VerifyMixed(4, 1, 31814);
      //VerifyMixed(4, 2, 129749);
      //VerifyMixed(4, 3, 388080);
    }

    private static void VerifyMeld(int numberOfMelds, int numberOfCombinations)
    {
      var creator = MeldedCombinationsCreator.ForSuits();
      var combinations = creator.Create(numberOfMelds);
      Assert.AreEqual(numberOfCombinations, combinations.Count(), "Count of meld combinations was wrong");
    }

    private static void VerifyConcealed(int numberOfTiles, int numberOfCombinations)
    {
      var creator = ConcealedCombinationCreator.ForSuits();
      var combinations = creator.Create(numberOfTiles);
      Assert.AreEqual(numberOfCombinations, combinations.Count(), "Count of concealed combinations was wrong");
    }

    private static void VerifyMixed(int numberOfMelds, int numberOfTiles, int numberOfCombinations)
    {
      var meldCreator = MeldedCombinationsCreator.ForSuits();
      var concealedCreator = ConcealedCombinationCreator.ForSuits();
      var meldedCombinations = meldCreator.Create(numberOfMelds).ToList();
      var mixedCombinations = meldedCombinations.SelectMany(c => concealedCreator.Create(numberOfTiles, c));
      Assert.AreEqual(numberOfCombinations, mixedCombinations.Count(), "Count of mixed combinations was wrong");
    }
  }
}