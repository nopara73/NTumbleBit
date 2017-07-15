using System;
using System.IO;
using System.Text;

using NTumbleBit.BouncyCastle.Math;
using NTumbleBit.BouncyCastle.Utilities;

namespace NTumbleBit.BouncyCastle.Asn1
{
	internal class DerObjectIdentifier
		: Asn1Object
	{
		private readonly string identifier;

		private byte[] body = null;

		public DerObjectIdentifier(
			string identifier)
		{
			if(identifier == null)
				throw new ArgumentNullException(nameof(identifier));
			if(!IsValidIdentifier(identifier))
				throw new FormatException("string " + identifier + " not an OID");

			this.identifier = identifier;
		}

		internal DerObjectIdentifier(DerObjectIdentifier oid, string branchID)
		{
			if(!IsValidBranchID(branchID, 0))
				throw new ArgumentException("string " + branchID + " not a valid OID branch", nameof(branchID));

			identifier = oid.Id + "." + branchID;
		}

		// TODO Change to ID?
		public string Id => identifier;

		public virtual DerObjectIdentifier Branch(string branchID) => new DerObjectIdentifier(this, branchID);

		/**
         * Return  true if this oid is an extension of the passed in branch, stem.
         * @param stem the arc or branch that is a possible parent.
         * @return  true if the branch is on the passed in stem, false otherwise.
         */
		public virtual bool On(DerObjectIdentifier stem)
		{
			var id = Id;
			var stemId = stem.Id;
			return id.Length > stemId.Length && id[stemId.Length] == '.' && Platform.StartsWith(id, stemId);
		}

		internal DerObjectIdentifier(byte[] bytes)
		{
			identifier = MakeOidStringFromBytes(bytes);
			body = Arrays.Clone(bytes);
		}

		private static void WriteField(
			Stream outputStream,
			long fieldValue)
		{
			var result = new byte[9];
			var pos = 8;
			result[pos] = (byte)(fieldValue & 0x7f);
			while(fieldValue >= (1L << 7))
			{
				fieldValue >>= 7;
				result[--pos] = (byte)((fieldValue & 0x7f) | 0x80);
			}
			outputStream.Write(result, pos, 9 - pos);
		}

		private static void WriteField(
			Stream outputStream,
			BigInteger fieldValue)
		{
			var byteCount = (fieldValue.BitLength + 6) / 7;
			if (byteCount == 0)
			{
				outputStream.WriteByte(0);
			}
			else
			{
				var tmpValue = fieldValue;
				var tmp = new byte[byteCount];
				for (int i = byteCount - 1; i >= 0; i--)
				{
					tmp[i] = (byte)((tmpValue.IntValue & 0x7f) | 0x80);
					tmpValue = tmpValue.ShiftRight(7);
				}
				tmp[byteCount - 1] &= 0x7f;
				outputStream.Write(tmp, 0, tmp.Length);
			}
		}

		protected override int Asn1GetHashCode() => identifier.GetHashCode();

		protected override bool Asn1Equals(
			Asn1Object asn1Object)
		{
			var other = asn1Object as DerObjectIdentifier;

			if (other == null)
				return false;

			return identifier.Equals(other.identifier);
		}

		public override string ToString() => identifier;

		private static bool IsValidBranchID(
			String branchID, int start)
		{
			var periodAllowed = false;

			var pos = branchID.Length;
			while (--pos >= start)
			{
				var ch = branchID[pos];

				// TODO Leading zeroes?
				if ('0' <= ch && ch <= '9')
				{
					periodAllowed = true;
					continue;
				}

				if(ch == '.')
				{
					if(!periodAllowed)
						return false;

					periodAllowed = false;
					continue;
				}

				return false;
			}

			return periodAllowed;
		}

		private static bool IsValidIdentifier(string identifier)
		{
			if(identifier.Length < 3 || identifier[1] != '.')
				return false;

			var first = identifier[0];
			if (first < '0' || first > '2')
				return false;

			return IsValidBranchID(identifier, 2);
		}

		private const long LONG_LIMIT = (long.MaxValue >> 7) - 0x7f;

		private static string MakeOidStringFromBytes(
			byte[] bytes)
		{
			var objId = new StringBuilder();
			long value = 0;
			BigInteger bigValue = null;
			var first = true;

			for (int i = 0; i != bytes.Length; i++)
			{
				int b = bytes[i];

				if(value <= LONG_LIMIT)
				{
					value += (b & 0x7f);
					if((b & 0x80) == 0)             // end of number reached
					{
						if(first)
						{
							if(value < 40)
							{
								objId.Append('0');
							}
							else if(value < 80)
							{
								objId.Append('1');
								value -= 40;
							}
							else
							{
								objId.Append('2');
								value -= 80;
							}
							first = false;
						}

						objId.Append('.');
						objId.Append(value);
						value = 0;
					}
					else
					{
						value <<= 7;
					}
				}
				else
				{
					if(bigValue == null)
					{
						bigValue = BigInteger.ValueOf(value);
					}
					bigValue = bigValue.Or(BigInteger.ValueOf(b & 0x7f));
					if((b & 0x80) == 0)
					{
						if(first)
						{
							objId.Append('2');
							bigValue = bigValue.Subtract(BigInteger.ValueOf(80));
							first = false;
						}

						objId.Append('.');
						objId.Append(bigValue);
						bigValue = null;
						value = 0;
					}
					else
					{
						bigValue = bigValue.ShiftLeft(7);
					}
				}
			}

			return objId.ToString();
		}

		private static readonly DerObjectIdentifier[] cache = new DerObjectIdentifier[1024];

		internal byte[] GetBody()
		{
			lock(this)
			{
				if(body == null)
				{
					var bOut = new MemoryStream();
					DoOutput(bOut);
					body = bOut.ToArray();
				}
			}

			return body;
		}

		private void DoOutput(MemoryStream bOut)
		{
			var tok = new OidTokenizer(identifier);

			var token = tok.NextToken();
			var first = int.Parse(token) * 40;

			token = tok.NextToken();
			if(token.Length <= 18)
			{
				WriteField(bOut, first + Int64.Parse(token));
			}
			else
			{
				WriteField(bOut, new BigInteger(token).Add(BigInteger.ValueOf(first)));
			}

			while(tok.HasMoreTokens)
			{
				token = tok.NextToken();
				if(token.Length <= 18)
				{
					WriteField(bOut, Int64.Parse(token));
				}
				else
				{
					WriteField(bOut, new BigInteger(token));
				}
			}
		}

		internal override void Encode(DerOutputStream derOut)
		{
			derOut.WriteEncoded(Asn1Tags.ObjectIdentifier, GetBody());
		}

		internal static Asn1Object FromOctetString(byte[] enc)
		{
			var hashCode = Arrays.GetHashCode(enc);
			var first = hashCode & 1023;

			lock (cache)
			{
				var entry = cache[first];
				if (entry != null && Arrays.AreEqual(enc, entry.GetBody()))
				{
					return entry;
				}

				return cache[first] = new DerObjectIdentifier(enc);
			}
		}
	}
}
