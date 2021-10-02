using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaidsBot
{
    [Function("raidIndex", "uint256")]
    public class RaidIndexVariable : FunctionMessage
    {
    }
}
