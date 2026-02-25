#nullable enable
using System.Collections.Generic;
using nadena.dev.ndmf;
using Object = UnityEngine.Object;

namespace net.puk06.TextureReplacer.Editor.Ndmf
{
    internal static class ObjectReferenceService
    {
        internal static void RegisterReplacements<TKey, TValue>(Dictionary<TKey, TValue?> objectDictionary)
            where TKey : Object
            where TValue : Object
        {
            foreach (KeyValuePair<TKey, TValue?> objectKpv in objectDictionary)
            {
                if (objectKpv.Value == null) continue;
                ObjectRegistry.RegisterReplacedObject(objectKpv.Key, objectKpv.Value);
            }
        }
    }
}
