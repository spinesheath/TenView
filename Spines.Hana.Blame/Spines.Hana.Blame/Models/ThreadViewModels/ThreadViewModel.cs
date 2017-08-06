﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Spines.Hana.Blame.Models.ThreadViewModels
{
  public class ThreadViewModel
  {
    public ThreadViewModel()
    {
      Comments = new List<string>();
      Message = string.Empty;
    }

    public List<string> Comments { get; }

    public string Message { get; set; }

    public string Hand { get; set; }
  }
}