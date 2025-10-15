#nullable enable
using nadena.dev.ndmf.preview;

namespace net.puk06.TextureReplacer.Utils
{
    internal static class TextureReplacerUtils
    {
        /// <summary>
        /// コンポーネントが有効状態にあるかを返します。PreviewEnabledはチェックされないので注意。
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        internal static bool IsEnabled(PukoTextureReplacer component)
            => component != null && component.Enabled && component.originalTexture != null && component.targetTexture != null && component.gameObject != null && component.gameObject.activeSelf;

        /// <summary>
        /// コンポーネントが有効状態にあるかを返します。PreviewEnabledはチェックされないので注意。
        /// gameObject.activeSelfがActiveInHierarchyで呼ばれるので、自動的に監視対象になります。
        /// </summary>
        /// <param name="component"></param>
        /// <param name="computeContext"></param>
        /// <returns></returns>
        internal static bool IsEnabled(PukoTextureReplacer component, ComputeContext computeContext)
            => component != null && component.Enabled && component.originalTexture != null && component.targetTexture != null && computeContext.ActiveInHierarchy(component.gameObject);
    }
}
