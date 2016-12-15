﻿using Microsoft.Extensions.Logging;
using NTumbleBit.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if !CLIENT
namespace NTumbleBit.TumblerServer.Services
#else
namespace NTumbleBit.Client.Tumbler.Services
#endif
{
	public class BroadcasterJob
	{
		public BroadcasterJob(ExternalServices services, ILogger logger = null)
		{
			BroadcasterService = services.BroadcastService;
			TrustedBroadcasterService = services.TrustedBroadcastService;
			BlockExplorerService = services.BlockExplorerService;
			Logger = logger ?? new NullLogger();
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

		public ILogger Logger
		{
			get; set;
		}

		CancellationToken _Stop;
		public void Start(CancellationToken cancellation)
		{
			_Stop = cancellation;
			new Thread(() =>
			{
				try
				{
					int lastHeight = 0;
					while(true)
					{
						_Stop.WaitHandle.WaitOne(5000);
						_Stop.ThrowIfCancellationRequested();

						var height = BlockExplorerService.GetCurrentHeight();
						if(height == lastHeight)
							continue;
						lastHeight = height;
						try
						{
							var transactions = BroadcasterService.TryBroadcast();
							foreach(var tx in transactions)
							{
								Logger.LogInformation("Broadcaster broadcasted  " + tx.GetHash());
							}
						}
						catch(Exception ex)
						{
							Logger.LogError("Error while running Broadcaster: " + ex.Message);
							Logger.LogDebug(ex.StackTrace);
						}
						try
						{
							var transactions = TrustedBroadcasterService.TryBroadcast();
							foreach(var tx in transactions)
							{
								Logger.LogInformation("TrustedBroadcaster broadcasted " + tx.GetHash());
							}
						}
						catch(Exception ex)
						{
							Logger.LogError("Error while running TrustedBroadcaster: " + ex.Message);
							Logger.LogDebug(ex.StackTrace);
						}
					}
				}
				catch(OperationCanceledException) { }
			}).Start();
		}
	}
}