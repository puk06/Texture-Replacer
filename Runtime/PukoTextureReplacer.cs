#nullable enable
using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace net.puk06.TextureReplacer
{
    [Serializable]
    public class PukoTextureReplacer : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        [Header("スクリプトの有効 / 無効")]
        [Tooltip("スクリプト本体の有効 / 無効を切り替えます")]
        [FormerlySerializedAs("Enabled")]
        public bool IsEnabled = true;

        [Header("プレビューの有効 / 無効")]
        [Tooltip("NDMFのリアルタイムプレビューの有効 / 無効を切り替えます")]
        [FormerlySerializedAs("PreviewEnabled")]
        public bool IsPreviewEnabled = true;

        [FormerlySerializedAs("sourceTexture")]
        [HideInInspector]
        public Texture2D? SourceTexture = null;

        [FormerlySerializedAs("destinationTexture")]
        [HideInInspector]
        public Texture2D? DestinationTexture = null;
        
        [Space(10)]

        [Header("テクスチャ置き換え定義")]
        public List<TextureEntry> ReplacementDefinitions = new();

        void Awake() => Migrate();
        private void Migrate()
        {
            if (ReplacementDefinitions.Count != 0) return;
            
            if (SourceTexture != null)
            {
                ReplacementDefinitions.Add(new TextureEntry()
                {
                    SourceTexture = SourceTexture,
                    DestinationTexture = DestinationTexture,
                });
            }
        }
    }

    [Serializable]
    public class TextureEntry
    {
        [Header("置き換え元")]
        public Texture2D? SourceTexture;

        [Header("置き換え先")]
        public Texture2D? DestinationTexture;
    }
}
