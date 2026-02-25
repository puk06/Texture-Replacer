using net.puk06.TextureReplacer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace net.puk06.TextureReplacer.Editor
{
    internal static class PTRContextMenu
    {
        private const int Pri = 20;

        private const string MenuBasePath = "GameObject/Puko's Texture Replacer/"; // Base path for the context menu

        [MenuItem(MenuBasePath + "Add Component", false, Pri)]

        private static void AddComponent()
        {
            GameObject textureReplacerObject = new("Puko's Texture Replacer");
            GameObject activeObject = Selection.activeGameObject;
            if (activeObject != null) textureReplacerObject.transform.SetParent(activeObject.transform);

            Undo.RegisterCreatedObjectUndo(textureReplacerObject, "Create Puko's Texture Replacer");

            Undo.AddComponent<PukoTextureReplacer>(textureReplacerObject);
            LogUtils.Log($"Component created on '{textureReplacerObject.name}'.");
            PingObject(textureReplacerObject);
        }

        private static void PingObject(GameObject gameObject)
        {
            if (gameObject == null) return;

            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }
    }
}
