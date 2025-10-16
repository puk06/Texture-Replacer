#nullable enable
using nadena.dev.ndmf;
using net.puk06.TextureReplacer.NDMF;

[assembly: ExportsPlugin(typeof(NDMFPlugin))]
namespace net.puk06.TextureReplacer.NDMF
{
    internal class NDMFPlugin : Plugin<NDMFPlugin>
    {
        public override string QualifiedName => "net.puk06.texture-replacer";
        public override string DisplayName => "Puko's Texture Replacer";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("net.rs64.tex-trans-tool")
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run(TextureReplacer.Instance)
                .PreviewingWith(new NDMFPreview());
        }
    }
}
