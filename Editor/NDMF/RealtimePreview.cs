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
                    foreach (Renderer avatarRenderer in context.GetComponentsInChildren<Renderer>(avatarGameObject, true).Where(r => r is MeshRenderer or SkinnedMeshRenderer))
                    {
                        Material[] materials = context.Observe(avatarRenderer, i => i.sharedMaterials, (a, b) => a != null && b != null && a.SequenceEqual(b));
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

                materialMap = new();

                foreach ((Renderer original, Renderer proxy) in proxyPairs)
                {
                    Material?[] materials = proxy.sharedMaterials;
                    Material?[] newMaterials = (Material?[])materials.Clone();
                    bool changed = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null) continue;

                        if (materialMap.TryGetValue(materials[i], out Material? cached))
                        {
                            newMaterials[i] = cached;
                            changed = true;
                        }
                        else
                        {
                            Material? processed = NdmfProcessor.GetProcessedMaterial(materials[i], replacedTexturesDictionary);
                            if (processed != materials[i])
                            {
                                materialMap.Add(materials[i], processed!);
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
                        foreach (Material material in materialMap.Values)
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
                    foreach (Material material in _createdMaterials)
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
