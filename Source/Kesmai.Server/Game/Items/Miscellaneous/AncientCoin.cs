﻿using System;
using System.Collections.Generic;
using System.IO;
using Kesmai.Server.Game;
using Kesmai.Server.Network;

namespace Kesmai.Server.Items
{
	public partial class AncientCoin : ItemEntity, ITreasure
	{
		/// <inheritdoc />
		public override int Weight => 5;

		/// <summary>
		/// Gets the label number.
		/// </summary>
		public override int LabelNumber => 6000028;

		/// <summary>
		/// Initializes a new instance of the <see cref="AncientCoin"/> class.
		/// </summary>
		[WorldForge]
		public AncientCoin() : base(73)
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="AncientCoin"/> class.
		/// </summary>
		public AncientCoin(Serial serial) : base(serial)
		{
		}

		/// <inheritdoc />
		public override void GetDescription(List<LocalizationEntry> entries)
		{
			entries.Add(new LocalizationEntry(6200000, 6200307)); /* [You are looking at] [an old piece of currency, the face of a forgotten monarch engraved upon it.] */
		}
	}
}