﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Spines.Utility;

namespace Spines.Tools.AnalyzerBuilder.Combinations
{
  /// <summary>
  /// Determines whether an arrangement is worse than another.
  /// </summary>
  internal class ArrangementComparer
  {
    /// <summary>
    /// Determines whether an arrangement is worse than another.
    /// </summary>
    public bool IsWorseThan(Arrangement lhs, Arrangement rhs)
    {
      Validate.NotNull(lhs, nameof(lhs));
      Validate.NotNull(rhs, nameof(rhs));

      if (lhs == rhs)
      {
        return false;
      }
      // Same mentsu but better pairs.
      if (lhs.JantouValue < rhs.JantouValue && lhs.MentsuCount == rhs.MentsuCount && lhs.MentsuValue == rhs.MentsuValue)
      {
        return true;
      }
      // Same TotalValue and MentsuCount, but higher PairValue is worse.
      if (lhs.JantouValue > rhs.JantouValue && lhs.MentsuCount == rhs.MentsuCount && lhs.TotalValue == rhs.TotalValue)
      {
        return true;
      }
      // Both with or without jantou.
      if (lhs.JantouValue == rhs.JantouValue)
      {
        // Perfect with more mentsu is better than perfect with less mentsu.
        if (lhs.MentsuCount < rhs.MentsuCount)
        {
          return IsPerfect(lhs) && IsPerfect(rhs);
        }
        // Lower value with same MentsuCount is worse.
        if (lhs.MentsuCount == rhs.MentsuCount)
        {
          return lhs.MentsuValue < rhs.MentsuValue;
        }
        // Same value with more mentsu is worse.
        if (lhs.MentsuCount > rhs.MentsuCount)
        {
          return lhs.TotalValue <= rhs.TotalValue;
        }
      }
      return false;
    }

    private static bool IsPerfect(Arrangement arrangement)
    {
      return arrangement.MentsuValue == arrangement.MentsuCount * 3 && arrangement.JantouValue != 1;
    }
  }
}