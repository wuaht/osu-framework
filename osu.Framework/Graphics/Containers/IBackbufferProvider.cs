// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which draws its children to a framebuffer.
    /// <see cref="IBackdropBlurDrawable"/>s blur the contents of their nearest <see cref="IBackbufferProvider"/>.
    /// </summary>
    [Cached]
    public interface IBackbufferProvider : IContainer
    {
        /// <summary>
        /// The screen-space rectangle which is covered by the framebuffer that the children of this <see cref="IBackbufferProvider"/> are drawn to.
        /// </summary>
        RectangleF BackbufferDrawRectangle { get; }

        /// <summary>
        /// Requests the children of this <see cref="IBackbufferProvider"/> to be drawn to a framebuffer.
        /// Must be called every update frame by drawables which depend on the framebuffer.
        /// </summary>
        void RequestBackbuffer();
    }
}
