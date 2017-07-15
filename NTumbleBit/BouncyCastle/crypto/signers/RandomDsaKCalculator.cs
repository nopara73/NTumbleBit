﻿using System;

using NTumbleBit.BouncyCastle.Math;
using NTumbleBit.BouncyCastle.Security;

namespace NTumbleBit.BouncyCastle.Crypto.Signers
{
	internal class RandomDsaKCalculator
		: IDsaKCalculator
	{
		private BigInteger q;
		private SecureRandom random;

		public virtual bool IsDeterministic => false;

		public virtual void Init(BigInteger n, SecureRandom random)
		{
			q = n;
			this.random = random;
		}

		public virtual void Init(BigInteger n, BigInteger d, byte[] message)
		{
			throw new InvalidOperationException("Operation not supported");
		}

		public virtual BigInteger NextK()
		{
			var qBitLength = q.BitLength;

			BigInteger k;
			do
			{
				k = new BigInteger(qBitLength, random);
			}
			while(k.SignValue < 1 || k.CompareTo(q) >= 0);

			return k;
		}
	}
}
