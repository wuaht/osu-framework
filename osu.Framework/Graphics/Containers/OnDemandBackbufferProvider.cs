// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which only draws its children to a framebuffer while a descendant requests it via <see cref="RequestBackbuffer"/>.
    /// This avoids the cost of an always-active <see cref="BufferedContainer"/> when no descendant needs a framebuffer.
    /// </summary>
    public partial class OnDemandBackbufferProvider : Container, IBackbufferProvider
    {
        /// <summary>
        /// The duration for which the framebuffer is kept after the last request,
        /// to avoid repeatedly re-parenting the content when requests are intermittent.
        /// </summary>
        public double InactivityTimeout { get; set; } = 5000;

        private readonly Container content = new Container { RelativeSizeAxes = Axes.Both };

        protected override Container<Drawable> Content => content;

        private BufferedContainer? bufferedContainer;

        private bool requestedThisFrame;
        private double lastRequestTime;

        public OnDemandBackbufferProvider()
        {
            AddInternal(content);
        }

        public RectangleF BackbufferDrawRectangle => ((IBackbufferProvider?)bufferedContainer)?.BackbufferDrawRectangle ?? ScreenSpaceDrawQuad.AABBFloat;

        /// <summary>
        /// Whether the children are currently being drawn to a framebuffer.
        /// </summary>
        public bool IsActive => bufferedContainer != null;

        public void RequestBackbuffer() => requestedThisFrame = true;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (requestedThisFrame)
            {
                requestedThisFrame = false;
                lastRequestTime = Time.Current;

                if (bufferedContainer == null)
                    activate();
            }
            else if (bufferedContainer != null && Time.Current - lastRequestTime > InactivityTimeout)
                deactivate();
        }

        private void activate()
        {
            Logger.Log($@"{nameof(OnDemandBackbufferProvider)} became active.");

            RemoveInternal(content, false);
            AddInternal(bufferedContainer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = content,
            });
        }

        private void deactivate()
        {
            Logger.Log($@"{nameof(OnDemandBackbufferProvider)} became inactive.");

            bufferedContainer!.Remove(content, false);
            RemoveInternal(bufferedContainer, true);
            bufferedContainer = null;

            AddInternal(content);
        }
    }
}
