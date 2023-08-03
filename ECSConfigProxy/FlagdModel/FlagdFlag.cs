namespace ECSConfigProxy.FlagdModel
{
    public sealed class FlagdFlag
    {
        public string state { get; set; }

        public string defaultVariant { get; set; }

        public Dictionary<string, object> variants { get; set; }
    }
}
