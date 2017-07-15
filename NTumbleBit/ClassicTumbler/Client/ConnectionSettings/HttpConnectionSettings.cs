using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using NTumbleBit;

namespace NTumbleBit.ClassicTumbler.Client.ConnectionSettings
{
	public class HttpConnectionSettings : ConnectionSettingsBase
	{
		class CustomProxy : IWebProxy
		{
			private Uri _Address;

			public CustomProxy(Uri address)
			{
				_Address = address ?? throw new ArgumentNullException(nameof(address));
			}

			public Uri GetProxy(Uri destination) => _Address;

			public bool IsBypassed(Uri host) => false;

			public ICredentials Credentials
			{
				get; set;
			}
		}
		public Uri Proxy
		{
			get; set;
		}
		public NetworkCredential Credentials
		{
			get; set;
		}

		public override HttpMessageHandler CreateHttpHandler()
		{
			var proxy = new CustomProxy(Proxy)
			{
				Credentials = Credentials
			};
			var handler = new HttpClientHandler
			{
				Proxy = proxy
			};
			handler.SetAntiFingerprint();
			return handler;
		}
	}
}
