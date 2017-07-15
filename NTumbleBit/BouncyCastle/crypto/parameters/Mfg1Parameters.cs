﻿using System;

namespace NTumbleBit.BouncyCastle.Crypto.Parameters
{
	/// <remarks>Parameters for mask derivation functions.</remarks>
	internal class MgfParameters
	{
		private readonly byte[] seed;

		public MgfParameters(
			byte[] seed)
			: this(seed, 0, seed.Length)
		{
		}

		public MgfParameters(
			byte[] seed,
			int off,
			int len)
		{
			this.seed = new byte[len];
			Array.Copy(seed, off, this.seed, 0, len);
		}

		public byte[] GetSeed() => (byte[])seed.Clone();
	}
}