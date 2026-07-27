#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using net.puk06.TextureReplacer.Editor.Extension;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.puk06.TextureReplacer.Editor.Ndmf
{
    internal class RealtimePreview : IRenderFilter
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            var avatarGameObjects = context.GetAvatarRoots().Distinct();

            var targetRenderGroups = new List<RenderGroup>();

            foreach (var avatarGameObject in avatarGameObjects)
            {
                try
                {
                    var components = context.GetComponentsInChildren<PukoTextureReplacer>(avatarGameObject, true);
                    if (components.Length == 0) continue;

                    var targetTextures = new List<Texture2D>();

                    foreach (var component in components)
                    {
                        context.Observe(component, c => new List<TextureEntry>(c.ReplacementDefinitions), (a, b) => a.SequenceEqual(b));
                        foreach (var entry in component.ReplacementDefinitions)
                        {
                            if (entry.SourceTexture == null || targetTextures.Contains(entry.SourceTexture)) continue;
                            targetTextures.Add(entry.SourceTexture);
                        }
                    }

                    var targetRenderers = new List<Renderer>();
                    foreach (var avatarRenderer in context.GetComponentsInChildren<Renderer>(avatarGameObject, true).Where(r => r is MeshRenderer or SkinnedMeshRenderer))
                    {
                        var materials = context.Observe(avatarRenderer, i => i.sharedMaterials, (a, b) => a != null && b != null && a.SequenceEqual(b));
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
            Dictionary<Material, Material>? materialMap = null;

            try
            {
                var root = group.GetData<GameObject>();

                var components = root.GetComponentsInChildren<PukoTextureReplacer>(true);

                foreach (var component in components)
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

                materialMap = new();

                foreach ((Renderer original, Renderer proxy) in proxyPairs)
                {
                    Material?[] materials = proxy.sharedMaterials;
                    Material?[] newMaterials = (Material?[])materials.Clone();
                    bool changed = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (material == null) continue;

                        if (materialMap.TryGetValue(material, out Material? cached))
                        {
                            newMaterials[i] = cached;
                            changed = true;
                        }
                        else
                        {
                            var processed = NdmfProcessor.GetProcessedMaterial(material, replacedTexturesDictionary);
                            if (processed != material)
                            {
                                materialMap.Add(material, processed!);
                                newMaterials[i] = processed;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                        processedMaterialDictionary[original] = newMaterials;
                }

                return Task.FromResult<IRenderFilterNode>(new TextureReplacerNode(processedMaterialDictionary, materialMap.Values));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to instantiate.\n{ex}");
                if (processedMaterialDictionary != null)
                {
                    if (materialMap != null)
                    {
                        foreach (var material in materialMap.Values)
                            Object.DestroyImmediate(material);
                    }
                    processedMaterialDictionary.Clear();
                    processedMaterialDictionary = null;
                }
                return Task.FromResult<IRenderFilterNode>(new EmptyNode());
            }
        }

        private class TextureReplacerNode : IRenderFilterNode, IDisposable
        {
            private Dictionary<Renderer, Material?[]>? _processedMaterialDictionary;
            private IEnumerable<Material>? _createdMaterials;

            public RenderAspects WhatChanged { get; private set; } = RenderAspects.Texture | RenderAspects.Material;

            public TextureReplacerNode(Dictionary<Renderer, Material?[]>? processedMaterialDictionary, IEnumerable<Material>? createdMaterials)
            {
                _processedMaterialDictionary = processedMaterialDictionary;
                _createdMaterials = createdMaterials;
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
                if (_createdMaterials != null)
                {
                    foreach (var material in _createdMaterials)
                        Object.DestroyImmediate(material);
                    _createdMaterials = null;
                }

                if (_processedMaterialDictionary != null)
                {
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
