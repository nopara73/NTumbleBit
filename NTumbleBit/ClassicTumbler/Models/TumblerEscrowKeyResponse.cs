using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;

namespace NTumbleBit.ClassicTumbler.Models
{
	public class TumblerEscrowKeyResponse
	{
		public int KeyIndex
		{
			get; set;
		}
		public PubKey PubKey
		{
			get; set;
		}
	}
}
