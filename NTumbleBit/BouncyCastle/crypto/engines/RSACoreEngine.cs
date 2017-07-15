﻿using System;

using NTumbleBit.BouncyCastle.Crypto.Parameters;
using NTumbleBit.BouncyCastle.Math;
using NTumbleBit.BouncyCastle.Security;

namespace NTumbleBit.BouncyCastle.Crypto.Engines
{
	/**
	* this does your basic RSA algorithm.
	*/

	internal class RsaCoreEngine
	{
		private RsaKeyParameters key;
		private bool forEncryption;
		private int bitSize;

		/**
		* initialise the RSA engine.
		*
		* @param forEncryption true if we are encrypting, false otherwise.
		* @param param the necessary RSA key parameters.
		*/
		public virtual void Init(
			bool forEncryption,
			ICipherParameters parameters)
		{
			if(!(parameters is RsaKeyParameters))
				throw new InvalidKeyException("Not an RSA key");

			key = (RsaKeyParameters)parameters;
			this.forEncryption = forEncryption;
			bitSize = key.Modulus.BitLength;
		}

		/**
		* Return the maximum size for an input block to this engine.
		* For RSA this is always one byte less than the key size on
		* encryption, and the same length as the key size on decryption.
		*
		* @return maximum size for an input block.
		*/
		public virtual int GetInputBlockSize()
		{
			if(forEncryption)
			{
				return (bitSize - 1) / 8;
			}

			return (bitSize + 7) / 8;
		}

		/**
		* Return the maximum size for an output block to this engine.
		* For RSA this is always one byte less than the key size on
		* decryption, and the same length as the key size on encryption.
		*
		* @return maximum size for an output block.
		*/
		public virtual int GetOutputBlockSize()
		{
			if(forEncryption)
			{
				return (bitSize + 7) / 8;
			}

			return (bitSize - 1) / 8;
		}

		public virtual BigInteger ConvertInput(
			byte[] inBuf,
			int inOff,
			int inLen)
		{
			var maxLength = (bitSize + 7) / 8;

			if (inLen > maxLength)
				throw new DataLengthException("input too large for RSA cipher.");

			var input = new BigInteger(1, inBuf, inOff, inLen);

			if (input.CompareTo(key.Modulus) >= 0)
				throw new DataLengthException("input too large for RSA cipher.");

			return input;
		}

		public virtual byte[] ConvertOutput(
			BigInteger result)
		{
			var output = result.ToByteArrayUnsigned();

			if (forEncryption)
			{
				var outSize = GetOutputBlockSize();

				// TODO To avoid this, create version of BigInteger.ToByteArray that
				// writes to an existing array
				if (output.Length < outSize) // have ended up with less bytes than normal, lengthen
				{
					var tmp = new byte[outSize];
					output.CopyTo(tmp, tmp.Length - output.Length);
					output = tmp;
				}
			}

			return output;
		}

		public virtual BigInteger ProcessBlock(
			BigInteger input)
		{
			//
			// we have the extra factors, use the Chinese Remainder Theorem - the author
			// wishes to express his thanks to Dirk Bonekaemper at rtsffm.com for
			// advice regarding the expression of this.
			//
			if (key is RsaPrivateCrtKeyParameters crtKey)
			{
				var p = crtKey.P;
				var q = crtKey.Q;
				var dP = crtKey.DP;
				var dQ = crtKey.DQ;
				var qInv = crtKey.QInv;

				BigInteger mP, mQ, h, m;

				// mP = ((input Mod p) ^ dP)) Mod p
				mP = (input.Remainder(p)).ModPow(dP, p);

				// mQ = ((input Mod q) ^ dQ)) Mod q
				mQ = (input.Remainder(q)).ModPow(dQ, q);

				// h = qInv * (mP - mQ) Mod p
				h = mP.Subtract(mQ);
				h = h.Multiply(qInv);
				h = h.Mod(p);               // Mod (in Java) returns the positive residual

				// m = h * q + mQ
				m = h.Multiply(q);
				m = m.Add(mQ);

				return m;
			}

			return input.ModPow(key.Exponent, key.Modulus);
		}
	}
}