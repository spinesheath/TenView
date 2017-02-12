﻿// Spines.Tools.AnalyzerBuilder.MainWindow.xaml.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Spines.Mahjong.Analysis.Classification;
using Spines.Mahjong.Analysis.Combinations;
using Spines.Tools.AnalyzerBuilder.Precalculation;
using Spines.Utility;

namespace Spines.Tools.AnalyzerBuilder
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : IProgressManager
  {
    private static readonly IDictionary<CreationType, Func<int, IEnumerable<string>>> CreatorFuncs = new Dictionary<CreationType, Func<int, IEnumerable<string>>>
    {
      {CreationType.ConcealedSuit, CreateConcealedCombinations},
      {CreationType.MeldedSuit, CreateMeldedCombinations},
      {CreationType.MixedSuit, CreateMixedCombinations},
      {CreationType.AnalyzedSuit, CreateAnalyzedSuit},
      {CreationType.ArrangementCsv, CreateArrangementCsvLines},
      {CreationType.AnalyzedHonors, CreateAnalyzedHonors}
    };

    /// <summary>
    /// Constructor.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
    }

    private static IEnumerable<string> CreateAnalyzedSuit(int count)
    {
      var maxMelds = (14 - count) / 3;
      for (var meldCount = 0; meldCount <= maxMelds; ++meldCount)
      {
        var meldedCreator = MeldedCombinationsCreator.ForSuits();
        var meldedCombinations = meldedCreator.Create(meldCount);
        foreach (var meldedCombination in meldedCombinations)
        {
          var m = meldCount;
          var concealedCreator = ConcealedCombinationCreator.ForSuits();
          var combinations = concealedCreator.Create(count, meldedCombination);
          foreach (var combination in combinations)
          {
            var analyzer = TileGroupAnalyzer.ForSuits(combination, meldedCombination, m);
            var arrangements = analyzer.Analyze();
            var arrangementsString = GetArrangementsString(arrangements);
            yield return $"{m}{string.Join("", meldedCombination.Counts)}{string.Join("", combination.Counts)}{arrangementsString}";
          }
        }
      }
    }

    private static string GetArrangementsString(IEnumerable<Arrangement> arrangements)
    {
      var formattedArrangements = arrangements.Select(a => $"({a.JantouValue},{a.MentsuCount},{a.MentsuValue})");
      return string.Join("", formattedArrangements);
    }

    private static IEnumerable<string> CreateAnalyzedHonors(int count)
    {
      var maxMelds = (14 - count) / 3;
      for (var meldCount = 0; meldCount <= maxMelds; ++meldCount)
      {
        var meldedCreator = MeldedCombinationsCreator.ForHonors();
        var meldedCombinations = meldedCreator.Create(meldCount);
        foreach (var meldedCombination in meldedCombinations)
        {
          var m = meldCount;
          var concealedCreator = ConcealedCombinationCreator.ForHonors();
          var combinations = concealedCreator.Create(count, meldedCombination);
          foreach (var combination in combinations)
          {
            var analyzer = TileGroupAnalyzer.ForHonors(combination, meldedCombination, m);
            var arrangements = analyzer.Analyze();
            var formattedArrangements = arrangements.Select(CreateCompactArrangementString);
            var arrangementsString = string.Join("", formattedArrangements);
            yield return $"{m}{string.Join("", meldedCombination.Counts)}{string.Join("", combination.Counts)}{arrangementsString}";
          }
        }
      }
    }

    private static string CreateCompactArrangementString(Arrangement a)
    {
      return $"({a.JantouValue},{a.MentsuCount},{a.MentsuValue})";
    }

    private void Create(CreationType creationType)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      CreateCombinationsAsync(workingDirectory, creationType);
    }

    private void UniqueArrangementCombinations(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      var files = GetAnalyzedFiles(workingDirectory);
      var lines = files.SelectMany(File.ReadAllLines);
      var combinations = lines.Select(GetCombinationSubstring);
      var unique = combinations.Distinct();
      var ordered = unique.OrderBy(u => u);
      var targetFile = Path.Combine(workingDirectory, "ArrangementCombinations.txt");
      File.WriteAllLines(targetFile, ordered);
    }

    private static IEnumerable<string> GetAnalyzedFiles(string workingDirectory)
    {
      var suitPrefix = CreationData.Prefixes[CreationType.AnalyzedSuit];
      var honorPrefix = CreationData.Prefixes[CreationType.AnalyzedHonors];
      return Directory.GetFiles(workingDirectory).Where(f => f.Contains(suitPrefix) || f.Contains(honorPrefix));
    }

    private static string GetCombinationSubstring(string line)
    {
      var index = line.IndexOf('(');
      return line.Substring(index);
    }

    private void CreateArrangementWords(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      CreateArrangementWordsAsync(workingDirectory);
    }

    private async void CreateArrangementWordsAsync(string workingDirectory)
    {
      ProgressBar.Minimum = 0;
      ProgressBar.Maximum = 100;
      ProgressBar.Value = 0;
      await Task.Run(() => CreateArrangementWords(workingDirectory));
    }

    private static IEnumerable<List<Arrangement>> GetAllArrangements(string workingDirectory)
    {
      var arrangementsFile = Path.Combine(workingDirectory, "ArrangementCombinations.txt");
      var lines = File.ReadAllLines(arrangementsFile);
      return lines.Select(ParseArrangements).Select(a => a.ToList());
    }

    private void FindRedundantArrangements(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      Reset(100);
      var b = true;
      while (b)
      {
        b = RemoveRedundantArrangement(workingDirectory);
      }
      ProgressBar.Value = 100;
    }

    private static bool RemoveRedundantArrangement(string workingDirectory)
    {
      var redundancyLog = Path.Combine(workingDirectory, "RedundanciesLog.txt");
      File.WriteAllLines(redundancyLog, Enumerable.Empty<string>());

      var arrangements = GetAllArrangements(workingDirectory).ToList();
      var alphabetSize = arrangements.Count;
      var tilesInArrangements = arrangements.Select(a => a.Max(b => b.TotalValue)).ToList();

      for (var i = 0; i < arrangements.Count; ++i)
      {
        var arrangement = arrangements[i];
        if (arrangement.Count < 2)
        {
          continue;
        }

        for (var j = 0; j < arrangement.Count; ++j)
        {
          var isRedundant = true;
          var language = CreateBaseLanguage(alphabetSize);
          foreach (var word in language)
          {
            if (word.All(c => c != i))
            {
              continue;
            }

            var sumOfTiles = word.Sum(c => tilesInArrangements[c]);
            if (sumOfTiles > 14)
            {
              continue;
            }
            var analyzer = new ArrangementAnalyzer();
            foreach (var character in word)
            {
              analyzer.AddSetOfArrangements(arrangements[character]);
            }
            var shanten = analyzer.CalculateShanten();
            if (shanten >= 9)
            {
              continue;
            }

            var replacement = arrangement.Where((t, index) => index != j).ToList();
            var analyzer2 = new ArrangementAnalyzer();
            foreach (var character in word)
            {
              analyzer2.AddSetOfArrangements(character == i ? replacement : arrangements[character]);
            }
            var shanten2 = analyzer2.CalculateShanten();
            if (shanten != shanten2)
            {
              isRedundant = false;
              break;
            }
          }
          if (isRedundant)
          {
            File.AppendAllLines(redundancyLog, $"Removing {arrangements[i][j]} from {string.Join("", arrangements[i])}; i = {i}, j = {j}".Yield());
            arrangements[i].RemoveAt(j);
            var newLines = arrangements.Select(GetArrangementsString).Distinct().OrderBy(line => line);
            var targetFile = Path.Combine(workingDirectory, "ArrangementCombinations.txt");
            File.WriteAllLines(targetFile, newLines);
            return true;
          }
        }
      }
      return false;
    }

    private void RedundancyLogToIds(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      Reset(100);

      var redundancyLog = Path.Combine(workingDirectory, "RedundanciesLog.txt");
      var lines = File.ReadAllLines(redundancyLog);
      var newLines = lines.Select(RedundanciesToIds);
      var targetFile = Path.Combine(workingDirectory, "RedundanciesByIds.txt");
      File.WriteAllLines(targetFile, newLines);

      ProgressBar.Value = 100;
    }

    private static string RedundanciesToIds(string line)
    {
      var regex = new Regex(@"Removing .* from (.*); i = .*, j = ([0-9]+)");
      var match = regex.Match(line);
      var arrangementsString = match.Groups[1].Value;
      var toRemove = Convert.ToInt32(match.Groups[2].Value);
      var arrangements = ParseArrangements(arrangementsString);
      return string.Join(",", arrangements.Select(a => a.Id)) + $";{toRemove}";
    }

    private void CreateArrangementWords(string workingDirectory)
    {
      var arrangements = GetAllArrangements(workingDirectory);
      var words = CreateWords(arrangements);
      var wordsFile = Path.Combine(workingDirectory, "ArrangementWords.txt");
      File.WriteAllLines(wordsFile, words.Select(w => string.Join(",", w.Word) + ":" + w.Value));
    }

    private IEnumerable<WordWithValue> CreateWords(IEnumerable<List<Arrangement>> arrangements)
    {
      var arrangementsList = arrangements.ToList();
      var alphabetSize = arrangementsList.Count;
      var language = CreateBaseLanguage(alphabetSize);
      var tilesInArrangements = arrangementsList.Select(a => a.Max(b => b.TotalValue)).ToList();

      var max = alphabetSize * ((long)alphabetSize + 1) * ((long)alphabetSize + 2) * ((long)alphabetSize + 3) / 24;
      long count = 0;
      long current = 0;
      foreach (var word in language)
      {
        var sumOfTiles = word.Sum(c => tilesInArrangements[c]);
        if (sumOfTiles <= 14)
        {
          var analyzer = new ArrangementAnalyzer();
          foreach (var character in word)
          {
            analyzer.AddSetOfArrangements(arrangementsList[character]);
          }
          var shanten = analyzer.CalculateShanten();
          if (shanten < 9)
          {
            yield return new WordWithValue(word, shanten);
          }
        }

        count += 1;
        if (current * (max / 100) < count)
        {
          IncrementProgressBar();
          current += 1;
        }
      }
    }

    private static IEnumerable<IList<int>> CreateBaseLanguage(int alphabetSize)
    {
      for (var a = 0; a < alphabetSize; ++a)
      {
        for (var b = a; b < alphabetSize; ++b)
        {
          for (var c = b; c < alphabetSize; ++c)
          {
            for (var d = c; d < alphabetSize; ++d)
            {
              yield return new [] {a, b, c, d};
            }
          }
        }
      }
    }

    private static IEnumerable<Arrangement> ParseArrangements(string line)
    {
      var regex = new Regex(@"\((\d+), *(\d+), *(\d+)\)");
      var matches = regex.Matches(line);
      foreach (Match match in matches)
      {
        var jantouValue = Convert.ToInt32(match.Groups[1].Value);
        var mentsuCount = Convert.ToInt32(match.Groups[2].Value);
        var mentsuValue = Convert.ToInt32(match.Groups[3].Value);
        yield return new Arrangement(jantouValue, mentsuCount, mentsuValue);
      }
    }

    private async void CreateCombinationsAsync(string workingDirectory, CreationType creationType)
    {
      var creationCount = CreationData.CreationCounts[creationType];
      ProgressBar.Minimum = 0;
      ProgressBar.Maximum = creationCount;
      ProgressBar.Value = 0;
      var counts = Enumerable.Range(0, creationCount);
      await Task.Run(() => Parallel.ForEach(counts, c => CreateCombinationFile(c, workingDirectory, creationType)));
    }

    private void CreateCombinationFile(int count, string workingDirectory, CreationType creationType)
    {
      var lines = CreatorFuncs[creationType].Invoke(count);
      WriteToFile(workingDirectory, count, lines, creationType);
      IncrementProgressBar();
    }

    private void CreateArrangementClassifier(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      var factory = new ArrangementClassifierFactory(this, workingDirectory);
      factory.CreateAsync();
    }

    private void CreateHonorClassifier(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      var factory = HandClassifierFactory.CreateHonorClassifierFactory(this, workingDirectory, "HonorClassifier.bin");
      factory.CreateAsync(false);
    }

    private void CreateMirroredHonorClassifier(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      var factory = HandClassifierFactory.CreateHonorClassifierFactory(this, workingDirectory, "MirroredHonorClassifier.bin");
      factory.CreateAsync(true);
    }

    private void CreateSuitClassifier(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      var factory = HandClassifierFactory.CreateSuitClassifierFactory(this, workingDirectory, "SuitClassifier.bin");
      factory.CreateAsync(false);
    }

    private void CreateMirroredSuitClassifier(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (workingDirectory == null)
      {
        return;
      }

      var factory = HandClassifierFactory.CreateSuitClassifierFactory(this, workingDirectory, "MirroredSuitClassifier.bin");
      factory.CreateAsync(true);
    }

    private static void WriteToFile(string workingDirectory, int count, IEnumerable<string> lines, CreationType creationType)
    {
      var prefix = CreationData.Prefixes[creationType];
      var fileType = CreationData.FileTypes[creationType];
      var fileName = $"{prefix}_{count}.{fileType}";
      var path = Path.Combine(workingDirectory, fileName);
      File.WriteAllLines(path, lines);
    }

    private static IEnumerable<string> CreateLines(IEnumerable<Combination> combinations)
    {
      return combinations.Select(c => string.Join(string.Empty, c.Counts));
    }

    private static IEnumerable<string> CreateConcealedCombinations(int count)
    {
      return CreateLines(ConcealedCombinationCreator.ForSuits().Create(count));
    }

    private static IEnumerable<string> CreateMeldedCombinations(int count)
    {
      return CreateLines(MeldedCombinationsCreator.ForSuits().Create(count));
    }

    private static IEnumerable<string> CreateMixedCombinations(int count)
    {
      var maxMelds = (14 - count) / 3;
      for (var meldCount = 0; meldCount <= maxMelds; ++meldCount)
      {
        var meldedCreator = MeldedCombinationsCreator.ForSuits();
        var meldedCombinations = meldedCreator.Create(meldCount);
        foreach (var meldedCombination in meldedCombinations)
        {
          var m = meldCount;
          var concealedCreator = ConcealedCombinationCreator.ForSuits();
          var combinations = concealedCreator.Create(count, meldedCombination);
          foreach (var combination in combinations)
          {
            yield return $"{m}{string.Join("", meldedCombination.Counts)}{string.Join("", combination.Counts)}";
          }
        }
      }
    }

    private void IncrementProgressBar()
    {
      Dispatcher.Invoke(() => ProgressBar.Value += 1);
    }

    private void CreateConcealedCombinations(object sender, RoutedEventArgs e)
    {
      Create(CreationType.ConcealedSuit);
    }

    private void CreateMeldedCombinations(object sender, RoutedEventArgs e)
    {
      Create(CreationType.MeldedSuit);
    }

    private void CreateMixedCombinations(object sender, RoutedEventArgs e)
    {
      Create(CreationType.MixedSuit);
    }

    private void AnalyzeSuitCombinations(object sender, RoutedEventArgs e)
    {
      Create(CreationType.AnalyzedSuit);
    }

    private void CreateArrangementCsv(object sender, RoutedEventArgs e)
    {
      Create(CreationType.ArrangementCsv);
    }

    private void AnalyzeHonorCombinations(object sender, RoutedEventArgs e)
    {
      Create(CreationType.AnalyzedHonors);
    }

    private static IEnumerable<string> CreateArrangementCsvLines(int tileCount)
    {
      var arrangements = CreateAllArrangements(tileCount).ToList();
      var comparer = new ArrangementComparer();
      yield return ";" + string.Join(";", arrangements);
      foreach (var a in arrangements)
      {
        var sb = new StringBuilder();
        sb.Append(a);
        sb.Append(";");
        foreach (var b in arrangements)
        {
          var worse = comparer.IsWorseThan(a, b);
          var better = comparer.IsWorseThan(b, a);
          if (worse && better)
          {
            sb.Append("x;");
          }
          else if(better)
          {
            sb.Append("b;");
          }
          else if (worse)
          {
            sb.Append("w;");
          }
          else
          {
            sb.Append("o;");
          }
        }
        yield return sb.ToString();
      }
    }

    private string GetWorkingDirectory()
    {
      using (var dialog = new CommonOpenFileDialog())
      {
        dialog.IsFolderPicker = true;
        dialog.EnsurePathExists = true;
        var result = dialog.ShowDialog(this);
        if (result != CommonFileDialogResult.Ok)
        {
          return null;
        }
        return dialog.FileNames.Single();
      }
    }

    private static IEnumerable<Arrangement> CreateAllArrangements(int totalTiles)
    {
      for (var jantouValue = 0; jantouValue <= 2; ++jantouValue)
      {
        for (var mentsuCount = 0; mentsuCount <= 4; ++mentsuCount)
        {
          for (var mentsuValue = mentsuCount; mentsuValue <= mentsuCount * 3; ++mentsuValue)
          {
            var arrangement = new Arrangement(jantouValue, mentsuCount, mentsuValue);
            if (arrangement.TotalValue <= totalTiles)
            {
              yield return arrangement;
            }
          }
        }
      }
    }

    /// <summary>
    /// Resets the ProgressBar.
    /// </summary>
    public void Reset(int max)
    {
      ProgressBar.Minimum = 0;
      ProgressBar.Maximum = max;
      ProgressBar.Value = 0;
    }

    /// <summary>
    /// Increments the ProgressBar.
    /// </summary>
    public void Increment()
    {
      IncrementProgressBar();
    }

    /// <summary>
    /// Shows that the process is done.
    /// </summary>
    public void Done()
    {
      ProgressBar.Value = 0;
    }

    private void CreateHandToCompactedArrangements(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (null == workingDirectory)
      {
        return;
      }
      var dic = GetArrangementDictionary(workingDirectory);
      var handFiles = GetAnalyzedFiles(workingDirectory);
      var compactionData = GetCompactionData(workingDirectory);
      foreach (var handFile in handFiles)
      {
        var targetName = handFile.Replace("Analyzed", "c_").Replace("Combinations_", "_");
        var lines = File.ReadAllLines(handFile);
        var transformed = lines.Select(s => ReplaceArrangementId(s, compactionData));
        File.WriteAllLines(targetName, transformed);
      }
    }

    private static Dictionary<string, string> GetCompactionData(string workingDirectory)
    {
      var lines = File.ReadAllLines(Path.Combine(workingDirectory, "compactionData.txt"));
      return lines.ToDictionary(line => line.Substring(0, line.IndexOf('>')), line => line.Substring(line.IndexOf('>') + 1));
    }

    private string ReplaceArrangementId(string arg, Dictionary<string, string> compactionData)
    {
      var i = arg.IndexOf('(');
      var tiles = arg.Substring(0, i);
      var arrangements = arg.Substring(i);
      if (compactionData.ContainsKey(arrangements))
        arrangements = compactionData[arrangements];
      return tiles + arrangements;
    }

    private static Dictionary<string, int> GetArrangementDictionary(string workingDirectory)
    {
      var arrangementFile = Path.Combine(workingDirectory, "ArrangementCombinations.txt");
      var lines = File.ReadAllLines(arrangementFile);
      var withIndices = lines.Select((a, i) => new { Arrangement = a, Index = i });
      return withIndices.ToDictionary(p => p.Arrangement, p => p.Index);
    }

    private void CreateArrangementCompactionData(object sender, RoutedEventArgs e)
    {
      var workingDirectory = GetWorkingDirectory();
      if (null == workingDirectory)
      {
        return;
      }
      var redundancyLog = Path.Combine(workingDirectory, "RedundanciesLog.txt");
      var lines = File.ReadAllLines(redundancyLog);
      var dic = lines.Select(GetCompactionReplacement).ToDictionary(k => k.Key, k => k.Value);
      var data = CreateCompactionData(dic);
      File.WriteAllLines(Path.Combine(workingDirectory, "compactionData.txt"), data);
    }

    private static IEnumerable<string> CreateCompactionData(IReadOnlyDictionary<string, string> dic)
    {
      foreach (var kvp in dic)
      {
        var target = kvp.Value;
        while (dic.ContainsKey(target))
        {
          target = dic[target];
        }
        yield return kvp.Key + ">" + target;
      }
    }

    private static KeyValuePair<string, string> GetCompactionReplacement(string line)
    {
      var regex = new Regex(@"Removing (.*) from (.*);");
      var match = regex.Match(line);
      var arrangementsString = match.Groups[2].Value.Replace(" ", "");
      var toRemove = match.Groups[1].Value.Replace(" ", "");
      return new KeyValuePair<string, string>(arrangementsString, arrangementsString.Replace(toRemove, ""));
    }

    private void AllAtOnce(object sender, RoutedEventArgs e)
    {
      var wd = GetWorkingDirectory();
      if (null == wd)
      {
        return;
      }
      var c = new Creator(wd);
      c.Create();
    }
  }
}