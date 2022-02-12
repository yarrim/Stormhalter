﻿using System.Collections.Generic;
using System.IO;
using Kesmai.Server.Game;
using Kesmai.Server.Network;
using Kesmai.Server.Spells;

namespace Kesmai.Server.Items
{
	public partial class ThreeFiftyTwoRing : Ring, ITreasure
	{
		/// <summary>
		/// Gets the price.
		/// </summary>
		public override uint BasePrice => 600;

		/// <summary>
		/// Gets the weight.
		/// </summary>
		public override int Weight => 20;

		/// <summary>
		/// Initializes a new instance of the <see cref="ThreeFiftyTwoRing"/> class.
		/// </summary>
		public ThreeFiftyTwoRing() : base(352)
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ThreeFiftyTwoRing"/> class.
		/// </summary>
		public ThreeFiftyTwoRing(Serial serial) : base(serial)
		{
		}

		/// <summary>
		/// Gets the description for this instance.
		/// </summary>
		public override void GetDescription(List<LocalizationEntry> entries)
		{
			entries.Add(new LocalizationEntry(6200000, 6200323)); /* [You are looking at] [a bronze ring with a dangerous spike facing outward.] */

			if (Identified)
				entries.Add(new LocalizationEntry(6250041)); /* The ring contains the spell of Blind Resistance. */
		}

		protected override bool OnEquip(MobileEntity entity)
		{
			if (!base.OnEquip(entity))
				return false;

			if (!entity.GetStatus(typeof(BlindResistanceStatus), out var resistance))
			{
				resistance = new BlindResistanceStatus(entity)
				{
					Inscription = new SpellInscription() { SpellId = 47 }
				};
				resistance.AddSource(new ItemSource(this));

				entity.AddStatus(resistance);
			}
			else
			{
				resistance.AddSource(new ItemSource(this));
			}

			return true;
		}

		protected override bool OnUnequip(MobileEntity entity)
		{
			if (!base.OnUnequip(entity))
				return false;

			if (entity.GetStatus(typeof(BlindResistanceStatus), out var resistance))
				resistance.RemoveSourceFor(this);

			return true;
		}
	}
}