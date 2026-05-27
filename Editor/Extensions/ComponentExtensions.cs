using UnityEngine;

namespace net.puk06.TextureReplacer.Editor.Extension
{
    internal static class ComponentExtensions
    {
        public static bool IsActiveTRComponent(this Component component, bool isPreview = false)
        {
            if (!IsActiveComponent(component)) return false;

            if (component is PukoTextureReplacer textureReplacerComponent)
            {
                return textureReplacerComponent.IsEnabled && (isPreview == false || textureReplacerComponent.IsPreviewEnabled);
            }

            return false;
        }

        public static bool IsActiveComponent(this Component component)
        {
            return component.gameObject.activeInHierarchy && component.IsEditorOnly() == false;
        }

        public static bool IsEditorOnly(this Component component)
        {
            return component.CompareTag("EditorOnly");
        }
    }
}
