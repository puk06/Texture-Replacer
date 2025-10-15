#nullable enable
using System;
using UnityEngine;

namespace net.puk06.TextureReplacer
{
    [Serializable]
    public class PukoTextureReplacer : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        [Header("スクリプトの有効 / 無効")]
        public bool Enabled = true;

        [Header("プレビューの有効 / 無効")]
        public bool PreviewEnabled = true;

        [Header("置き換え元のテクスチャ")]
        public Texture2D? originalTexture = null;

        [Header("置き換え後のテクスチャ")]
        public Texture2D? targetTexture = null;
    }
}
