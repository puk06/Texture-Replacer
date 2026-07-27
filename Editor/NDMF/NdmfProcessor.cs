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
        internal static Dictionary<Texture2D, Texture2D> ProcessAllComponents(IEnumerable<PukoTextureReplacer> components, bool isPreview = false)
        {
            var result = new Dictionary<Texture2D, Texture2D>();

            foreach (var textureEntry in components.Where(i => i.IsActiveTRComponent(isPreview)).SelectMany(i => i.ReplacementDefinitions))
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
            if (processedTexturesDictionary.Count == 0) return;

            var materialMap = new Dictionary<Material, Material>();
            
            foreach (Renderer renderer in renderers)
            {
                Material?[] materials = renderer.sharedMaterials;
                bool changed = false;

                foreach (ref var material in materials.AsSpan())
                {
                    if (material == null) continue;
                    if (materialMap.TryGetValue(material, out Material? cloned))
                    {
                        material = cloned;
                        changed = true;
                    }
                    else
                    {
                        var newMaterial = GetProcessedMaterial(material, processedTexturesDictionary);
                        if (newMaterial == material) continue;

                        ObjectRegistry.RegisterReplacedObject(material, newMaterial!);
                        materialMap.Add(material, newMaterial!);
                        material = newMaterial;
                        changed = true;
                    }
                }

                if (changed) renderer.sharedMaterials = materials;
            }
        }

        [return:NotNullIfNotNull("material")]
        internal static Material? GetProcessedMaterial(Material? material, Dictionary<Texture2D, Texture2D> processedTextures)
        {
            if (material == null) return null;

            Material? newMaterial = null;

            material.ForEachTexture((texture, propName) =>
            {
                if (texture is not Texture2D originalTexture || !processedTextures.TryGetValue(originalTexture, out Texture2D? processedTexture)) return;
                if (newMaterial == null) newMaterial = UnityEngine.Object.Instantiate(material);
                newMaterial.SetTexture(propName, processedTexture);
            });

            if (newMaterial != null) return newMaterial;
            return material;
        }
    }
}
