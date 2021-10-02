using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NLog;
using RaidsBot.Options;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace RaidsBot
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public static Web3 web3;
        public static string raidContractAddress;
        
        static async Task Main(string[] args)
        {
            try
            {
                Logger.Info("Booting up...");
                Console.WriteLine("Test");

                Logger.Info("ARGS:");
                for(int i = 0; i < args.Length; i++)
                {
                    Logger.Info("["+i+"] "+args[i]);
                }

                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile("appsettings.local.json", true, true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();

                var web3Options = configuration.GetSection(Web3Options.Key).Get<Web3Options>();
                if(web3Options.AccountPrivateKey.Length == 0)
                    web3Options.AccountPrivateKey = Environment.GetEnvironmentVariable("ACCOUNT_PRIVATE_KEY");
                var account = new Account(web3Options.AccountPrivateKey);

                if (web3Options.RpcUrl.Length == 0)
                    web3Options.RpcUrl = Environment.GetEnvironmentVariable("RPC_URL");
                web3 = new Web3(account, web3Options.RpcUrl);

                var raidContractOptions = configuration.GetSection(RaidContractOptions.Key).Get<RaidContractOptions>();
                if (raidContractOptions.Address.Length == 0)
                    raidContractOptions.Address = Environment.GetEnvironmentVariable("RAID_CONTRACT_ADDRESS");
                raidContractAddress = raidContractOptions.Address;

                BigInteger raidIndex = await GetRaidIndex();
                Logger.Info("Raid index: " + raidIndex);
                BigInteger raidStatus = await GetRaidStatus(raidIndex);
                Logger.Info("Raid status: " + raidStatus);
                if (raidStatus == 4) // PAUSED, we quit
                {
                    Logger.Info("Status PAUSED, quitting.");
                    return;
                }

                BigInteger raidEndTime = await GetRaidEndTime(raidIndex);
                var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                var blockInfo = await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(latestBlockNumber);
                var timestamp = blockInfo.Timestamp.Value;

                Logger.Info("Timestamp of block #" + latestBlockNumber + ": " + timestamp);
                BigInteger timeDifference = timestamp - raidEndTime;
                Logger.Info("Time difference: " + timeDifference + " sec");

                if (timeDifference > 60)
                {
                    Logger.Info("Calling doRaidAuto()");
                    Logger.Info(await DoRaidAuto());
                }

                Logger.Info("Done.");
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
            }
            //Console.ReadLine();
        }

        public static async Task<BigInteger> GetRaidIndex()
        {
            return await web3.Eth.GetContractQueryHandler<RaidIndexVariable>()
                .QueryAsync<BigInteger>(raidContractAddress);
        }

        public static async Task<BigInteger> GetRaidStatus(BigInteger index)
        {
            return await web3.Eth.GetContractQueryHandler<RaidStatusVariable>()
                .QueryAsync<BigInteger>(raidContractAddress,
                new RaidStatusVariable
                {
                    RaidIndex = index
                }
            );
        }

        public static async Task<BigInteger> GetRaidEndTime(BigInteger index)
        {
            return await web3.Eth.GetContractQueryHandler<RaidEndTimeVariable>()
                .QueryAsync<BigInteger>(raidContractAddress,
                new RaidEndTimeVariable
                {
                    RaidIndex = index
                }
            );
        }
        
        public static async Task<string> DoRaidAuto()
        {
            return await web3.Eth.GetContractTransactionHandler<DoRaidAutoFunction>()
                .SendRequestAsync(
                    raidContractAddress,
                    new DoRaidAutoFunction()
            );
        }
    }
}
