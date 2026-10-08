// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// A <see cref="BufferedDrawNode"/> which blurs the framebuffer it is drawn into behind its content.
    /// </summary>
    /// <remarks>
    /// The main buffer contains the content as usual. Every frame, the region of the currently bound framebuffer
    /// (that of the nearest <see cref="Containers.IBackbufferProvider"/>) behind the content is blurred into the effect buffers,
    /// after which both are combined by <see cref="IBackdropBlurDrawable.BlendShader"/>.
    /// The blend shader receives the main buffer as its first texture and the blurred backdrop at <see cref="BackdropTextureUnit"/>.
    /// </remarks>
    public class BackdropBlurDrawNode : BufferedDrawNode
    {
        public BackdropBlurDrawNode(IBackdropBlurDrawable source, DrawNode child, BackdropBlurDrawNodeSharedData sharedData)
            : base(source, child, sharedData)
        {
        }

        protected new IBackdropBlurDrawable Source => (IBackdropBlurDrawable)base.Source;

        /// <summary>
        /// The texture unit which the blurred backdrop is bound to while drawing with <see cref="IBackdropBlurDrawable.BlendShader"/>.
        /// </summary>
        protected virtual int BackdropTextureUnit => 1;

        private Vector2 blurSigma;
        private Vector2I blurRadius;
        private float blurRotation;

        private float maskCutoff;
        private float backdropOpacity;
        private float backdropTintStrength;

        private Vector2 effectBufferSize;
        private RectangleF? backbufferDrawRectangle;

        private IShader blurShader;
        private IShader blendShader;

        private IUniformBuffer<BlurParameters> blurParametersBuffer;
        private IUniformBuffer<BlendParameters> blendParametersBuffer;

        /// <summary>
        /// Whether the effect buffers contain the blurred backdrop for the current frame.
        /// </summary>
        private bool backdropPopulated;

        public override void ApplyState()
        {
            base.ApplyState();

            Vector2 effectBufferScale = Source.EffectBufferScale;
            effectBufferSize = new Vector2(MathF.Ceiling(DrawRectangle.Width * effectBufferScale.X), MathF.Ceiling(DrawRectangle.Height * effectBufferScale.Y));

            blurSigma = Source.BlurSigma * effectBufferScale;
            blurRadius = new Vector2I(Blur.KernelSize(blurSigma.X), Blur.KernelSize(blurSigma.Y));
            blurRotation = Source.BlurRotation;

            maskCutoff = Source.MaskCutoff;
            backdropOpacity = Source.BackdropOpacity;
            backdropTintStrength = Source.BackdropTintStrength;

            backbufferDrawRectangle = Source.BackbufferDrawRectangle;

            blurShader = Source.BlurShader;
            blendShader = Source.BlendShader;
        }

        private bool backdropEnabled =>
            (blurRadius.X > 0 || blurRadius.Y > 0)
            && backdropOpacity > 0
            && backbufferDrawRectangle != null
            && effectBufferSize.X >= 1 && effectBufferSize.Y >= 1;

        // The backdrop may change at any time, so it has to be blurred every frame.
        protected override bool RequiresEffectBufferRedraw => backdropEnabled || base.RequiresEffectBufferRedraw;

        protected override void PopulateContents(IRenderer renderer)
        {
            base.PopulateContents(renderer);

            backdropPopulated = false;

            if (!backdropEnabled)
                return;

            // The framebuffer we are being drawn into, which is expected to be that of the nearest backbuffer provider.
            // If we are drawn directly to the backbuffer (i.e. the provider has not activated yet), there is nothing to blur.
            IFrameBuffer backbuffer = renderer.FrameBuffer;
            if (backbuffer == null)
                return;

            renderer.PushScissorState(false);
            renderer.PushDepthInfo(new DepthInfo(false));

            // The first pass samples the backbuffer, positioned such that the region behind this drawable fills the effect buffer.
            RectangleF backbufferRect = backbufferDrawRectangle.Value.RelativeIn(DrawRectangle) * effectBufferSize;

            if (blurRadius.X > 0)
            {
                drawBlurredFrameBuffer(renderer, backbuffer, backbufferRect, blurRadius.X, blurSigma.X, blurRotation);
                backbuffer = null;
            }

            if (blurRadius.Y > 0)
            {
                if (backbuffer != null)
                    drawBlurredFrameBuffer(renderer, backbuffer, backbufferRect, blurRadius.Y, blurSigma.Y, blurRotation + 90);
                else
                {
                    IFrameBuffer current = SharedData.CurrentEffectBuffer;
                    drawBlurredFrameBuffer(renderer, current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), blurRadius.Y, blurSigma.Y, blurRotation + 90);
                }
            }

            renderer.PopDepthInfo();
            renderer.PopScissorState();

            backdropPopulated = true;
        }

        private void drawBlurredFrameBuffer(IRenderer renderer, IFrameBuffer source, RectangleF sourceRect, int kernelRadius, float sigma, float rotation)
        {
            blurParametersBuffer ??= renderer.CreateUniformBuffer<BlurParameters>();

            IFrameBuffer target = SharedData.GetNextEffectBuffer();

            renderer.SetBlend(BlendingParameters.None);

            using (BindFrameBuffer(target))
            {
                // The effect buffers may be smaller than the main buffer.
                renderer.PushViewport(new RectangleI(0, 0, (int)target.Size.X, (int)target.Size.Y));

                // Parts of the effect buffer may not be covered by the source (e.g. if this drawable extends beyond the backbuffer provider).
                renderer.Clear(new ClearInfo(Color4.Transparent));

                float radians = float.DegreesToRadians(rotation);

                blurParametersBuffer.Data = blurParametersBuffer.Data with
                {
                    Radius = kernelRadius,
                    Sigma = sigma,
                    // The blur kernel steps by single pixels of the effect buffer, regardless of the resolution of the source.
                    TexSize = sourceRect.Size,
                    Direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians))
                };

                blurShader.BindUniformBlock("m_BlurParameters", blurParametersBuffer);
                blurShader.Bind();
                renderer.DrawFrameBuffer(source, sourceRect, ColourInfo.SingleColour(Color4.White));
                blurShader.Unbind();

                renderer.PopViewport();
            }
        }

        protected override void DrawContents(IRenderer renderer)
        {
            if (!backdropPopulated || !backdropEnabled)
            {
                base.DrawContents(renderer);
                return;
            }

            renderer.SetBlend(DrawColourInfo.Blending);

            blendParametersBuffer ??= renderer.CreateUniformBuffer<BlendParameters>();
            blendParametersBuffer.Data = blendParametersBuffer.Data with
            {
                MaskCutoff = maskCutoff,
                BackdropOpacity = backdropOpacity,
                BackdropTintStrength = backdropTintStrength,
            };

            renderer.BindTexture(SharedData.CurrentEffectBuffer.Texture, BackdropTextureUnit);

            blendShader.BindUniformBlock("m_BackdropBlendParameters", blendParametersBuffer);
            blendShader.Bind();
            renderer.DrawFrameBuffer(SharedData.MainBuffer, DrawRectangle, DrawColourInfo.Colour);
            blendShader.Unbind();
        }

        protected override Vector2 GetFrameBufferSize(IFrameBuffer frameBuffer)
        {
            if (frameBuffer != SharedData.MainBuffer)
                return effectBufferSize;

            return base.GetFrameBufferSize(frameBuffer);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            blurParametersBuffer?.Dispose();
            blendParametersBuffer?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct BlurParameters
        {
            public UniformVector2 TexSize;
            public UniformInt Radius;
            public UniformFloat Sigma;
            public UniformVector2 Direction;
            private readonly UniformPadding8 pad1;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct BlendParameters
        {
            public UniformFloat MaskCutoff;
            public UniformFloat BackdropOpacity;
            public UniformFloat BackdropTintStrength;
            private readonly UniformPadding4 pad1;
        }
    }

    /// <summary>
    /// Data shared between the <see cref="BackdropBlurDrawNode"/>s of a single <see cref="IBackdropBlurDrawable"/>.
    /// </summary>
    public class BackdropBlurDrawNodeSharedData : BufferedDrawNodeSharedData
    {
        /// <param name="mainBufferTextureFormat">The texture format of the main buffer, which contains the content.</param>
        /// <param name="mainBufferFormats">The render buffer formats attached to the main buffer.</param>
        public BackdropBlurDrawNodeSharedData(TexturePixelFormat mainBufferTextureFormat = TexturePixelFormat.R8G8B8A8Float, RenderBufferFormat[] mainBufferFormats = null)
            : base(2, mainBufferTextureFormat, mainBufferFormats, clipToRootNode: true)
        {
        }
    }
}
