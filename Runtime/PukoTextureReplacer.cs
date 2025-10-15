#nullable enable
using System;
using UnityEngine;

namespace net.puk06.TextureReplacer
{
    [Serializable]
    public class PukoTextureReplacer : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        [Header("スクリプトの有効 / 無効")]
        [Tooltip("スクリプト本体の有効 / 無効を切り替えます")]
        public bool Enabled = true;

        [Header("プレビューの有効 / 無効")]
        [Tooltip("NDMFのリアルタイムプレビューの有効 / 無効を切り替えます")]
        public bool PreviewEnabled = true;

        [Space(10)]

        [Header("置き換え元のテクスチャ")]
        [Tooltip("アバター内のこのテクスチャ全てが置き換えられます")]
        public Texture2D? sourceTexture = null;

        [Header("置き換え後のテクスチャ")]
        [Tooltip("指定したテクスチャがこのテクスチャに全て置き換えられます")]
        public Texture2D? destinationTexture = null;
    }
}
