﻿using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json.Linq;
using NBitcoin.DataEncoders;
using System.Threading;

namespace NTumbleBit.Services.RPC
{
	public class RPCBlockExplorerService : IBlockExplorerService
	{
		RPCWalletCache _Cache;
		public RPCBlockExplorerService(RPCClient client, RPCWalletCache cache, IRepository repo)
		{
			_RPCClient = client ?? throw new ArgumentNullException(nameof(client));
			_Repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_Cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		IRepository _Repo;
		private readonly RPCClient _RPCClient;

		public RPCClient RPCClient => _RPCClient;

		public int GetCurrentHeight() => _Cache.BlockCount;

		public async Task<uint256> WaitBlockAsync(uint256 currentBlock, CancellationToken cancellation = default(CancellationToken))
		{
			while(true)
			{
				cancellation.ThrowIfCancellationRequested();
				var h = _RPCClient.GetBestBlockHash();
				if(h != currentBlock)
				{
					_Cache.Refresh(h);
					return h;
				}
				await Task.Delay(5000, cancellation).ConfigureAwait(false);
			}
		}

		public TransactionInformation[] GetTransactions(Script scriptPubKey, bool withProof)
		{
			if(scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));

			var address = scriptPubKey.GetDestinationAddress(RPCClient.Network);
			if(address == null)
				return new TransactionInformation[0];

			var walletTransactions = _Cache.GetEntries();
			var results = Filter(walletTransactions, !withProof, address);

			if (withProof)
			{
				foreach(var tx in results.ToList())
				{
					MerkleBlock proof = null;
					var result = RPCClient.SendCommandNoThrows("gettxoutproof", new JArray(tx.Transaction.GetHash().ToString()));
					if(result == null || result.Error != null)
					{
						results.Remove(tx);
						continue;
					}
					proof = new MerkleBlock();
					proof.ReadWrite(Encoders.Hex.DecodeData(result.ResultString));
					tx.MerkleProof = proof;
				}
			}
			return results.ToArray();
		}

		private List<TransactionInformation> QueryWithListReceivedByAddress(bool withProof, BitcoinAddress address)
		{
			var result = RPCClient.SendCommand("listreceivedbyaddress", 0, false, true, address.ToString());
			var transactions = ((JArray)result.Result).OfType<JObject>().Select(o => o["txids"]).OfType<JArray>().SingleOrDefault();
			if(transactions == null)
				return null;

			var resultsSet = new HashSet<uint256>();
			var results = new List<TransactionInformation>();
			foreach (var txIdObj in transactions)
			{
				var txId = new uint256(txIdObj.ToString());
				//May have duplicates
				if(!resultsSet.Contains(txId))
				{
					var tx = GetTransaction(txId);
					if(tx == null || (withProof && tx.Confirmations == 0))
						continue;
					resultsSet.Add(txId);
					results.Add(tx);
				}
			}
			return results;
		}

		private List<TransactionInformation> Filter(RPCWalletEntry[] entries, bool includeUnconf, BitcoinAddress address)
		{
			var results = new List<TransactionInformation>();
			var resultsSet = new HashSet<uint256>();
			foreach (var obj in entries)
			{
				//May have duplicates
				if(!resultsSet.Contains(obj.TransactionId))
				{
					var confirmations = obj.Confirmations;
					var tx = _Cache.GetTransaction(obj.TransactionId);

					if(tx == null || (!includeUnconf && confirmations == 0))
						continue;

					if(tx.Outputs.Any(o => o.ScriptPubKey == address.ScriptPubKey) ||
					   tx.Inputs.Any(o => o.ScriptSig.GetSigner().ScriptPubKey == address.ScriptPubKey))
					{

						resultsSet.Add(obj.TransactionId);
						results.Add(new TransactionInformation
						{
							Transaction = tx,
							Confirmations = confirmations
						});
					}
				}
			}
			return results;
		}

		public TransactionInformation GetTransaction(uint256 txId)
		{
			try
			{
				//check in the wallet tx
				var result = RPCClient.SendCommandNoThrows("gettransaction", txId.ToString(), true);
				if(result == null || result.Error != null)
				{
					//check in the txindex
					result = RPCClient.SendCommandNoThrows("getrawtransaction", txId.ToString(), 1);
					if(result == null || result.Error != null)
						return null;
				}
				var tx = new Transaction((string)result.Result["hex"]);
				var confirmations = result.Result["confirmations"];
				var confCount = confirmations == null ? 0 : Math.Max(0, (int)confirmations);

				return new TransactionInformation
				{
					Confirmations = confCount,
					Transaction = tx
				};
			}
			catch(RPCException) { return null; }
		}

		public void Track(Script scriptPubkey)
		{
			RPCClient.ImportAddress(scriptPubkey, "", false);
		}

		public int GetBlockConfirmations(uint256 blockId)
		{
			var result = RPCClient.SendCommandNoThrows("getblock", blockId.ToString(), true);
			if(result == null || result.Error != null)
				return 0;
			return (int)result.Result["confirmations"];
		}

		public bool TrackPrunedTransaction(Transaction transaction, MerkleBlock merkleProof)
		{
			var result = RPCClient.SendCommandNoThrows("importprunedfunds", transaction.ToHex(), Encoders.Hex.EncodeData(merkleProof.ToBytes()));
			var success = result != null && result.Error == null;
			if(success)
			{
				_Cache.ImportTransaction(transaction, GetBlockConfirmations(merkleProof.Header.GetHash()));
			}
			return success;
		}
	}
}
