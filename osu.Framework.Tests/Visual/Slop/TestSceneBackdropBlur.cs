// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Slop
{
    [Category("slop")]
    public partial class TestSceneBackdropBlur : FrameworkTestScene
    {
        private OnDemandBackbufferProvider provider = null!;
        private BackdropBlurContainer container = null!;
        private BackdropBlurPath path = null!;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = provider = new OnDemandBackbufferProvider
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.YellowGreenDark,
                    },
                    createStripes(),
                    container = new BackdropBlurContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Position = new Vector2(0, -150),
                        Size = new Vector2(400, 150),
                        BlurSigma = new Vector2(10),
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Red,
                            Alpha = 0.5f,
                        },
                    },
                    path = new GradientPath
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Position = new Vector2(0, 150),
                        PathRadius = 50,
                        BlurSigma = new Vector2(10),
                        BackdropTintStrength = 0.5f,
                        Vertices = new[]
                        {
                            new Vector2(0, 0),
                            new Vector2(150, 50),
                            new Vector2(250, -25),
                            new Vector2(400, 25)
                        },
                    },
                },
            };
        });

        [Test]
        public void TestProviderActivation()
        {
            AddUntilStep("provider active", () => provider.IsActive);

            AddStep("disable blur", () =>
            {
                provider.InactivityTimeout = 0;
                container.BlurSigma = Vector2.Zero;
                path.BlurSigma = Vector2.Zero;
            });
            AddUntilStep("provider inactive", () => !provider.IsActive);

            AddStep("enable path blur", () => path.BlurSigma = new Vector2(10));
            AddUntilStep("provider active", () => provider.IsActive);
        }

        [Test]
        public void TestParameters()
        {
            AddSliderStep("blur", 0f, 30f, 10f, blur =>
            {
                if (path == null) return;

                container.BlurSigma = new Vector2(blur);
                path.BlurSigma = new Vector2(blur);
            });

            AddSliderStep("alpha", 0f, 1f, 1f, alpha =>
            {
                if (path == null) return;

                container.Alpha = alpha;
                path.Alpha = alpha;
            });

            AddSliderStep("mask cutoff", 0f, 1f, 0f, cutoff =>
            {
                if (path == null) return;

                container.MaskCutoff = cutoff;
                path.MaskCutoff = cutoff;
            });

            AddSliderStep("tint strength", 0f, 1f, 0.5f, tint =>
            {
                if (path == null) return;

                container.BackdropTintStrength = tint;
                path.BackdropTintStrength = tint;
            });

            AddSliderStep("effect buffer scale", 0.05f, 2f, 1f, scale =>
            {
                if (path == null) return;

                container.EffectBufferScale = new Vector2(scale);
                path.EffectBufferScale = new Vector2(scale);
            });
        }

        private static Drawable createStripes()
        {
            var stripes = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
            };

            for (int i = 0; i < 40; i++)
            {
                stripes.Add(new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = 20,
                    Colour = i % 2 == 0 ? Color4.White : Color4.Black,
                });
            }

            stripes.OnLoadComplete += d => d.Spin(20000, RotationDirection.Clockwise);

            return new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(800),
                Child = stripes,
            };
        }

        private partial class GradientPath : BackdropBlurPath
        {
            protected override Color4 ColourAt(float position) => base.ColourAt(position) with { A = 0.5f + position * 0.5f };
        }
    }
}
