using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
///  This class is a bare bones implementation for notifiying actors when other players have been discovered on the screen. Most of this code was copied from the EnemyWatcher class, circumventing the need for the Trait to be AnnounceOnSeen.
/// </summary>
namespace OpenRA.Mods.Common.Traits.Esu
{
    [Desc("Tracks neutral and enemy actors' visibility and notifies the player.",
        "Attach this to the player actor.")]
    class DiscoveryNotificationWatcherInfo : ITraitInfo
    {
        [Desc("Interval in ticks between scanning for enemies.")]
        public readonly int ScanInterval = 25;

        [Desc("Minimal ticks in-between notifications.")]
        public readonly int NotificationInterval = 750;

        public object Create(ActorInitializer init) { return new DiscoveryNotificationWatcher(this); }
    }

    class DiscoveryNotificationWatcher : ITick
    {
        readonly DiscoveryNotificationWatcherInfo info;
		readonly HashSet<Player> discoveredPlayers;

		bool announcedAny;
		int rescanInterval;
		int ticksBeforeNextNotification;
		HashSet<uint> lastKnownActorIds;
		HashSet<uint> visibleActorIds;
		HashSet<string> playedNotifications;

        public DiscoveryNotificationWatcher(DiscoveryNotificationWatcherInfo info)
		{
			lastKnownActorIds = new HashSet<uint>();
			discoveredPlayers = new HashSet<Player>();
			this.info = info;
			rescanInterval = 0;
			ticksBeforeNextNotification = 0;
		}

		// Here self is the player actor
		public void Tick(Actor self)
		{
			if (self.Owner.Shroud.Disabled || !self.Owner.Playable || self.Owner.PlayerReference.Spectating)
				return;

			rescanInterval--;
			ticksBeforeNextNotification--;

			if (rescanInterval > 0)
				return;

			rescanInterval = info.ScanInterval;

			announcedAny = false;
			visibleActorIds = new HashSet<uint>();
			playedNotifications = new HashSet<string>();

			foreach (var actor in self.World.ActorsWithTrait<INotifyDiscovered>())
			{
				if (actor.Actor.IsDead || !actor.Actor.IsInWorld)
					continue;

				// The actor is not currently visible
				if (!self.Owner.CanViewActor(actor.Actor))
					continue;

				visibleActorIds.Add(actor.Actor.ActorID);

				// We already know about this actor
				if (lastKnownActorIds.Contains(actor.Actor.ActorID))
					continue;

				// Notify the actor that he has been discovered
				foreach (var trait in actor.Actor.TraitsImplementing<INotifyDiscovered>())
					trait.OnDiscovered(actor.Actor, self.Owner, false);

				var discoveredPlayer = actor.Actor.Owner;
				if (!discoveredPlayers.Contains(discoveredPlayer))
				{
					// Notify the actor's owner that he has been discovered
					foreach (var trait in discoveredPlayer.PlayerActor.TraitsImplementing<INotifyDiscovered>())
						trait.OnDiscovered(actor.Actor, self.Owner, false);

					discoveredPlayers.Add(discoveredPlayer);
				}
			}

			if (announcedAny)
				ticksBeforeNextNotification = info.NotificationInterval;

			lastKnownActorIds = visibleActorIds;
		}
    }
}
