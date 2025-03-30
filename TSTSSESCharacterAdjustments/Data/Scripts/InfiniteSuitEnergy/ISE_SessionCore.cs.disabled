using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace InfiniteSuitEnergy {

	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]

	public class ISE_SessionCore : MySessionComponentBase {

		private List<IMyPlayer> _players = new List<IMyPlayer>();
		private bool _initialSetup = false;
		private byte _counter = 0;

		public override void UpdateBeforeSimulation() {

			if (!_initialSetup) {

				_initialSetup = true;

				if (MyAPIGateway.Multiplayer.IsServer == false)
					MyAPIGateway.Utilities.InvokeOnGameThread(() => { this.SetUpdateOrder(MyUpdateOrder.NoUpdate); });

			}

			_counter++;

			if (_counter < 60)
				return;

			_counter = 0;
			_players.Clear();
			MyAPIGateway.Players.GetPlayers(_players);

			foreach (var player in _players) {

				if (player.IsBot || player.Character == null)
					continue;

				MyVisualScriptLogicProvider.SetPlayersEnergyLevel(player.IdentityId, 1);
			
			}

		}

	}
}
