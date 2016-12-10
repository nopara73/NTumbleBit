﻿using NBitcoin;
using NBitcoin.Crypto;
using NTumbleBit.PuzzlePromise;
using NTumbleBit.PuzzleSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTumbleBit.ClassicTumbler
{
	public class ServerChannelNegotiation
	{

		public ServerChannelNegotiation(ClassicTumblerParameters parameters, RsaKey tumblerKey, RsaKey voucherKey)
		{
			if(tumblerKey == null)
				throw new ArgumentNullException("tumblerKey");
			if(voucherKey == null)
				throw new ArgumentNullException("voucherKey");
			if(parameters.VoucherKey != voucherKey.PubKey)
				throw new ArgumentException("Voucher key does not match");
			if(parameters.ServerKey != tumblerKey.PubKey)
				throw new ArgumentException("Tumbler key does not match");
			TumblerKey = tumblerKey;
			VoucherKey = voucherKey;
			Parameters = parameters;
		}

		public RsaKey TumblerKey
		{
			get;
			private set;
		}
		public RsaKey VoucherKey
		{
			get;
			private set;
		}

		public ClassicTumblerParameters Parameters
		{
			get; set;
		}
	}
	public class TumblerAliceServerSession : ServerChannelNegotiation
	{
		State InternalState
		{
			get; set;
		}

		public class State
		{
			public State()
			{
			}
					
			public PuzzleValue UnsignedVoucher
			{
				get; set;
			}
			public Key EscrowKey
			{
				get; set;
			}
			public PubKey OtherEscrowKey
			{
				get; set;
			}
			public PubKey RedeemKey
			{
				get; set;
			}
			public int CycleStart
			{
				get;
				set;
			}
		}

		public State GetInternalState()
		{
			var state =  Serializer.Clone(InternalState);
			return state;
		}

		public TumblerAliceServerSession(ClassicTumblerParameters parameters,
										RsaKey tumblerKey,
										RsaKey voucherKey) : base(parameters, tumblerKey, voucherKey)
		{
			if(parameters == null)
				throw new ArgumentNullException("parameters");
			InternalState = new State();
		}

		public TumblerAliceServerSession(ClassicTumblerParameters parameters,
										RsaKey tumblerKey,
										RsaKey voucherKey,
										State state) : base(parameters, tumblerKey, voucherKey)
		{
			if(parameters == null)
				throw new ArgumentNullException("parameters");
			InternalState = Serializer.Clone(state);
		}

		public PubKey ReceiveAliceEscrowInformation(ClientEscrowInformation escrowInformation)
		{
			var cycle = Parameters.CycleGenerator.GetCycle(escrowInformation.Cycle);
			InternalState.CycleStart = cycle.Start;
			InternalState.EscrowKey = new Key();
			InternalState.OtherEscrowKey = escrowInformation.EscrowKey;
			InternalState.RedeemKey = escrowInformation.RedeemKey;
			InternalState.UnsignedVoucher = escrowInformation.UnsignedVoucher;
			return InternalState.EscrowKey.PubKey;
		}

		public TxOut BuildEscrowTxOut()
		{
			return new TxOut(Parameters.Denomination + Parameters.Fee, CreateEscrowScript().Hash);
		}

		public string GetChannelId()
		{
			return CreateEscrowScript().Hash.ScriptPubKey.ToHex();
		}

		private Script CreateEscrowScript()
		{
			return EscrowScriptBuilder.CreateEscrow(new[] { InternalState.EscrowKey.PubKey, InternalState.OtherEscrowKey }, InternalState.RedeemKey, GetCycle().GetClientLockTime());
		}

		public SolverServerSession ConfirmAliceEscrow(Transaction transaction, out PuzzleSolution solvedVoucher)
		{
			solvedVoucher = null;
			var escrow = CreateEscrowScript();
			var coin = transaction.Outputs.AsCoins().FirstOrDefault(txout => txout.ScriptPubKey == escrow.Hash.ScriptPubKey);
			if(coin == null)
				throw new PuzzleException("No output containing the escrowed coin");
			if(coin.Amount != Parameters.Denomination + Parameters.Fee)
				throw new PuzzleException("Incorrect amount");
			var voucher = InternalState.UnsignedVoucher;			
			var escrowedCoin = coin.ToScriptCoin(escrow);

			var session = new SolverServerSession(this.TumblerKey, this.Parameters.CreateSolverParamaters());				
			session.ConfigureEscrowedCoin(escrowedCoin, InternalState.EscrowKey);
			InternalState.UnsignedVoucher = null;
			InternalState.OtherEscrowKey = null;
			InternalState.RedeemKey = null;
			InternalState.EscrowKey = null;
			solvedVoucher = voucher.WithRsaKey(VoucherKey.PubKey).Solve(VoucherKey);
			return session;
		}		

		public CycleParameters GetCycle()
		{
			return Parameters.CycleGenerator.GetCycle(InternalState.CycleStart);
		}
	}

	public enum TumblerBobStates
	{
		WaitingVoucherRequest,
		WaitingBobEscrowInformation,
		WaitingSignedTransaction,
		Completed
	}
	public class TumblerBobServerSession : ServerChannelNegotiation
	{
		State InternalState
		{
			get; set;
		}

		public class State
		{
			public State()
			{
			}
			public Key RedeemKey
			{
				get; set;
			}
			public Key EscrowKey
			{
				get; set;
			}
			public int CycleStart
			{
				get;
				set;
			}			

			public TumblerBobStates Status
			{
				get;
				set;
			}			
			public uint160 VoucherHash
			{
				get;
				set;
			}
			public PubKey OtherEscrowKey
			{
				get;
				set;
			}
		}

		public CycleParameters GetCycle()
		{
			return Parameters.CycleGenerator.GetCycle(InternalState.CycleStart);
		}

		public TumblerBobServerSession(ClassicTumblerParameters parameters,
										RsaKey tumblerKey,
										RsaKey voucherKey,
										int cycleStart) : base(parameters, tumblerKey, voucherKey)
		{
			if(parameters == null)
				throw new ArgumentNullException("parameters");
			InternalState = new State();
			InternalState.CycleStart = cycleStart;
		}

		public TumblerBobServerSession(ClassicTumblerParameters parameters,
										RsaKey tumblerKey,
										RsaKey voucherKey,
										State state) : base(parameters, tumblerKey, voucherKey)
		{
			if(parameters == null)
				throw new ArgumentNullException("parameters");
			InternalState = Serializer.Clone(state);
		}
		
		public State GetInternalState()
		{
			var state = Serializer.Clone(InternalState);
			return state;
		}
		
		public PuzzleValue GenerateUnsignedVoucher(ref PuzzleSolution solution)
		{
			AssertState(TumblerBobStates.WaitingVoucherRequest);
			var puzzle = Parameters.VoucherKey.GeneratePuzzle(ref solution);
			InternalState.VoucherHash = Hashes.Hash160(solution.ToBytes());
			InternalState.Status = TumblerBobStates.WaitingBobEscrowInformation;
			return puzzle.PuzzleValue;
		}		

		public void ReceiveBobEscrowInformation(BobEscrowInformation bobEscrowInformation)
		{
			if(bobEscrowInformation == null)
				throw new ArgumentNullException("bobKey");
			AssertState(TumblerBobStates.WaitingBobEscrowInformation);
			if(Hashes.Hash160(bobEscrowInformation.SignedVoucher.ToBytes()) != InternalState.VoucherHash)
				throw new PuzzleException("Incorrect voucher");

			var escrow = new Key();
			var redeem = new Key();
			InternalState.EscrowKey = escrow;
			InternalState.OtherEscrowKey = bobEscrowInformation.EscrowKey;
			InternalState.RedeemKey = redeem;
			InternalState.VoucherHash = null;
			InternalState.Status = TumblerBobStates.WaitingSignedTransaction;
		}

		public TxOut BuildEscrowTxOut()
		{
			AssertState(TumblerBobStates.WaitingSignedTransaction);
			var escrowScript = CreateEscrowScript();
			return new TxOut(Parameters.Denomination, escrowScript.Hash);
		}

		private Script CreateEscrowScript()
		{
			return EscrowScriptBuilder.CreateEscrow(
				new[]
				{
					InternalState.EscrowKey.PubKey,
					InternalState.OtherEscrowKey
				},
				InternalState.RedeemKey.PubKey,
				GetCycle().GetTumblerLockTime());
		}

		public PromiseServerSession SetSignedTransaction(Transaction transaction)
		{
			AssertState(TumblerBobStates.WaitingSignedTransaction);
			var escrow = BuildEscrowTxOut();
			var output = transaction.Outputs.AsIndexedOutputs()
				.Single(o => o.TxOut.ScriptPubKey == escrow.ScriptPubKey && o.TxOut.Value == escrow.Value);
			var escrowedCoin = new Coin(output).ToScriptCoin(CreateEscrowScript());			
			PromiseServerSession session = new PromiseServerSession(Parameters.CreatePromiseParamaters());
			session.ConfigureEscrowedCoin(escrowedCoin, InternalState.EscrowKey, InternalState.RedeemKey);
			InternalState.EscrowKey = null;
			InternalState.RedeemKey = null;
			InternalState.Status = TumblerBobStates.Completed;			
			return session;
		}

		private void AssertState(TumblerBobStates state)
		{
			if(state != InternalState.Status)
				throw new InvalidOperationException("Invalid state, actual " + InternalState.Status + " while expected is " + state);
		}
	}
}