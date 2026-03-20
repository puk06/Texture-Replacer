#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        internal static void ReplaceTexturesInRenderers(IEnumerable<Renderer> renderers, Dictionary<Texture2D, Texture2D> processedTexturesDictionary)
        {
            Dictionary<Material, Material> materialMap = new();
            
            foreach (Renderer renderer in renderers)
            {
                Material?[] materials = renderer.sharedMaterials;

                foreach (ref Material? material in materials.AsSpan())
                {
                    if (material == null) continue;
                    if (materialMap.TryGetValue(material, out Material? cloned))
                    {
                        material = cloned;
                    }
                    else
                    {
                        Material newMaterial = GetProcessedMaterial(material, processedTexturesDictionary);

                        ObjectRegistry.RegisterReplacedObject(material, newMaterial);
                        materialMap.Add(material, newMaterial);
                        material = newMaterial;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        [return:NotNullIfNotNull("material")]
        internal static Material? GetProcessedMaterial(Material? material, Dictionary<Texture2D, Texture2D> processedTextures)
        {
            if (material == null) return null;

            Material newMaterial = UnityEngine.Object.Instantiate(material);

            newMaterial.ForEachTexture((texture, propName) =>
            {
                if (texture is not Texture2D originalTexture || !processedTextures.TryGetValue(originalTexture, out Texture2D? processedTexture)) return;
                newMaterial.SetTexture(propName, processedTexture);
            });

            return newMaterial;
        }
    }
}
