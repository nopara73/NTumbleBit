using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NTumbleBit.ClassicTumbler.CLI
{
	public interface IClientInteraction
	{
		Task ConfirmParametersAsync(ClassicTumblerParameters parameters, StandardCycle standardCyle);
		Task AskConnectToTorAsync(string torPath, string args);
	}

	public class AcceptAllClientInteraction : IClientInteraction
	{
		public Task AskConnectToTorAsync(string torPath, string args) => Task.CompletedTask;

		public Task ConfirmParametersAsync(ClassicTumblerParameters parameters, StandardCycle standardCyle) => Task.CompletedTask;
	}

	public class ClientInteractionException : Exception
	{
		public ClientInteractionException(string message) : base(message)
		{

		}
	}
}
