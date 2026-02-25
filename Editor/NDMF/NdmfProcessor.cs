#nullable enable
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using net.puk06.TextureReplacer.Editor.Extension;
using UnityEngine;

namespace net.puk06.TextureReplacer.Editor.Ndmf
{
    internal class NdmfProcessor
    {
        internal static Dictionary<Texture2D, Texture2D?> ProcessAllComponents(IEnumerable<PukoTextureReplacer> components)
        {
            Dictionary<Texture2D, Texture2D?> result = new();

            foreach (TextureEntry textureEntry in components.SelectMany(i => i.ReplacementDefinitions))
            {
                if (textureEntry.SourceTexture == null || result.ContainsKey(textureEntry.SourceTexture)) continue;
                if (textureEntry.DestinationTexture == null || result.ContainsKey(textureEntry.DestinationTexture)) continue;
                
                if (textureEntry.SourceTexture == textureEntry.DestinationTexture) continue;

                result.Add(textureEntry.SourceTexture, textureEntry.DestinationTexture);
            }

            return result;
        }

        internal static void ReplaceTexturesInRenderers(IEnumerable<Renderer> renderers, Dictionary<Texture2D, Texture2D?> processedTexturesDictionary)
        {
            foreach (Renderer renderer in renderers)
            {
                Material?[] materials = renderer.sharedMaterials;
                Material?[] newMaterials = new Material[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    newMaterials[i] = GetProcessedMaterial(materials[i], processedTexturesDictionary);

                    ObjectRegistry.RegisterReplacedObject(materials[i], newMaterials[i]);
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        internal static Material? GetProcessedMaterial(Material? material, Dictionary<Texture2D, Texture2D?> processedTextures)
        {
            if (material == null) return null;

            Material newMaterial = Object.Instantiate(material);

            newMaterial.ForEachTexture((texture, propName) =>
            {
                if (texture is not Texture2D originalTexture || !processedTextures.TryGetValue(originalTexture, out Texture2D? processedTexture)) return;
                newMaterial.SetTexture(propName, processedTexture);
            });

            return newMaterial;
        }
    }
}
