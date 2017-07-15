﻿using NTumbleBit.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace NTumbleBit.ClassicTumbler.Client.ConnectionSettings
{
	public class ConnectionSettingsBase
	{
		//Default to socks connection is safe if the cycle parameters make it impossible to have Alice and Bob both connected on a 10 minutes span
		public static ConnectionSettingsBase ParseConnectionSettings(string prefix, TextFileConfiguration config, string defaultType = "socks")
		{
			var type = config.GetOrDefault<string>(prefix + ".proxy.type", defaultType);
			if(type.Equals("none", StringComparison.OrdinalIgnoreCase))
			{
				return new ConnectionSettingsBase();
			}
			else if(type.Equals("http", StringComparison.OrdinalIgnoreCase))
			{

				var settings = new HttpConnectionSettings();
				var server = config.GetOrDefault<Uri>(prefix + ".proxy.server", null);
				if(server != null)
					settings.Proxy = server;
				var user = config.GetOrDefault<string>(prefix + ".proxy.username", null);
				var pass = config.GetOrDefault<string>(prefix + ".proxy.password", null);
				if(user != null && pass != null)
					settings.Credentials = new NetworkCredential(user, pass);
				return settings;
			}
			else if(type.Equals("socks", StringComparison.OrdinalIgnoreCase))
			{
				var settings = new SocksConnectionSettings();
				var server = config.GetOrDefault<IPEndPoint>(prefix + ".proxy.server", new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050));
				settings.Proxy = server;
				return settings;
			}
			else if(type.Equals("tor", StringComparison.OrdinalIgnoreCase))
			{
				return TorConnectionSettings.ParseConnectionSettings(prefix + ".proxy", config);
			}
			else
				throw new ConfigException(prefix + ".proxy.type is not supported, should be socks or http");
		}
		public virtual HttpMessageHandler CreateHttpHandler() => null;
	}
}
