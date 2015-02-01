﻿// Spines.Tenhou.Client.MatchService.cs
// 
// Copyright (C) 2015  Johannes Heckl
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

using System.Collections.Generic;

namespace Spines.Tenhou.Client.LocalServer
{
  internal class MatchService : IMatchService
  {
    private readonly ISet<LobbyConnection> _players = new HashSet<LobbyConnection>();

    public void EnterQueue(LobbyConnection connection, int lobby, MatchType matchType)
    {
      lock (_players)
      {
        _players.Add(connection);
      }
    }
  }
}