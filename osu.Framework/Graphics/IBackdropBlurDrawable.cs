// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osuTK;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// A drawable that blurs the contents of its nearest <see cref="IBackbufferProvider"/> behind itself.
    /// </summary>
    public interface IBackdropBlurDrawable : IBufferedDrawable
    {
        /// <summary>
        /// Controls the amount of blurring in two orthogonal directions (X and Y if
        /// <see cref="BlurRotation"/> is zero).
        /// Blur is parametrized by a gaussian image filter. This property controls
        /// the standard deviation (sigma) of the gaussian kernel.
        /// </summary>
        Vector2 BlurSigma { get; }

        /// <summary>
        /// Rotates the blur kernel clockwise. In degrees. Has no effect if
        /// <see cref="BlurSigma"/> has the same magnitude in both directions.
        /// </summary>
        float BlurRotation { get; }

        /// <summary>
        /// The opacity at which the blurred backdrop is drawn.
        /// </summary>
        float BackdropOpacity { get; }

        /// <summary>
        /// The alpha of the content at or below which the content is not considered opaque enough for the backdrop to be blurred behind it.
        /// </summary>
        float MaskCutoff { get; }

        /// <summary>
        /// Controls how much the blurred backdrop is tinted by the colour of the content.
        /// </summary>
        float BackdropTintStrength { get; }

        /// <summary>
        /// The scale of the framebuffers the backdrop is blurred in, relative to the size of this drawable.
        /// Lower values are cheaper but produce a coarser blur.
        /// </summary>
        Vector2 EffectBufferScale { get; }

        /// <summary>
        /// The shader used to blur the backdrop.
        /// </summary>
        IShader BlurShader { get; }

        /// <summary>
        /// The shader used to blend the content with the blurred backdrop.
        /// </summary>
        IShader BlendShader { get; }

        /// <summary>
        /// The screen-space rectangle covered by the framebuffer of the nearest <see cref="IBackbufferProvider"/>,
        /// or null if there is no <see cref="IBackbufferProvider"/>.
        /// </summary>
        RectangleF? BackbufferDrawRectangle { get; }
    }
}
