﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderWarFactoryInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public object Create(ActorInitializer init) { return new RenderWarFactory(init.self); }
	}

	class RenderWarFactory : INotifyBuildComplete, INotifyDamage, ITick, INotifyProduction, INotifySold
	{
		public Animation roof;
		[Sync]
		bool isOpen;
		[Sync]
		int2 openExit;
		
		string GetPrefix(Actor self)
		{
			return self.GetDamageState() >= DamageState.Heavy ? "damaged-" : "";
		}

		public RenderWarFactory(Actor self)
		{
			roof = new Animation(self.Trait<RenderSimple>().GetImage(self));
		}

		public void BuildingComplete( Actor self )
		{
			roof.Play( GetPrefix(self) + "idle-top" );
			self.Trait<RenderSimple>().anims.Add( "roof", new RenderSimple.AnimationWithOffset( roof ) { ZOffset = 2 } );
		}

		public void Tick(Actor self)
		{
			if (isOpen && !self.World.WorldActor.Trait<UnitInfluence>()
				.GetUnitsAt(openExit).Any())
			{
				isOpen = false;
				roof.PlayBackwardsThen(GetPrefix(self) + "build-top", () => roof.Play(GetPrefix(self) + "idle-top"));
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;
			
			if (e.DamageState >= DamageState.Heavy)
				roof.ReplaceAnim("damaged-" + roof.CurrentSequence.Name);
			else
				roof.ReplaceAnim(roof.CurrentSequence.Name.Replace("damaged-",""));
		}

		public void UnitProduced(Actor self, Actor other, int2 exit)
		{
			roof.PlayThen(GetPrefix(self) + "build-top", () => {isOpen = true; openExit = exit;});
		}

		public void Selling( Actor self )
		{
			self.Trait<RenderSimple>().anims.Remove( "roof" );
		}

		public void Sold( Actor self ) { }
	}
}
