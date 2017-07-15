﻿using System;

using NTumbleBit.BouncyCastle.Utilities;

namespace NTumbleBit.BouncyCastle.Asn1
{
	/**
     * ASN.1 TaggedObject - in ASN.1 notation this is any object preceded by
     * a [n] where n is some number - these are assumed to follow the construction
     * rules (as with sequences).
     */

	internal abstract class Asn1TaggedObject
		: Asn1Object
	{
		internal int tagNo;
		//        internal bool           empty;
		internal bool explicitly;
		internal Asn1Encodable obj;

		public static Asn1TaggedObject GetInstance(
			Asn1TaggedObject obj,
			bool explicitly)
		{
			if(explicitly)
			{
				return (Asn1TaggedObject)obj.GetObject();
			}

			throw new ArgumentException("implicitly tagged tagged object");
		}

		public static Asn1TaggedObject GetInstance(
			object obj)
		{
			if(obj == null || obj is Asn1TaggedObject)
			{
				return (Asn1TaggedObject)obj;
			}

			throw new ArgumentException("Unknown object in GetInstance: " + obj.GetType().FullName, nameof(obj));
		}

		/**
         * @param tagNo the tag number for this object.
         * @param obj the tagged object.
         */
		protected Asn1TaggedObject(
			int tagNo,
			Asn1Encodable obj)
		{
			explicitly = true;
			this.tagNo = tagNo;
			this.obj = obj;
		}

		protected override bool Asn1Equals(
			Asn1Object asn1Object)
		{
			var other = asn1Object as Asn1TaggedObject;

			if(other == null)
				return false;

			return tagNo == other.tagNo
				//				&& this.empty == other.empty
				&& explicitly == other.explicitly   // TODO Should this be part of equality?
				&& Platform.Equals(GetObject(), other.GetObject());
		}

		protected override int Asn1GetHashCode()
		{
			var code = tagNo.GetHashCode();

			// TODO: actually this is wrong - the problem is that a re-encoded
			// object may end up with a different hashCode due to implicit
			// tagging. As implicit tagging is ambiguous if a sequence is involved
			// it seems the only correct method for both equals and hashCode is to
			// compare the encodings...
			//			code ^= explicitly.GetHashCode();

			if (obj != null)
			{
				code ^= obj.GetHashCode();
			}

			return code;
		}

		public int TagNo => tagNo;

		/**
         * return whether or not the object may be explicitly tagged.
         * <p>
         * Note: if the object has been read from an input stream, the only
         * time you can be sure if isExplicit is returning the true state of
         * affairs is if it returns false. An implicitly tagged object may appear
         * to be explicitly tagged, so you need to understand the context under
         * which the reading was done as well, see GetObject below.</p>
         */
		public bool IsExplicit() => explicitly;

		public static bool IsEmpty() => false;

		/**
         * return whatever was following the tag.
         * <p>
         * Note: tagged objects are generally context dependent if you're
         * trying to extract a tagged object you should be going via the
         * appropriate GetInstance method.</p>
         */
		public Asn1Object GetObject()
		{
			if(obj != null)
			{
				return obj.ToAsn1Object();
			}

			return null;
		}

		public override string ToString() => "[" + tagNo + "]" + obj;
	}
}