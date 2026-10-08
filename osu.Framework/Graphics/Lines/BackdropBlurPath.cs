// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Lines
{
    /// <summary>
    /// A <see cref="SmoothPath"/> which blurs the contents of its nearest <see cref="IBackbufferProvider"/> behind itself ("frosted glass").
    /// </summary>
    /// <remarks>
    /// The backdrop is only blurred while <see cref="BlurSigma"/> is non-zero and an <see cref="IBackbufferProvider"/> is available.
    /// Otherwise, this behaves exactly like a regular <see cref="SmoothPath"/>.
    /// </remarks>
    public partial class BackdropBlurPath : SmoothPath, IBackdropBlurDrawable
    {
        private Vector2 blurSigma;

        /// <inheritdoc/>
        public Vector2 BlurSigma
        {
            get => blurSigma;
            set
            {
                if (blurSigma == value)
                    return;

                blurSigma = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float blurRotation;

        /// <inheritdoc/>
        public float BlurRotation
        {
            get => blurRotation;
            set
            {
                if (blurRotation == value)
                    return;

                blurRotation = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float maskCutoff;

        /// <inheritdoc/>
        public float MaskCutoff
        {
            get => maskCutoff;
            set
            {
                if (maskCutoff == value)
                    return;

                maskCutoff = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float backdropTintStrength;

        /// <inheritdoc/>
        public float BackdropTintStrength
        {
            get => backdropTintStrength;
            set
            {
                if (backdropTintStrength == value)
                    return;

                backdropTintStrength = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Vector2 effectBufferScale = Vector2.One;

        /// <inheritdoc/>
        public Vector2 EffectBufferScale
        {
            get => effectBufferScale;
            set
            {
                if (effectBufferScale == value)
                    return;

                effectBufferScale = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        /// <inheritdoc/>
        public virtual float BackdropOpacity => MathF.Min(1, (FrameBufferDrawColour?.Colour.MaxAlpha ?? 1) * 2.5f);

        [Resolved(CanBeNull = true)]
        private IBackbufferProvider backbufferProvider { get; set; }

        private IShader downsampleShader;
        private IShader blurShader;
        private IShader blendShader;

        IShader IBackdropBlurDrawable.DownsampleShader => downsampleShader;
        IShader IBackdropBlurDrawable.BlurShader => blurShader;
        IShader IBackdropBlurDrawable.BlendShader => blendShader;

        private RectangleF? backbufferDrawRectangle;

        RectangleF? IBackdropBlurDrawable.BackbufferDrawRectangle => backbufferDrawRectangle;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            downsampleShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BACKDROP_DOWNSAMPLE);
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
            blendShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.PATH_BACKDROP_BLUR_BLEND);
        }

        protected override void Update()
        {
            base.Update();

            if (backbufferProvider == null)
                return;

            if (BlurSigma.X > 0 || BlurSigma.Y > 0)
                backbufferProvider.RequestBackbuffer();

            RectangleF rect = backbufferProvider.BackbufferDrawRectangle;

            if (backbufferDrawRectangle == null || !backbufferDrawRectangle.Value.Equals(rect))
            {
                backbufferDrawRectangle = rect;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private protected override BufferedDrawNodeSharedData CreateSharedData() => new BackdropBlurDrawNodeSharedData(TexturePixelFormat.R16Float);

        protected override DrawNode CreateDrawNode() => new BackdropBlurPathDrawNode(this, new PathDrawNode(this), (BackdropBlurDrawNodeSharedData)SharedData);

        private class BackdropBlurPathDrawNode : BackdropBlurDrawNode
        {
            protected new BackdropBlurPath Source => (BackdropBlurPath)base.Source;

            // Texture unit 1 holds the texture of the path (see BindUniformResources).
            protected override int BackdropTextureUnit => 2;

            private long pathInvalidationID = -1;
            private Texture texture;
            private Vector4 textureRect;
            private IUniformBuffer<PathTextureParameters> parametersBuffer;

            public BackdropBlurPathDrawNode(BackdropBlurPath source, PathDrawNode child, BackdropBlurDrawNodeSharedData sharedData)
                : base(source, child, sharedData)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                pathInvalidationID = Source.PathInvalidationID;
                texture = Source.Texture;

                var rect = texture.GetTextureRect();
                textureRect = new Vector4(rect.Left, rect.Top, rect.Width, rect.Height);
            }

            // Matches Path.PathBufferedDrawNode, such that both the path shader and the blend shader can map the distance to the path texture.
            protected override void BindUniformResources(IShader shader, IRenderer renderer)
            {
                base.BindUniformResources(shader, renderer);

                parametersBuffer ??= renderer.CreateUniformBuffer<PathTextureParameters>();
                parametersBuffer.Data = new PathTextureParameters
                {
                    TexRect1 = textureRect,
                };
                shader.BindUniformBlock("m_PathTextureParameters", parametersBuffer);

                texture?.Bind(1);
            }

            protected override long GetDrawVersion() => pathInvalidationID;

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                parametersBuffer?.Dispose();
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct PathTextureParameters
            {
                public UniformVector4 TexRect1;
            }
        }
    }
}
