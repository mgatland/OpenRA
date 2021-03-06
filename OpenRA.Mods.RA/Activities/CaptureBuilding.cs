#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class CaptureBuilding : IActivity
	{
		Actor target;

		public CaptureBuilding(Actor target) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead()) return NextActivity;

			target.World.AddFrameEndTask(w =>
			{
				// momentarily remove from world so the ownership queries don't get confused
				var oldOwner = target.Owner;
				w.Remove(target);
				target.Owner = self.Owner;
				w.Add(target);
				
				foreach (var t in target.TraitsImplementing<INotifyCapture>())
					t.OnCapture(target, self, oldOwner, self.Owner);

				self.Destroy();
			});
			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
