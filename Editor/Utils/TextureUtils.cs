#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.puk06.TextureReplacer.Utils
{
    internal static class TextureUtils
    {
        /// <summary>
        /// マテリアルの配列の中で、渡されたテクスチャの画像が入っているMaterialの配列を返します。
        /// </summary>
        /// <param name="materials"></param>
        /// <param name="targetTexture"></param>
        /// <returns></returns>
        internal static List<Material> FindMaterialsWithTexture(Material[] materials, Texture2D targetTexture)
        {
            List<Material> result = new List<Material>();
            if (targetTexture == null) return result;

            foreach (Material material in materials)
            {
                if (material == null) continue;

                var shader = material.shader;
                if (shader == null) continue;

                int count = MaterialUtils.GetPropertyCount(shader);
                if (count == 0) continue;

                for (int i = 0; i < count; i++)
                {
                    if (!MaterialUtils.IsTexture(shader, i)) continue;

                    string propName = ShaderUtil.GetPropertyName(shader, i);
                    if (propName == null) continue;

                    Texture currentTex = material.GetTexture(propName);
                    if (currentTex == null) continue;

                    if (currentTex == targetTexture)
                    {
                        result.Add(material);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// レンダラー内の全てのテクスチャのオブジェクトリファレンスを取得します。
        /// </summary>
        /// <param name="renderers"></param>
        /// <returns></returns>
        internal static ObjectReference[] GetRenderersTexturesReferences(IEnumerable<Renderer> renderers)
        {
            return renderers
                .SelectMany(r => r.sharedMaterials)
                .SelectMany(m =>
                {
                    var referenceList = new List<ObjectReference>();
                    MaterialUtils.ForEachTex(m, (texture, _) => referenceList.Add(NDMFUtils.GetReference(texture)));
                    return referenceList;
                })
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// レンダラー内の全てのテクスチャのハッシュセット(比較用)を取得します。
        /// </summary>
        /// <param name="renderers"></param>
        /// <returns></returns>
        internal static HashSet<Texture2D> GetRenderersTexturesHashSet(IEnumerable<Renderer> renderers)
        {
            return renderers
                .SelectMany(r => r.sharedMaterials)
                .SelectMany(m =>
                {
                    var textureList = new List<Texture>();
                    MaterialUtils.ForEachTex(m, (texture, _) => textureList.Add(texture));
                    return textureList;
                })
                .OfType<Texture2D>()
                .Distinct()
                .ToHashSet();
        }

        /// <summary>
        /// アバター内のすべてのレンダラーを取得します。
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        internal static Renderer[] GetRenderers(GameObject avatar)
            => avatar.GetComponentsInChildren<Renderer>(true);
    }
}
