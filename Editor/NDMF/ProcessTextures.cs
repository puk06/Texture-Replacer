#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using net.puk06.TextureReplacer.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.puk06.TextureReplacer.NDMF
{
    public class TextureReplacer : Pass<TextureReplacer>
    {
        protected override void Execute(BuildContext buildContext)
        {
            var avatar = buildContext.AvatarRootObject;

            var components = avatar.GetComponentsInChildren<PukoTextureReplacer>(true);
            if (components == null || !components.Any()) return;

            try
            {
                // 中身が有効なコンポーネントだけ取り出す。Enabledもここでチェック。
                var enabledComponents = components.Where(x => TextureReplacerUtils.IsEnabled(x));
                if (!enabledComponents.Any()) return;

                // このアバター配下の全てのRendererが使っている全てのテクスチャのハッシュ一覧
                var avatarRenderers = TextureUtils.GetRenderers(avatar);
                var avatarTexturesHashSet = TextureUtils.GetRenderersTexturesHashSet(avatarRenderers);
                if (avatarTexturesHashSet == null || !avatarTexturesHashSet.Any()) return;

                var avatarComponents = enabledComponents
                    .Where(c => avatarTexturesHashSet.Contains(c.originalTexture!));
                if (!avatarComponents.Any()) return;

                Dictionary<Texture2D, Texture2D> processedDictionary = new();

                foreach (var component in avatarComponents)
                {
                    processedDictionary.Add(component.originalTexture!, component.targetTexture!);
                }

                Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>();
                ReplaceTextures(renderers, processedDictionary);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occured while processing avatar: '{avatar.name}'\n{ex}");
            }
            finally
            {
                DeleteAllComponents(components);
            }
        }

        private void ReplaceTextures(Renderer[] renderers, Dictionary<Texture2D, Texture2D> processedTextureDictionary)
        {
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[materials.Length];

                var materialsToChange = processedTextureDictionary.Keys
                    .SelectMany(tex => TextureUtils.FindMaterialsWithTexture(materials, tex))
                    .ToList();

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;

                    if (materialsToChange.Contains(materials[i]))
                    {
                        newMaterials[i] = new Material(materials[i]);

                        MaterialUtils.ForEachTex(newMaterials[i], (texture, propName) =>
                        {
                            if (!processedTextureDictionary.TryGetValue((Texture2D)texture, out Texture2D newTexture)) return;
                            newMaterials[i].SetTexture(propName, newTexture);
                        });
                    }
                    else
                    {
                        newMaterials[i] = materials[i];
                    }
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        private void DeleteAllComponents(PukoTextureReplacer[] components)
        {
            foreach (var component in components)
            {
                Object.DestroyImmediate(component);
            }
        }
    }
}
