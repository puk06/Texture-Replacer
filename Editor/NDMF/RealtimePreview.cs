#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf;
using nadena.dev.ndmf.preview;
using net.puk06.TextureReplacer.Editor.Extension;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.puk06.TextureReplacer.Editor.Ndmf
{
    internal class RealtimePreview : IRenderFilter
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            IEnumerable<GameObject> avatarGameObjects = context.GetAvatarRoots().Distinct();

            List<RenderGroup> targetRenderGroups = new();

            foreach (GameObject avatarGameObject in avatarGameObjects)
            {
                try
                {
                    PukoTextureReplacer[] components = context.GetComponentsInChildren<PukoTextureReplacer>(avatarGameObject, true);
                    if (components.Length == 0) continue;

                    List<Texture2D> targetTextures = new();

                    foreach (PukoTextureReplacer component in components)
                    {
                        context.Observe(component, c => new List<TextureEntry>(c.ReplacementDefinitions), (a, b) => a.SequenceEqual(b));
                        foreach (TextureEntry entry in component.ReplacementDefinitions)
                        {
                            if (entry.SourceTexture == null || targetTextures.Contains(entry.SourceTexture)) continue;
                            targetTextures.Add(entry.SourceTexture);
                        }
                    }

                    List<Renderer> targetRenderers = new();
                    foreach (Renderer avatarRenderer in avatarGameObject.GetComponentsInChildren<Renderer>(true).Where(r => r is MeshRenderer or SkinnedMeshRenderer))
                    {
                        Material[] materials = avatarRenderer.sharedMaterials;
                        if (materials == null) continue;

                        if (materials.Any(material => targetTextures.Any(targetTexture => targetTexture != null && material.HasTexture(targetTexture))))
                        {
                            targetRenderers.Add(avatarRenderer);
                        }
                    }

                    if (targetRenderers.Count > 0)
                    {
                        targetRenderGroups.Add(RenderGroup.For(targetRenderers).WithData(avatarGameObject));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to add renderer for avatar: '{avatarGameObject.name}'.\n{ex}");
                }
            }

            return targetRenderGroups.ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            Dictionary<Texture2D, Texture2D>? replacedTexturesDictionary = null;
            Dictionary<Renderer, Material?[]>? processedMaterialDictionary = new();

            try
            {
                GameObject root = group.GetData<GameObject>();

                PukoTextureReplacer[] components = root.GetComponentsInChildren<PukoTextureReplacer>(true);

                foreach (PukoTextureReplacer component in components)
                {
                    context.Observe(component);
                    component.ReplacementDefinitions.ForEach(i =>
                    {
                        if (i.SourceTexture != null && i.DestinationTexture != null)
                        {
                            context.Observe(i.DestinationTexture);
                        }
                    });
                }

                replacedTexturesDictionary = NdmfProcessor.ProcessAllComponents(components, isPreview: true);
                ObjectReferenceService.RegisterReplacements(replacedTexturesDictionary);

                foreach ((Renderer original, Renderer proxy) in proxyPairs)
                {
                    processedMaterialDictionary[original] = proxy.sharedMaterials.Select(mat => {
                        Material? newMaterial = NdmfProcessor.GetProcessedMaterial(mat, replacedTexturesDictionary);
                        if (mat != null && newMaterial != null) ObjectRegistry.RegisterReplacedObject(mat, newMaterial);
                        return newMaterial;
                    }).ToArray();
                }

                return Task.FromResult<IRenderFilterNode>(new TextureReplacerNode(processedMaterialDictionary));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to instantiate.\n{ex}");
                if (processedMaterialDictionary != null)
                {
                    foreach (Material?[] materials in processedMaterialDictionary.Values)
                        foreach (Material? material in materials)
                            if (material != null) Object.DestroyImmediate(material);
                    processedMaterialDictionary.Clear();
                    processedMaterialDictionary = null;
                }
                return Task.FromResult<IRenderFilterNode>(new EmptyNode());
            }
        }

        private class TextureReplacerNode : IRenderFilterNode, IDisposable
        {
            private Dictionary<Renderer, Material?[]>? _processedMaterialDictionary;

            public RenderAspects WhatChanged { get; private set; } = RenderAspects.Texture | RenderAspects.Material;

            public TextureReplacerNode(Dictionary<Renderer, Material?[]>? processedMaterialDictionary)
            {
                _processedMaterialDictionary = processedMaterialDictionary;
            }

            public void OnFrame(Renderer original, Renderer proxy)
            {
                try
                {
                    if (_processedMaterialDictionary?.TryGetValue(original, out Material?[] processedMaterials) ?? false)
                    {
                        proxy.sharedMaterials = processedMaterials;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error occurred while rendering proxy.\n" + ex);
                }
            }

            public void Dispose()
            {
                if (_processedMaterialDictionary != null)
                {
                    foreach (Material?[] materials in _processedMaterialDictionary.Values)
                        foreach (Material? material in materials)
                            if (material != null) Object.DestroyImmediate(material);
                    _processedMaterialDictionary.Clear();
                    _processedMaterialDictionary = null;
                }
            }
        }

        private class EmptyNode : IRenderFilterNode
        {
            public RenderAspects WhatChanged { get; private set; } = 0;

            public void OnFrame(Renderer original, Renderer proxy)
            {
                // Do nothing
            }
        }
    }
}
