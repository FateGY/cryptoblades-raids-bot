namespace RaidsBot.Options
{
    public class Web3Options
    {
        public const string Key = "Web3";

        public string RpcUrl { get; set; }
        public string AccountPrivateKey { get; set; }
        public long ChainId { get; set; }
        public BigInteger GasPrice { get; set; }
    }
}