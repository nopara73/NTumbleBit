﻿using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Logging.Console;
using System;
using NTumbleBit.Logging;
using System.Text;
using NBitcoin.RPC;
using CommandLine;
using System.Reflection;
using NTumbleBit.ClassicTumbler;
using NTumbleBit.Configuration;
using NTumbleBit.ClassicTumbler.Client;
using NTumbleBit.ClassicTumbler.CLI;

namespace NTumbleBit.ClassicTumbler.Client.CLI
{
	public partial class Program
	{
		public static void Main(string[] args)
		{
			Logs.Configure(new FuncLoggerFactory(i => new CustomerConsoleLogger(i, (a, b) => true, false)));

			using(var interactive = new Interactive())
			{

				try
				{
					var config = new TumblerClientConfiguration();
					config.LoadArgs(args);

					var runtime = TumblerClientRuntime.FromConfiguration(config, new TextWriterClientInteraction(Console.Out, Console.In));
					interactive.Runtime = new ClientInteractiveRuntime(runtime);


					var broadcaster = runtime.CreateBroadcasterJob();
					broadcaster.Start();
					interactive.Services.Add(broadcaster);
					//interactive.Services.Add(new CheckIpService(runtime));
					//interactive.Services.Last().Start();

					if(!config.OnlyMonitor)
					{
						var stateMachine = runtime.CreateStateMachineJob();
						stateMachine.Start();
						interactive.Services.Add(stateMachine);
					}

					interactive.StartInteractive();
				}
				catch(ClientInteractionException ex)
				{
					if(!string.IsNullOrEmpty(ex.Message))
						Logs.Configuration.LogError(ex.Message);
				}
				catch(ConfigException ex)
				{
					if(!string.IsNullOrEmpty(ex.Message))
						Logs.Configuration.LogError(ex.Message);
				}
				catch(InterruptedConsoleException) { }
				catch(Exception ex)
				{
					Logs.Configuration.LogError(ex.Message);
					Logs.Configuration.LogDebug(ex.StackTrace);
				}
			}
		}
	}
}
