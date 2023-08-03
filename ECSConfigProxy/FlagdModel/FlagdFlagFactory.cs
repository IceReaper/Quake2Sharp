namespace ECSConfigProxy.FlagdModel
{
    public sealed class FlagdFlagFactory
    {
        public static FlagdFlag CreateFlag(KeyValuePair<string, object>[] variants)
        {
            if (variants == null || variants.Length == 0)
            {
                throw new ArgumentException("variants cannot be null or empty");
            }

            var dict = new Dictionary<string, object>();
            foreach ( var variant in variants )
            {
                dict.Add(variant.Key, variant.Value);
            }

            return new FlagdFlag
            {
                state = "ENABLED",
                defaultVariant = variants[0].Key,
                variants = dict
            };
        }

        public static FlagdFlagSet CreateFlagsSet(IEnumerable<KeyValuePair<string, object>> flagsKvp)
        {
            if (flagsKvp == null)
            {
                throw new ArgumentException("flagsKvp cannot be null or empty");
            }

            var dict = new Dictionary<string, FlagdFlag>();
            foreach ( var flagKvp in flagsKvp )
            {
                dict.Add(flagKvp.Key, CreateFlag(new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("current", flagKvp.Value) }));
            }

            return new FlagdFlagSet
            {
                flags = dict
            };
        }
    }
}
