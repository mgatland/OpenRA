#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA;

namespace OpenRA.Mods.RA
{
	class DefaultShellmapScriptInfo : ITraitInfo
	{
		public object Create(Actor self) { return new DefaultShellmapScript(); }
	}

	class DefaultShellmapScript : ITick, ILoadWorldHook
	{		
		public void WorldLoaded(World w)
		{
			// Set the viewport location
			Game.MoveViewport(new int2(85,65));
		}
		
		// Rude hack around the multiple-creation bug:
		// wait long enough for the transient copies to die before starting
		int initialDelay = 20;
		public void Tick(Actor self)
		{
			if (initialDelay > 0 && --initialDelay == 0)
				Sound.PlayMusic("hell226m.aud");
		}
	}
}