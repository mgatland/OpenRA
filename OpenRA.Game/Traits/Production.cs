#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using System.Linq;

namespace OpenRA.Traits
{
	public class ProductionInfo : ITraitInfo
	{
		public readonly float[] SpawnOffsets; // in px relative to CenterLocation
		public readonly int[] ExitCells; // in cells relative to TopLeft, supports a list for multiple exits
		public readonly string[] Produces = { };
		
		public virtual object Create(ActorInitializer init) { return new Production(this); }
	}

	public class Production : ITick
	{	
		public readonly List<Pair<float2, int2>> Spawns = new List<Pair<float2, int2>>();
        private int clearAreaWaitTime;
		public Production(ProductionInfo info)
		{
			if (info.SpawnOffsets == null || info.ExitCells == null)
				return;
			
			if (info.SpawnOffsets.Length != info.ExitCells.Length)
				throw new System.InvalidOperationException("SpawnOffset, ExitCells length mismatch");
			
			for (int i = 0; i < info.ExitCells.Length; i+=2)
				Spawns.Add(Pair.New(new float2(info.SpawnOffsets[i],info.SpawnOffsets[i+1]), new int2(info.ExitCells[i], info.ExitCells[i+1])));
		}
		
		public void DoProduction(Actor self, Actor newUnit, int2 exit, float2 spawn)
		{
			var move = newUnit.Trait<IMove>();
			var facing = newUnit.TraitOrDefault<IFacing>();
			
			// Set the physical position of the unit as the exit cell
			move.SetPosition(newUnit,exit);
			var to = Util.CenterOfCell(exit);
			newUnit.CenterLocation = spawn;
			if (facing != null)
				facing.Facing = Util.GetFacing(to - spawn, facing.Facing);
			self.World.Add(newUnit);

			// Animate the spawn -> exit transition
			var speed = move.MovementSpeedForCell(self, exit);
			var length = speed > 0 ? (int)( ( to - spawn ).Length*3 / speed ) : 0;
			newUnit.QueueActivity(new Activities.Drag(spawn, to, length));
			
			// For the target line
			var target = exit;
			var rp = self.TraitOrDefault<RallyPoint>();
			if (rp != null)
			{
				target = rp.rallyPoint;
				// Todo: Move implies unit has Mobile
				newUnit.QueueActivity(new Activities.Move(target, 1));
			}
			
			if (newUnit.Owner == self.World.LocalPlayer)
			{
				self.World.AddFrameEndTask(w =>
				{
					var line = newUnit.TraitOrDefault<DrawLineToTarget>();
					if (line != null)
						line.SetTargetSilently(newUnit, Target.FromCell(target), Color.Green);
				});
			}

			foreach (var t in self.TraitsImplementing<INotifyProduction>())
				t.UnitProduced(self, newUnit, exit);

			Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);
		}
		
		public virtual bool Produce( Actor self, ActorInfo producee )
		{			
			var newUnit = self.World.CreateActor(false, producee.Name, new TypeDictionary
			{
				new OwnerInit( self.Owner ),
			});
			
			// Todo: remove assumption on Mobile; 
			// required for 3-arg CanEnterCell
			var mobile = newUnit.Trait<Mobile>();
	
			// Pick a spawn/exit point pair
			// Todo: Reorder in a synced random way
			foreach (var s in Spawns)
			{
				var exit = self.Location + s.Second;
				var spawn = self.CenterLocation + s.First;
                if (mobile.CanEnterCell(exit, self, true))
                {
                    DoProduction(self, newUnit, exit, spawn);
                    return true;
                }
                else
                {
                    ClearSpawnArea(exit, self);
                }
			}
			return false;
		}

        public void Tick(Actor self)
        {
            if (clearAreaWaitTime > 0) clearAreaWaitTime--;
        }

        private void ClearSpawnArea(int2 position, Actor self)
        {
            if (clearAreaWaitTime != 0) return;
            clearAreaWaitTime = 40;
            //Nudge every unit in the blocked cell and adjacent cells.
            for (var i = -1; i < 2; i++) {
                for (var j = -1; j < 2; j++) {
                    var pos = position + new int2(i, j);
                    var blockingUnits = self.World.WorldActor.Trait<UnitInfluence>().GetUnitsAt(pos);
                    foreach (var blockingUnit in blockingUnits) {
                        var nudgee = blockingUnit.TraitOrDefault<INudge>();
                        if (nudgee != null) nudgee.OnNudge(blockingUnit, self);
                    }
                }
            }
        }
    }
}
