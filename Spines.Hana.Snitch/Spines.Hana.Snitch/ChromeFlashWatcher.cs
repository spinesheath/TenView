// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spines.Hana.Snitch
{
  internal class ChromeFlashWatcher : Watcher
  {
    public ChromeFlashWatcher(Func<IEnumerable<ReplayData>, Task> resultHandler)
      : base(resultHandler)
    {
      var directory = GetDirectory();
      if (!Directory.Exists(directory))
      {
        return;
      }
      var fsw = new FileSystemWatcher(directory, FileName);
      fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
      fsw.Renamed += OnRenamed;
      fsw.EnableRaisingEvents = true;
    }

    protected override string GetPath()
    {
      var directory = GetDirectory();
      return directory == null ? null : Path.Combine(directory, FileName);
    }

    private const string FileName = "mjinfo.sol";

    private async void OnRenamed(object sender, FileSystemEventArgs e)
    {
      await QueueChange(e.FullPath);
    }

    private static string GetDirectory()
    {
      var roaming = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      var shared = Path.Combine(roaming, "Google", "Chrome", "User Data", "Default", "Pepper Data", "Shockwave Flash", "WritableRoot", "#SharedObjects");
      return Directory.GetDirectories(shared, "mjv.jp", SearchOption.AllDirectories).FirstOrDefault();
    }
  }
}