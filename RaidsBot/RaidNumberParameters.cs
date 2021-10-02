using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RaidsBot
{
    [Function("numberParameters", "uint256")]
    public class RaidNumberParameters : FunctionMessage
    {
        [Parameter("uint256", "paramIndex")]
        public BigInteger ParamIndex { get; set; }
    }
}
