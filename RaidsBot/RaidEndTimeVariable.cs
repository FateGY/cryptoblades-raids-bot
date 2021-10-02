using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RaidsBot
{
    [Function("raidEndTime", "uint256")]
    public class RaidEndTimeVariable : FunctionMessage
    {
        [Parameter("uint256", "raidIndex")]
        public BigInteger RaidIndex { get; set; }
    }
}
