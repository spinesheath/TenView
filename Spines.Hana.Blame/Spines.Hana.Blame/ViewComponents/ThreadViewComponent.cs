﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spines.Hana.Blame.Data;
using Spines.Hana.Blame.Models.ThreadViewModels;
using Spines.Hana.Blame.Models.Wwyd;

namespace Spines.Hana.Blame.ViewComponents
{
  public class ThreadViewComponent : ViewComponent
  {
    public ThreadViewComponent(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(string hand)
    {
      var parsed = WwydHand.Parse(hand);
      if (parsed.IsValid)
      {
        var threadViewModel = await LoadThread(hand);
        return View(threadViewModel);
      }
      return View(new ThreadViewModel());
    }

    private readonly ApplicationDbContext _context;

    private async Task<ThreadViewModel> LoadThread(string hand)
    {
      var comments = await _context.WwydThreads.Where(t => t.Hand == hand).SelectMany(t => t.Comments).ToListAsync();
      var threadViewModel = new ThreadViewModel();
      threadViewModel.Comments.AddRange(comments.Select(c => c.Message));
      threadViewModel.Hand = hand;
      return threadViewModel;
    }
  }
}