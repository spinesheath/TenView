﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Spines.Hana.Snitch
{
  internal abstract class Watcher
  {
    public event EventHandler HistoryUpdated;

    protected Watcher(Func<IEnumerable<ReplayData>, Task> resultHandler)
    {
      _resultHandler = resultHandler;
    }

    /// <summary>
    /// Queues a timestamp, then waits until a second after the last timestamp.
    /// FileSystemWatcher sometimes raises multiple events for a single modification.
    /// </summary>
    protected async Task QueueChange(string path)
    {
      FileChangeQueue.Enqueue(DateTime.UtcNow);
      if (Semaphore.CurrentCount == 0)
      {
        return;
      }
      await Semaphore.WaitAsync();
      try
      {
        await ClearQueue();
        var newReplays = ReadFile(path);
        await _resultHandler(newReplays);
      }
      catch (IOException)
      {
        await QueueChange(path);
        Logger.Warn($"IOException on file change {path}");
      }
      catch (Exception ex)
      {
        Logger.Error(ex, "on file change");
      }
      finally
      {
        Semaphore.Release();
      }
    }

    protected abstract Regex ReplayRegex { get; }
    private readonly Func<IEnumerable<ReplayData>, Task> _resultHandler;
    private static readonly ConcurrentQueue<DateTime> FileChangeQueue = new ConcurrentQueue<DateTime>();
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

    private IEnumerable<ReplayData> ReadFile(string path)
    {
      var lines = File.ReadAllLines(path);
      var matches = lines.Select(l => ReplayRegex.Match(l)).Where(m => m.Success);
      var results = matches.Reverse().Select(MatchToReplayData).ToList();

      var recent = new HashSet<string>(History.All().Select(r => r.Id));
      var newReplays = results.Where(r => !recent.Contains(r.Id)).ToList();
      if (!newReplays.Any())
      {
        return Enumerable.Empty<ReplayData>();
      }

      History.Append(newReplays);

      HistoryUpdated?.Invoke(this, EventArgs.Empty);

      return newReplays;
    }

    private static ReplayData MatchToReplayData(Match match)
    {
      var id = match.Groups[1].Value;
      var oya = ToInt(match.Groups[2].Value);
      var scores = match.Groups[3].Value.Split(',');
      var playerCount = scores.Length / 2;
      var position = (playerCount - oya) % playerCount;
      return new ReplayData(id, position);
    }

    private static int ToInt(string value)
    {
      return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Waits until a second after the last timestamp in the queue.
    /// </summary>
    private static async Task ClearQueue()
    {
      while (FileChangeQueue.TryDequeue(out var next))
      {
        var target = next + TimeSpan.FromSeconds(1);
        var now = DateTime.UtcNow;
        if (target > now)
        {
          await Task.Delay(target - now);
        }
      }
    }
  }
}