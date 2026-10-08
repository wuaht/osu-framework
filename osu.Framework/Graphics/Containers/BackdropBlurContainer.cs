// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which blurs the contents of its nearest <see cref="IBackbufferProvider"/> behind its children ("frosted glass").
    /// If all children are of a specific non-<see cref="Drawable"/> type, use the
    /// generic version <see cref="BackdropBlurContainer{T}"/>.
    /// </summary>
    public partial class BackdropBlurContainer : BackdropBlurContainer<Drawable>
    {
        /// <inheritdoc />
        public BackdropBlurContainer(RenderBufferFormat[] formats = null)
            : base(formats)
        {
        }
    }

    /// <summary>
    /// A container which blurs the contents of its nearest <see cref="IBackbufferProvider"/> behind its children ("frosted glass").
    /// </summary>
    /// <remarks>
    /// The backdrop is only blurred while <see cref="BlurSigma"/> is non-zero and an <see cref="IBackbufferProvider"/> is available.
    /// Otherwise, the children are drawn as with a regular <see cref="BufferedContainer"/>.
    /// </remarks>
    public partial class BackdropBlurContainer<T> : Container<T>, IBackdropBlurDrawable
        where T : Drawable
    {
        /// <inheritdoc/>
        public Vector2 BlurSigma { get; set; }

        /// <inheritdoc/>
        public float BlurRotation { get; set; }

        /// <inheritdoc/>
        public float MaskCutoff { get; set; }

        /// <inheritdoc/>
        public float BackdropTintStrength { get; set; }

        /// <inheritdoc/>
        public Vector2 EffectBufferScale { get; set; } = Vector2.One;

        /// <inheritdoc/>
        public virtual float BackdropOpacity => 1 - MathF.Pow(1 - base.DrawColourInfo.Colour.MaxAlpha, 2);

        [Resolved(CanBeNull = true)]
        private IBackbufferProvider backbufferProvider { get; set; }

        public IShader TextureShader { get; private set; }

        private IShader blurShader;
        private IShader blendShader;

        IShader IBackdropBlurDrawable.BlurShader => blurShader;
        IShader IBackdropBlurDrawable.BlendShader => blendShader;

        private RectangleF? backbufferDrawRectangle;

        RectangleF? IBackdropBlurDrawable.BackbufferDrawRectangle => backbufferDrawRectangle;

        private readonly BackdropBlurDrawNodeSharedData sharedData;

        /// <summary>
        /// Constructs an empty backdrop blur container.
        /// </summary>
        /// <param name="formats">The render buffer formats attached to the frame buffer of this <see cref="BackdropBlurContainer{T}"/>.</param>
        public BackdropBlurContainer(RenderBufferFormat[] formats = null)
        {
            sharedData = new BackdropBlurDrawNodeSharedData(mainBufferFormats: formats);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
            blendShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BACKDROP_BLUR_BLEND);
        }

        protected override DrawNode CreateDrawNode() => new BackdropBlurContainerDrawNode(this, new CompositeDrawableDrawNode(this), sharedData);

        protected override RectangleF ComputeChildMaskingBounds() => ScreenSpaceDrawQuad.AABBFloat; // Make sure children never get masked away

        protected override void Update()
        {
            base.Update();

            if (backbufferProvider != null)
            {
                if (BlurSigma.X > 0 || BlurSigma.Y > 0)
                    backbufferProvider.RequestBackbuffer();

                backbufferDrawRectangle = backbufferProvider.BackbufferDrawRectangle;
            }

            // The children and the backdrop may change at any time, so the frame buffers are redrawn every frame.
            Invalidate(Invalidation.DrawNode);
        }

        public Color4 BackgroundColour => Color4.Transparent;

        public DrawColourInfo? FrameBufferDrawColour => base.DrawColourInfo;

        public Vector2 FrameBufferScale => Vector2.One;

        // Children should not receive the true colour to avoid colour doubling when the frame-buffers are rendered to the back-buffer.
        public override DrawColourInfo DrawColourInfo
        {
            get
            {
                // Todo: This is incorrect.
                var blending = Blending;
                blending.ApplyDefaultToInherited();

                return new DrawColourInfo(Color4.White, blending);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            sharedData.Dispose();
        }

        private class BackdropBlurContainerDrawNode : BackdropBlurDrawNode, ICompositeDrawNode
        {
            public BackdropBlurContainerDrawNode(BackdropBlurContainer<T> source, CompositeDrawableDrawNode child, BackdropBlurDrawNodeSharedData sharedData)
                : base(source, child, sharedData)
            {
            }

            protected new CompositeDrawableDrawNode Child => (CompositeDrawableDrawNode)base.Child;

            public List<DrawNode> Children
            {
                get => Child.Children;
                set => Child.Children = value;
            }

            public bool AddChildDrawNodes => RequiresRedraw;
        }
    }
}
