﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using NTumbleBit.Services;
using System.Threading;
using NTumbleBit.Logging;
using NTumbleBit.ClassicTumbler;
using NTumbleBit.Configuration;
using NTumbleBit.ClassicTumbler.Server;
using NTumbleBit.ClassicTumbler.CLI;

namespace NTumbleBit.ClassicTumbler.Server.CLI
{
	public partial class Program
	{
		public static void Main(string[] args)
		{
			new Program().Run(args);
		}
		public void Run(string[] args)
		{
			Logs.Configure(new FuncLoggerFactory(i => new ConsoleLogger(i, (a, b) => true, false)));

			using(var interactive = new Interactive())
			{
				var config = new TumblerConfiguration();
				config.LoadArgs(args);
				try
				{
					var runtime = TumblerRuntime.FromConfiguration(config);

					IWebHost host = null;
					if(!config.OnlyMonitor)
					{
						host = new WebHostBuilder()
						.UseKestrel()
						.UseAppConfiguration(runtime)
						.UseContentRoot(Directory.GetCurrentDirectory())
						.UseStartup<Startup>()
						.UseUrls(config.GetUrls())
						.Build();
					}

					var job = new BroadcasterJob(interactive.Runtime.Services, Logs.Main);
					job.Start(interactive.BroadcasterCancellationToken);
					Logs.Main.LogInformation("BroadcasterJob started");

					if(!config.OnlyMonitor)
						new Thread(() =>
						{
							try
							{
								host.Run(interactive.MixingCancellationToken);
							}
							catch(Exception ex)
							{
								if(!interactive.MixingCancellationToken.IsCancellationRequested)
									Logs.Server.LogCritical(1, ex, "Error while starting the host");
							}
							if(interactive.MixingCancellationToken.IsCancellationRequested)
								Logs.Server.LogInformation("Server stopped");
						}).Start();
					interactive.StartInteractive();
				}
				catch(ConfigException ex)
				{
					if(!string.IsNullOrEmpty(ex.Message))
						Logs.Main.LogError(ex.Message);
				}
				catch(Exception exception)
				{
					Logs.Main.LogError("Exception thrown while running the server");
					Logs.Main.LogError(exception.ToString());
				}
			}
		}
	}
}

