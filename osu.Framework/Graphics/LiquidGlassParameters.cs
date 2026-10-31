// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Parameters of the "liquid glass" effect of an <see cref="IBackdropBlurDrawable"/>, which refracts and lights the backdrop
    /// as if it was seen through a slab of glass with rounded edges.
    /// </summary>
    /// <remarks>
    /// All sizes are in the local coordinate space of the drawable.
    /// </remarks>
    public readonly record struct LiquidGlassParameters
    {
        /// <summary>
        /// The width of the rounded edge of the glass, in which the backdrop is refracted. The effect is disabled while this is zero.
        /// </summary>
        public float Thickness { get; init; }

        /// <summary>
        /// The corner radius of the glass. Has no effect on paths, whose glass follows the shape of the path.
        /// </summary>
        public float CornerRadius { get; init; }

        /// <summary>
        /// The area covered by the glass, or null to cover the whole drawable. Has no effect on paths, whose glass follows the shape of the path.
        /// </summary>
        public RectangleF? Shape { get; init; }

        /// <summary>
        /// How strongly the backdrop is refracted at the edge of the glass.
        /// </summary>
        public float Refraction { get; init; }

        /// <summary>
        /// How strongly the colour channels of the backdrop are split up at the edge of the glass.
        /// </summary>
        public float ChromaticAberration { get; init; }

        /// <summary>
        /// The strength of the light reflections on the edge of the glass.
        /// </summary>
        public float Highlight { get; init; }

        /// <summary>
        /// How much the backdrop is darkened, from 0 (unchanged) to 1 (black).
        /// </summary>
        public float Darkness { get; init; }

        /// <summary>
        /// Whether the effect is enabled.
        /// </summary>
        public bool Enabled => Thickness > 0;
    }
}
