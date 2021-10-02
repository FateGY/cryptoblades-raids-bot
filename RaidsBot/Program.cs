using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NLog;
using RaidsBot.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace RaidsBot
{
    class Program
    {
        //private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public static Web3 web3;
        public static string raidContractAddress;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Booting up...");
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.local.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var web3Options = configuration.GetSection(Web3Options.Key).Get<Web3Options>();
            if (web3Options.AccountPrivateKey.Length == 0)
                web3Options.AccountPrivateKey = Environment.GetEnvironmentVariable("ACCOUNT_PRIVATE_KEY");
            var account = new Account(web3Options.AccountPrivateKey);

            if (web3Options.RpcUrl.Length == 0)
                web3Options.RpcUrl = Environment.GetEnvironmentVariable("RPC_URL");
            web3 = new Web3(account, web3Options.RpcUrl);

            var raidContractOptions = configuration.GetSection(RaidContractOptions.Key).Get<RaidContractOptions>();
            if (raidContractOptions.Address.Length == 0)
                raidContractOptions.Address = Environment.GetEnvironmentVariable("RAID_CONTRACT_ADDRESS");
            raidContractAddress = raidContractOptions.Address;

            ListenToWeb();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Running raid task...");
                long waitMinutes = await RaidTask();
                Console.WriteLine();
                Console.WriteLine("Waiting "+waitMinutes+" mins...");
                Thread.Sleep((int)waitMinutes * 1000 * 60);
            }
        }

        static void ListenToWeb()
        {
            Console.WriteLine("Setting up dummy web service...");
            if (!int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
            { port = 5000; }

            // create the socket
            Socket listenSocket = new Socket(AddressFamily.InterNetwork,
                                             SocketType.Stream,
                                             ProtocolType.Tcp);

            // bind the listening socket to the port
            IPAddress hostIP = (Dns.GetHostEntry(IPAddress.Any.ToString())).AddressList[0];
            IPEndPoint ep = new IPEndPoint(hostIP, port);
            listenSocket.Bind(ep);

            // start listening
            listenSocket.Listen(1);
            Console.WriteLine("Dummy web service listening on port "+port);
        }

        static async Task<long> RaidTask()
        {
            try
            {
                BigInteger raidIndex = await GetRaidIndex();
                Console.WriteLine("Raid index: " + raidIndex);
                BigInteger raidStatus = await GetRaidStatus(raidIndex);
                Console.WriteLine("Raid status: " + raidStatus);
                if (raidStatus == 4) // PAUSED, we quit
                {
                    Console.WriteLine("Status PAUSED!");
                    return 60; // wait an hour
                }

                BigInteger raidEndTime = await GetRaidEndTime(raidIndex);
                var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                var blockInfo = await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(latestBlockNumber);
                var timestamp = blockInfo.Timestamp.Value;

                Console.WriteLine("Timestamp of block #" + latestBlockNumber + ": " + timestamp);
                BigInteger timeDifference = timestamp - raidEndTime;
                Console.WriteLine("Time difference: " + timeDifference + " sec");

                if (timestamp > raidEndTime)
                {
                    Console.WriteLine("Raid over,");
                    if (timeDifference < 30)
                    {
                        Console.WriteLine("Need to wait a few seconds for security.");
                        Thread.Sleep(((int)timeDifference + 1) * 1000);
                    }

                    Console.WriteLine("Calling doRaidAuto()");
                    Console.WriteLine(await DoRaidAuto());
                    Console.WriteLine("Success!");
                    BigInteger waitMinutes = await GetRaidAutoDuration() + 1;
                    return (long)waitMinutes;
                }
                else
                {
                    long waitMinutes = (long)(raidEndTime - timestamp) / 60 + 1;
                    Console.WriteLine("Raid not over yet.");
                    return waitMinutes;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR: "+ex);
            }
            return 60;
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

        public static async Task<BigInteger> GetRaidAutoDuration()
        {
            return await web3.Eth.GetContractQueryHandler<RaidNumberParameters>()
                .QueryAsync<BigInteger>(raidContractAddress,
                new RaidNumberParameters
                {
                    ParamIndex = 1
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
