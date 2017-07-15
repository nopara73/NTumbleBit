using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NTumbleBit
{
	public class InterruptedConsoleException : OperationCanceledException
	{
		public InterruptedConsoleException(string message) : base(message)
		{

		}
	}
	public class InterruptableConsole
	{
		private static string input;
		private static AutoResetEvent newInput = new AutoResetEvent(false);
		private static bool interrupted;

		static InterruptableConsole()
		{
			Task.Run(() =>
			{
				while(true)
				{
					input = Console.ReadLine();
					newInput.Set();
				}
			});
			Console.CancelKeyPress += (s, e) =>
			{
				Interrupt();
			};
			AssemblyLoadContext.Default.Unloading += c =>
			{
				Interrupt();
			};
		}

		private static void Interrupt()
		{
			interrupted = true;
			newInput.Set();
		}

		public static string ReadLine()
		{
			while(true)
			{
				newInput.WaitOne();
				if(interrupted)
					throw new InterruptedConsoleException("User input is interrupted");
				if(input == null)
					continue;
				return input;
			}
		}
	}
}
