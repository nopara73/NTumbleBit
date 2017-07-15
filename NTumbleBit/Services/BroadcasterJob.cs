using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NBitcoin;
using NTumbleBit.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NTumbleBit.Services
{
	public class BroadcasterJob : TumblerServiceBase
	{
		public BroadcasterJob(ExternalServices services)
		{
			BroadcasterService = services.BroadcastService;
			TrustedBroadcasterService = services.TrustedBroadcastService;
			BlockExplorerService = services.BlockExplorerService;
		}

		public IBroadcastService BroadcasterService
		{
			get;
			private set;
		}
		public ITrustedBroadcastService TrustedBroadcasterService
		{
			get;
			private set;
		}

		public IBlockExplorerService BlockExplorerService
		{
			get;
			private set;
		}

		public override string Name => "broadcaster";

		public Transaction[] TryBroadcast()
		{
			uint256[] knownBroadcasted = null;
			var broadcasted = new List<Transaction>();
			try
			{
				broadcasted.AddRange(BroadcasterService.TryBroadcast(ref knownBroadcasted));
			}
			catch(Exception ex)
			{
				Logs.Broadcasters.LogError("Exception on Broadcaster");
				Logs.Broadcasters.LogError(ex.ToString());
			}
			try
			{
				broadcasted.AddRange(TrustedBroadcasterService.TryBroadcast(ref knownBroadcasted));
			}
			catch(Exception ex)
			{
				Logs.Broadcasters.LogError("Exception on TrustedBroadcaster");
				Logs.Broadcasters.LogError(ex.ToString());
			}
			return broadcasted.ToArray();
		}

		protected override void StartCore(CancellationToken cancellationToken)
		{
			Task.Run(async () =>
			{
				Logs.Broadcasters.LogInformation("BroadcasterJob started");
				while(true)
				{
					Exception unhandled = null;
					try
					{
						var lastBlock = uint256.Zero;
						while (true)
						{
							lastBlock = await BlockExplorerService.WaitBlockAsync(lastBlock, cancellationToken).ConfigureAwait(false);
							TryBroadcast();
						}
					}
					catch(OperationCanceledException ex)
					{
						if(cancellationToken.IsCancellationRequested)
						{
							Stopped();
							break;
						}
						else
							unhandled = ex;
					}
					catch(Exception ex)
					{
						unhandled = ex;
					}
					if(unhandled != null)
					{
						Logs.Broadcasters.LogError("Uncaught exception BroadcasterJob : " + unhandled.ToString());
						await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
					}
				}
			});
		}
	}
}
