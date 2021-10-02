using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RaidsBot
{
    [Function("raidStatus", "uint256")]
    public class RaidStatusVariable : FunctionMessage
    {
        [Parameter("uint256", "raidIndex")]
        public BigInteger RaidIndex { get; set; }
    }
}
