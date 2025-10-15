#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf;
using nadena.dev.ndmf.preview;
using net.puk06.TextureReplacer.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.puk06.TextureReplacer.NDMF
{
    internal class NDMFPreview : IRenderFilter
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            var avatars = context.GetAvatarRoots();

            var resultSet = new List<RenderGroup>();
            foreach (var avatar in avatars)
            {
                try
                {
                    // アバター内にある全部のコンポーネント
                    var components = context.GetComponentsInChildren<PukoTextureReplacer>(avatar, true);
                    if (!components.Any()) continue;

                    // その中で参照されてる全てのテクスチャ (重複対策してあります)
                    var targetTextures = components
                        .Select(c => context.Observe(c, c => c.originalTexture))
                        .Where(t => t != null)
                        .Distinct()
                        .ToArray();

                    // アバター内の全てのレンダラー
                    var avatarRenderers = avatar.GetComponentsInChildren<Renderer>()
                        .Where(r => r is MeshRenderer or SkinnedMeshRenderer)
                        .GroupBy(r => r.gameObject);
                    if (!avatarRenderers.Any()) continue;

                    var rendererList = new List<Renderer>();
                    foreach (var avatarRenderer in avatarRenderers)
                    {
                        var firstComponent = avatarRenderer.FirstOrDefault();
                        if (firstComponent == null) continue;

                        if (avatarRenderer.Count() >= 2)
                        {
                            LogUtils.LogWarning($"Duplicate Renderer GameObject detected: '{avatarRenderer.Key.name}' (using settings from '{firstComponent.GetType()}' component)");
                        }

                        var materials = firstComponent.sharedMaterials;
                        if (materials == null) continue;

                        if (materials.Any(material => targetTextures.Any(targetTexture => targetTexture != null && MaterialUtils.AnyTex(material, targetTexture))))
                        {
                            rendererList.Add(firstComponent);
                        }
                    }

                    // レンダラーリストは、コンポーネントによってアバター内のどれかのマテリアルテクスチャが参照されているレンダラーのリスト
                    // WithDataでアバター、現在のコンポーネントを返してあげることでInstantiateを呼び出す仕組みになっている
                    if (rendererList.Count > 0)
                        resultSet.Add(RenderGroup.For(rendererList).WithData((avatar, components)));
                }
                catch (Exception ex)
                {
                    LogUtils.LogError($"Failed to add renderer for avatar '{avatar.name}'.\n{ex}");
                }
            }

            return resultSet.ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            try
            {
                var renderData = group.GetData<(GameObject, PukoTextureReplacer[])>();
                var avatar = renderData.Item1;
                var components = renderData.Item2;

                // 早期リターンで監視対象から外れるのを防ぐため
                foreach (var component in components)
                {
                    context.Observe(component);
                }

                // 中身が有効なコンポーネントだけ取り出す。Enabledもここでチェック。
                var enabledComponents = components.Where(x => TextureReplacerUtils.IsEnabled(x, context) && x.PreviewEnabled);
                if (enabledComponents == null || !enabledComponents.Any()) return Task.FromResult<IRenderFilterNode>(new TextureReplacerNode(null));

                // このアバター配下の全てのRendererが使っている全てのテクスチャのオブジェクトリファレンス一覧
                var avatarTexturesReferences = TextureUtils.GetRenderersTexturesReferences(group.Renderers);
                if (avatarTexturesReferences == null || !avatarTexturesReferences.Any()) return Task.FromResult<IRenderFilterNode>(new TextureReplacerNode(null));

                // 変更される予定のテクスチャ（アバター配下で使われている物だけ）
                var targetTextures = enabledComponents
                    .Select(c => c.originalTexture)
                    .Where(t => avatarTexturesReferences.Any(r => r.Equals(NDMFUtils.GetReference(t))))
                    .Distinct()
                    .ToArray();
                if (targetTextures == null || !targetTextures.Any()) return Task.FromResult<IRenderFilterNode>(new TextureReplacerNode(null));

                // 元のテクスチャ、処理されたテクスチャのDictionary
                var processedTextures = new Dictionary<Texture2D, Texture2D>();

                // ターゲットテクスチャごとに分ける。これは複数同じテクスチャがあった時対策
                var groupedComponents = enabledComponents
                    .GroupBy(c => c.originalTexture);

                foreach (var groupedComponent in groupedComponents)
                {
                    var firstComponent = groupedComponent.FirstOrDefault();
                    if (firstComponent == null) continue;

                    if (groupedComponent.Count() >= 2)
                    {
                        LogUtils.LogWarning($"Duplicate originalTexture detected: '{groupedComponent.Key!.name}' (using texture settings from '{firstComponent.gameObject.name}')");
                    }

                    processedTextures.Add(groupedComponent.Key!, firstComponent.targetTexture!);
                }

                // テクスチャが含まれているマテリアルすべてを探す。
                var relevantMaterials = group.Renderers
                    .SelectMany(r => r.sharedMaterials)
                    .Where(material => targetTextures.Any(tex => MaterialUtils.AnyTex(material, tex)))
                    .Distinct()
                    .ToList();

                // テクスチャが含まれているマテリアル全てを複製 + 新しいテクスチャに置き換える
                var processedMaterials = relevantMaterials
                    .ToDictionary(
                        material => material,
                        material => ProcessMaterial(material, processedTextures)
                    );

                // RegisterReplacedObjectに登録
                // RegisterTextureReplace(processedTextures);
                // RegisterMaterialReplace(processedMaterials);

                // 変換前、変換後のマテリアルテクスチャ、生成したテクスチャの配列をまとめたものを渡してあげる。
                return Task.FromResult<IRenderFilterNode>(new TextureReplacerNode(processedMaterials));
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"Failed to instantiate.\n{ex}");
                return Task.FromResult<IRenderFilterNode>(new TextureReplacerNode(null));
            }
        }

        private Material ProcessMaterial(Material material, Dictionary<Texture2D, Texture2D> processedTextures)
        {
            var newMat = Object.Instantiate(material);

            MaterialUtils.ForEachTex(newMat, (tex, propName) =>
            {
                Texture2D? tex2D = tex as Texture2D;
                if (tex2D == null) return;
                
                if (processedTextures.TryGetValue(tex2D, out var texture))
                    newMat.SetTexture(propName, texture);
            });

            return newMat;
        }

        private void RegisterTextureReplace(Dictionary<Texture2D, Texture2D> processedTextures)
        {
            foreach (var processedTexture in processedTextures)
            {
                ObjectRegistry.RegisterReplacedObject(processedTexture.Key, processedTexture.Value);
            }
        }

        private void RegisterMaterialReplace(Dictionary<Material, Material> processedMaterials)
        {
            foreach (var processedMaterial in processedMaterials)
            {
                ObjectRegistry.RegisterReplacedObject(processedMaterial.Key, processedMaterial.Value);
            }
        }

        // このノードはアバター1体につき1個作られる。OnFrameは、RenderGroupの中身分のみ呼ばれる
        private class TextureReplacerNode : IRenderFilterNode, IDisposable
        {
            private readonly Dictionary<Material, Material>? _processedMaterialsDictionary;

            public RenderAspects WhatChanged { get; private set; } = RenderAspects.Texture & RenderAspects.Material;

            public TextureReplacerNode(Dictionary<Material, Material>? materialDictionary)
            {
                if (materialDictionary != null)
                {
                    _processedMaterialsDictionary = materialDictionary; // ここで渡されるものは、OnFrameで置き換えられるものがあるのが確定したマテリアルと、その処理済みマテリアルのDictionaryである
                }
            }

            public void OnFrame(Renderer original, Renderer proxy)
            {
                try
                {
                    if (proxy == null || proxy.sharedMaterials == null || _processedMaterialsDictionary == null || _processedMaterialsDictionary.Count == 0)
                        return;

                    proxy.sharedMaterials = GenerateSwappedMaterials(proxy.sharedMaterials);
                }
                catch (Exception ex)
                {
                    LogUtils.LogError("Error occurred while rendering proxy.\n" + ex);
                }
            }

            private Material?[] GenerateSwappedMaterials(Material[] proxyMaterials)
            {
                var processedMaterials = new Material?[proxyMaterials.Length];

                for (int i = 0; i < proxyMaterials.Length; i++)
                {
                    var proxyMaterial = proxyMaterials[i];
                    if (proxyMaterial == null)
                    {
                        processedMaterials[i] = null;
                        continue;
                    }

                    if (_processedMaterialsDictionary != null && _processedMaterialsDictionary.TryGetValue(proxyMaterial, out var processedMaterial))
                    {
                        processedMaterials[i] = processedMaterial;
                    }
                    else
                    {
                        processedMaterials[i] = proxyMaterial;
                    }
                }

                return processedMaterials;
            }

            public void Dispose()
            {
                if (_processedMaterialsDictionary != null && _processedMaterialsDictionary.Values != null && _processedMaterialsDictionary.Values.Count() != 0)
                {
                    foreach (var material in _processedMaterialsDictionary.Values)
                    {
                        Object.DestroyImmediate(material);
                    }

                    _processedMaterialsDictionary.Clear();
                }
            }
        }
    }
}
