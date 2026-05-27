#nullable enable
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using net.puk06.TextureReplacer.Editor.Ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(NdmfPlugin))]
namespace net.puk06.TextureReplacer.Editor.Ndmf
{
    internal class NdmfPlugin : Plugin<NdmfPlugin>
    {
        public override string QualifiedName => "net.puk06.texture-replacer";
        public override string DisplayName => "Puko's Texture Replacer";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("net.rs64.tex-trans-tool")
                .AfterPlugin("nadena.dev.modular-avatar")
                .BeforePlugin("net.puk06.tex-stack-editor")
                .BeforePlugin("net.puk06.color-changer")
                .Run(ReplaceTextures.Instance)
#if LLC_2_4_0_OR_NEWER
                .BeforePass("io.github.azukimochi.light-limit-changer.normalize-materials")
#endif
                .PreviewingWith(new RealtimePreview());

            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("net.rs64.tex-trans-tool")
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run(RemoveComponents.Instance);
        }
    }

    internal class ReplaceTextures : Pass<ReplaceTextures>
    {
        protected override void Execute(BuildContext context)
        {
            GameObject avatar = context.AvatarRootObject;
            PukoTextureReplacer[] components = avatar.GetComponentsInChildren<PukoTextureReplacer>(false);

            Dictionary<Texture2D, Texture2D> processedTexturesDictionary = NdmfProcessor.ProcessAllComponents(components);
            IEnumerable<Renderer> renderers = avatar.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer or SkinnedMeshRenderer);
            NdmfProcessor.ReplaceTexturesInRenderers(renderers, processedTexturesDictionary);
        }
    }

    internal class RemoveComponents : Pass<RemoveComponents>
    {
        protected override void Execute(BuildContext context)
        {
            GameObject avatar = context.AvatarRootObject;
            PukoTextureReplacer[] components = avatar.GetComponentsInChildren<PukoTextureReplacer>(true);

            RemoveAllComponents(components);
        }

        private void RemoveAllComponents(IEnumerable<Component> components)
        {
            foreach (Component component in components)
            {
                if (component == null) continue;
                Object.DestroyImmediate(component);
            }
        }
    }
}
