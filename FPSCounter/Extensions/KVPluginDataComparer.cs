using System.Collections.Generic;

namespace FPSCounter
{
    class KVPluginDataComparer: IComparer<KeyValuePair<string, long>>
    {
        public int Compare(KeyValuePair<string, long> val1, KeyValuePair<string, long> val2)
        {
            return val2.Value.CompareTo(val1.Value);
        }
    }
}
