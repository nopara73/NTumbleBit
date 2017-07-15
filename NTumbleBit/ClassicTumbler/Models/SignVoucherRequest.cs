﻿using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;

namespace NTumbleBit.ClassicTumbler.Models
{
	public class SignVoucherRequest
	{
		public int Cycle
		{
			get; set;
		}
		public int KeyReference
		{
			get; set;
		}
		public PuzzleValue UnsignedVoucher
		{
			get; set;
		}
		public MerkleBlock MerkleProof
		{
			get; set;
		}
		public PubKey ClientEscrowKey
		{
			get; set;
		}
		public Transaction Transaction
		{
			get; set;
		}
	}
}
