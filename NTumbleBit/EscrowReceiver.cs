using NBitcoin;
using NTumbleBit.ClassicTumbler;
using NTumbleBit.PuzzleSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTumbleBit
{
    public abstract class EscrowReceiver : IEscrow
    {
		public class State
		{
			public ScriptCoin EscrowedCoin
			{
				get;
				set;
			}
			public Key EscrowKey
			{
				get;
				set;
			}
		}

		protected State InternalState
		{
			get; set;
		}

		public string Id => InternalState.EscrowedCoin.ScriptPubKey.ToHex();

		public virtual void ConfigureEscrowedCoin(ScriptCoin escrowedCoin, Key escrowKey)
		{
			InternalState.EscrowKey = escrowKey ?? throw new ArgumentNullException(nameof(escrowKey));
			InternalState.EscrowedCoin = escrowedCoin ?? throw new ArgumentNullException(nameof(escrowedCoin));
		}

		public ScriptCoin EscrowedCoin => InternalState.EscrowedCoin;
	}
}
