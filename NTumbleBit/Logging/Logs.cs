using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTumbleBit.Logging
{
	public class Logs
	{
		static Logs()
		{
			Configure(new FuncLoggerFactory(n => NullLogger.Instance));
		}
		public static void Configure(ILoggerFactory factory)
		{
			Configuration = factory.CreateLogger(nameof(Configuration));
			Tumbler = factory.CreateLogger(nameof(Tumbler));
			Client = factory.CreateLogger(nameof(Client));
			Broadcasters = factory.CreateLogger(nameof(Broadcasters));
			Tracker = factory.CreateLogger(nameof(Tracker));
			Wallet = factory.CreateLogger(nameof(Wallet));
		}
		public static ILogger Tumbler
		{
			get; set;
		}
		public static ILogger Client
		{
			get; set;
		}
		public static ILogger Tracker
		{
			get; set;
		}
		public static ILogger Broadcasters
		{
			get; set;
		}
		public static ILogger Wallet
		{
			get; set;
		}
		public static ILogger Configuration
		{
			get; set;
		}
		public const int ColumnLength = 16;
	}
}
