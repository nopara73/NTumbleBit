using NBitcoin;
using NTumbleBit.ClassicTumbler;
using NTumbleBit.ClassicTumbler.Models;
using NTumbleBit.PuzzlePromise;
using NTumbleBit.PuzzleSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NTumbleBit;
using System.IO;
using NTumbleBit.Logging;
using Microsoft.Extensions.Logging;

namespace NTumbleBit.ClassicTumbler.Client
{
	public class TumblerClient
	{
		public TumblerClient(Network network, Uri serverAddress, Identity identity)
		{
			_Address = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
			_Network = network ?? throw new ArgumentNullException(nameof(network));
			_identity = identity;
			ClassicTumblerParameters.ExtractHashFromUrl(serverAddress); //Validate
		}

		private Identity _identity;

		private readonly Network _Network;
		public Network Network => _Network;

		private readonly Uri _Address;
		
		public Task<ClassicTumblerParameters> GetTumblerParametersAsync() => GetAsync<ClassicTumblerParameters>($"parameters");

		public ClassicTumblerParameters GetTumblerParameters() => GetTumblerParametersAsync().GetAwaiter().GetResult();

		private Task<T> GetAsync<T>(string relativePath, params object[] parameters) => SendAsync<T>(HttpMethod.Get, null, relativePath, parameters);

		public UnsignedVoucherInformation AskUnsignedVoucher() => AskUnsignedVoucherAsync().GetAwaiter().GetResult();

		public Task<UnsignedVoucherInformation> AskUnsignedVoucherAsync() => GetAsync<UnsignedVoucherInformation>($"vouchers/");

		public Task<PuzzleSolution> SignVoucherAsync(SignVoucherRequest signVoucherRequest) => SendAsync<PuzzleSolution>(HttpMethod.Post, signVoucherRequest, $"clientchannels/confirm");

		public PuzzleSolution SignVoucher(SignVoucherRequest signVoucherRequest) => SignVoucherAsync(signVoucherRequest).GetAwaiter().GetResult();

		public Task<ScriptCoin> OpenChannelAsync(OpenChannelRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));
			return SendAsync<ScriptCoin>(HttpMethod.Post, request, $"channels/");
		}

		public ScriptCoin OpenChannel(OpenChannelRequest request) => OpenChannelAsync(request).GetAwaiter().GetResult();

		public Task<TumblerEscrowKeyResponse> RequestTumblerEscrowKeyAsync() => SendAsync<TumblerEscrowKeyResponse>(HttpMethod.Post, _identity.CycleId, $"clientchannels/");

		public TumblerEscrowKeyResponse RequestTumblerEscrowKey() => RequestTumblerEscrowKeyAsync().GetAwaiter().GetResult();

		private string GetFullUri(string relativePath, params object[] parameters)
		{
			relativePath = String.Format(relativePath, parameters ?? new object[0]);
			var uri = _Address.AbsoluteUri;
			if (!uri.EndsWith("/", StringComparison.Ordinal))
				uri += "/";
			uri += relativePath;
			return uri;
		}

		public static Identity LastUsedIdentity { get; private set; } = Identity.DoesntMatter;

		private async Task<T> SendAsync<T>(HttpMethod method, object body, string relativePath, params object[] parameters)
		{
			var uri = GetFullUri(relativePath, parameters);
			var message = new HttpRequestMessage(method, uri);
			if (body != null)
			{
				message.Content = new StringContent(Serializer.ToString(body, Network), Encoding.UTF8, "application/json");
			}

			if (Tor.UseTor)
			{
				// torchangelog.txt for testing only, before merge to master it should be deleted
				if (_identity == new Identity(Role.Alice, -1))
				{
					File.AppendAllText("torchangelog.txt", Environment.NewLine + Environment.NewLine + "//RESTART" + Environment.NewLine);
				}

				if (_identity != LastUsedIdentity)
				{
					var start = DateTime.Now;
					Logs.Client.LogInformation($"Changing identity to {_identity}");
					await Tor.ControlPortClient.ChangeCircuitAsync().ConfigureAwait(false);
					var takelong = DateTime.Now - start;
					File.AppendAllText("torchangelog.txt", Environment.NewLine + Environment.NewLine + $"CHANGE IP: {(int)takelong.TotalSeconds} sec" + Environment.NewLine);
				}
				LastUsedIdentity = _identity;
				File.AppendAllText("torchangelog.txt", '\t' + _identity.ToString() + Environment.NewLine);
				File.AppendAllText("torchangelog.txt", '\t' + message.Method.Method + " " + message.RequestUri.AbsolutePath + Environment.NewLine);
			}

			HttpResponseMessage result;
			try
			{
				result = await Tor.HttpClient.SendAsync(message).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				File.AppendAllText("torchangelog.txt", ex.ToString() + Environment.NewLine);
				throw;
			}
			if(result.StatusCode != HttpStatusCode.OK)
			{
				File.AppendAllText("torchangelog.txt", message.ToHttpStringAsync() + Environment.NewLine);
				File.AppendAllText("torchangelog.txt", result.ToHttpStringAsync() + Environment.NewLine);
			}

			if (result.StatusCode == HttpStatusCode.NotFound)
				return default(T);
			if (!result.IsSuccessStatusCode)
			{
				var error = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (!string.IsNullOrEmpty(error))
				{
					throw new HttpRequestException(result.StatusCode + ": " + error);
				}
			}
			result.EnsureSuccessStatusCode();
			if (typeof(T) == typeof(byte[]))
				return (T)(object)await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			var str = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
			if (typeof(T) == typeof(string))
				return (T)(object)str;
			return Serializer.ToObject<T>(str, Network);
		}

		public ServerCommitmentsProof CheckRevelation(string channelId, PuzzlePromise.ClientRevelation revelation) => CheckRevelationAsync(channelId, revelation).GetAwaiter().GetResult();

		private Task<ServerCommitmentsProof> CheckRevelationAsync(string channelId, PuzzlePromise.ClientRevelation revelation) => SendAsync<ServerCommitmentsProof>(HttpMethod.Post, revelation, $"channels/{_identity.CycleId}/{channelId}/checkrevelation");

		public Task<PuzzlePromise.ServerCommitment[]> SignHashesAsync(string channelId, SignaturesRequest sigReq) => SendAsync<PuzzlePromise.ServerCommitment[]>(HttpMethod.Post, sigReq, $"channels/{_identity.CycleId}/{channelId}/signhashes");

		public SolutionKey[] CheckRevelation(string channelId, PuzzleSolver.ClientRevelation revelation) => CheckRevelationAsync(channelId, revelation).GetAwaiter().GetResult();

		public Task<SolutionKey[]> CheckRevelationAsync(string channelId, PuzzleSolver.ClientRevelation revelation) => SendAsync<SolutionKey[]>(HttpMethod.Post, revelation, $"clientschannels/{_identity.CycleId}/{channelId}/checkrevelation");

		public OfferInformation CheckBlindFactors(string channelId, BlindFactor[] blindFactors) => CheckBlindFactorsAsync(channelId, blindFactors).GetAwaiter().GetResult();

		public Task<OfferInformation> CheckBlindFactorsAsync(string channelId, BlindFactor[] blindFactors) => SendAsync<OfferInformation>(HttpMethod.Post, blindFactors, $"clientschannels/{_identity.CycleId}/{channelId}/checkblindfactors");

		public PuzzleSolver.ServerCommitment[] SolvePuzzles(string channelId, PuzzleValue[] puzzles) => SolvePuzzlesAsync(channelId, puzzles).GetAwaiter().GetResult();

		public Task<PuzzleSolver.ServerCommitment[]> SolvePuzzlesAsync(string channelId, PuzzleValue[] puzzles) => SendAsync<PuzzleSolver.ServerCommitment[]>(HttpMethod.Post, puzzles, $"clientchannels/{_identity.CycleId}/{channelId}/solvepuzzles");

		public PuzzlePromise.ServerCommitment[] SignHashes(string channelId, SignaturesRequest sigReq) => SignHashesAsync(channelId, sigReq).GetAwaiter().GetResult();

		public SolutionKey[] FulfillOffer(string channelId, TransactionSignature signature) => FulfillOfferAsync(_identity.CycleId, channelId, signature).GetAwaiter().GetResult();

		public Task<SolutionKey[]> FulfillOfferAsync(int cycleId, string channelId, TransactionSignature signature) => SendAsync<SolutionKey[]>(HttpMethod.Post, signature, $"clientchannels/{_identity.CycleId}/{channelId}/offer");

		public void GiveEscapeKey(string channelId, TransactionSignature signature)
		{
			GiveEscapeKeyAsync(channelId, signature).GetAwaiter().GetResult();
		}
		public Task GiveEscapeKeyAsync(string channelId, TransactionSignature signature) => SendAsync<string>(HttpMethod.Post, signature, $"clientchannels/{_identity.CycleId}/{channelId}/escape");
	}
}
