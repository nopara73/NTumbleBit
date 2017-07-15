namespace NTumbleBit.BouncyCastle.Asn1
{
	internal abstract class Asn1Encodable
		: IAsn1Convertible
	{
		public const string Der = "DER";
		public const string Ber = "BER";

		public sealed override int GetHashCode() => ToAsn1Object().CallAsn1GetHashCode();

		public sealed override bool Equals(
			object obj)
		{
			if(obj == this)
				return true;

			var other = obj as IAsn1Convertible;

			if(other == null)
				return false;

			var o1 = ToAsn1Object();
			var o2 = other.ToAsn1Object();

			return o1 == o2 || o1.CallAsn1Equals(o2);
		}

		public abstract Asn1Object ToAsn1Object();
	}
}
