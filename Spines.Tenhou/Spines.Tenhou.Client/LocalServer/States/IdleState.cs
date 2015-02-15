﻿// Spines.Tenhou.Client.IdleState.cs
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

using Spines.Utility;

namespace Spines.Tenhou.Client.LocalServer.States
{
  internal class IdleState : StateBase
  {
    private readonly string _accountId;
    private readonly LobbyConnection _connection;

    public IdleState(LobbyConnection connection, string accountId)
    {
      _connection = connection;
      _accountId = accountId;
    }

    public override IState Process(Message message)
    {
      RestartTimer();
      if (message.Content.Name == "BYE")
      {
        _connection.LogOff(_accountId);
        return new ConnectionEstablishedState(_connection);
      }
      if (message.Content.Name != "JOIN")
      {
        return this;
      }
      var parts = message.Content.Attribute("t").Value.Split(new[] {','});
      var lobby = InvariantConvert.ToInt32(parts[0]);
      var matchType = new MatchType(InvariantConvert.ToInt32(parts[1]));
      if (!_connection.MatchServer.CanEnterQueue(_accountId))
      {
        return this;
      }
      _connection.MatchServer.EnterQueue(_accountId, lobby, matchType);
      return new InQueueState(_connection, _accountId);
    }
  }
}