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

namespace NTumbleBit.ClassicTumbler.Client
{
	public class TumblerClient : IDisposable
	{
		public TumblerClient(Network network, Uri serverAddress, int cycleId)
		{
			_Address = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
			_Network = network ?? throw new ArgumentNullException(nameof(network));
			this.cycleId = cycleId;
			ClassicTumblerParameters.ExtractHashFromUrl(serverAddress); //Validate
		}

		private int cycleId;

		private readonly Network _Network;
		public Network Network => _Network;

		private readonly Uri _Address;

		private static readonly HttpClient SharedClient = new HttpClient(new HttpClientHandler().SetAntiFingerprint());

		internal HttpClient Client = SharedClient;

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

		public Task<TumblerEscrowKeyResponse> RequestTumblerEscrowKeyAsync() => SendAsync<TumblerEscrowKeyResponse>(HttpMethod.Post, cycleId, $"clientchannels/");

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

		private async Task<T> SendAsync<T>(HttpMethod method, object body, string relativePath, params object[] parameters)
		{
			var uri = GetFullUri(relativePath, parameters);
			var message = new HttpRequestMessage(method, uri);
			if (body != null)
			{
				message.Content = new StringContent(Serializer.ToString(body, Network), Encoding.UTF8, "application/json");
			}
			var result = await Client.SendAsync(message).ConfigureAwait(false);
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

		private Task<ServerCommitmentsProof> CheckRevelationAsync(string channelId, PuzzlePromise.ClientRevelation revelation) => SendAsync<ServerCommitmentsProof>(HttpMethod.Post, revelation, $"channels/{cycleId}/{channelId}/checkrevelation");

		public Task<PuzzlePromise.ServerCommitment[]> SignHashesAsync(string channelId, SignaturesRequest sigReq) => SendAsync<PuzzlePromise.ServerCommitment[]>(HttpMethod.Post, sigReq, $"channels/{cycleId}/{channelId}/signhashes");

		public SolutionKey[] CheckRevelation(string channelId, PuzzleSolver.ClientRevelation revelation) => CheckRevelationAsync(channelId, revelation).GetAwaiter().GetResult();

		public Task<SolutionKey[]> CheckRevelationAsync(string channelId, PuzzleSolver.ClientRevelation revelation) => SendAsync<SolutionKey[]>(HttpMethod.Post, revelation, $"clientschannels/{cycleId}/{channelId}/checkrevelation");

		public OfferInformation CheckBlindFactors(string channelId, BlindFactor[] blindFactors) => CheckBlindFactorsAsync(channelId, blindFactors).GetAwaiter().GetResult();

		public Task<OfferInformation> CheckBlindFactorsAsync(string channelId, BlindFactor[] blindFactors) => SendAsync<OfferInformation>(HttpMethod.Post, blindFactors, $"clientschannels/{cycleId}/{channelId}/checkblindfactors");

		public PuzzleSolver.ServerCommitment[] SolvePuzzles(string channelId, PuzzleValue[] puzzles) => SolvePuzzlesAsync(channelId, puzzles).GetAwaiter().GetResult();

		public void SetHttpHandler(HttpMessageHandler handler)
		{
			Client = new HttpClient(handler);
		}

		public Task<PuzzleSolver.ServerCommitment[]> SolvePuzzlesAsync(string channelId, PuzzleValue[] puzzles) => SendAsync<PuzzleSolver.ServerCommitment[]>(HttpMethod.Post, puzzles, $"clientchannels/{cycleId}/{channelId}/solvepuzzles");

		public PuzzlePromise.ServerCommitment[] SignHashes(string channelId, SignaturesRequest sigReq) => SignHashesAsync(channelId, sigReq).GetAwaiter().GetResult();

		public SolutionKey[] FulfillOffer(string channelId, TransactionSignature signature) => FulfillOfferAsync(cycleId, channelId, signature).GetAwaiter().GetResult();

		public Task<SolutionKey[]> FulfillOfferAsync(int cycleId, string channelId, TransactionSignature signature) => SendAsync<SolutionKey[]>(HttpMethod.Post, signature, $"clientchannels/{cycleId}/{channelId}/offer");

		public void GiveEscapeKey(string channelId, TransactionSignature signature)
		{
			GiveEscapeKeyAsync(channelId, signature).GetAwaiter().GetResult();
		}
		public Task GiveEscapeKeyAsync(string channelId, TransactionSignature signature) => SendAsync<string>(HttpMethod.Post, signature, $"clientchannels/{cycleId}/{channelId}/escape");

		public void Dispose()
		{
			if (Client != SharedClient)
				Client.Dispose();
		}
	}
}
