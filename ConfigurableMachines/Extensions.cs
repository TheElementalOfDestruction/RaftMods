using System.Collections.Generic;
using UnityEngine;


namespace DestinyConfigMachines
{
    static class DictionaryExtension
    {
        // Thanks Aidan.
        public static void Clean<X,Y>(this Dictionary<X,Y> c) where X : MonoBehaviour
        {
            var l = new List<X>();
            foreach (var k in c.Keys)
            {
                if (!k)
                {
                    l.Add(k);
                }
            }
            foreach (var i in l)
            {
                c.Remove(i);
            }
        }

        // Tries to get a value based on the key, returning the default if not
        // found.
        public static Y Get<X,Y>(this Dictionary<X,Y> d, X key, Y _default)
        {
            return d.ContainsKey(key) ? d[key] : _default;
        }
    }
}
