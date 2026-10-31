#ifndef PATH_BACKDROP_BLUR_BLEND_FS
#define PATH_BACKDROP_BLUR_BLEND_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_BackdropBlurUtils.h"

layout(location = 2) in mediump vec2 v_TexCoord;

// FrameBuffer texture (distance from the edge of the path)
layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

// Path texture
layout(set = 1, binding = 0) uniform lowp texture2D m_Texture1;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler1;

// Blurred backdrop
layout(set = 2, binding = 0) uniform lowp texture2D m_Texture2;
layout(set = 2, binding = 1) uniform lowp sampler m_Sampler2;

layout(std140, set = 3, binding = 0) uniform m_PathTextureParameters
{
	highp vec4 TexRect1;
	highp float g_GlassStart;
	highp float g_GlassSeparation;
};

layout(std140, set = 4, binding = 0) uniform m_BackdropBlendParameters
{
	lowp float g_MaskCutoff;
	lowp float g_BackdropOpacity;
	lowp float g_BackdropTintStrength;
	lowp vec4 g_BackdropUnderlay;
	highp vec4 g_ShapeRect;
	highp vec2 g_DrawSize;
	highp float g_ShapeRadius;
	highp float g_Bevel;
	mediump float g_Refraction;
	mediump float g_ChromaticAberration;
	mediump float g_Highlight;
	mediump float g_Darkness;
};

layout(location = 0) out vec4 o_Colour;

// Samples the backdrop behind liquid glass, see liquidGlass().
lowp vec4 sampleLiquidGlassBackdrop(highp vec2 refractionOffset, highp vec2 dispersionOffset)
{
    highp vec2 uv = clamp(v_TexCoord + refractionOffset / g_DrawSize, 0.0, 1.0);
    lowp vec4 backdrop = texture(sampler2D(m_Texture2, m_Sampler2), uv);

    if (g_ChromaticAberration > 0.0)
    {
        highp vec2 dispersion = dispersionOffset / g_DrawSize;
        backdrop.r = texture(sampler2D(m_Texture2, m_Sampler2), clamp(uv + dispersion, 0.0, 1.0)).r;
        backdrop.b = texture(sampler2D(m_Texture2, m_Sampler2), clamp(uv - dispersion, 0.0, 1.0)).b;
    }

    backdrop.rgb *= 1.0 - g_Darkness;
    return backdrop;
}

// The distance from the nearest edge of the piece of glass at a distance from the edge of the path (relative to its radius), and the width of the rounded edge of that piece, both in pixels.
// The glass starts at g_GlassStart, and is split into an outer ring and an inner piece at g_GlassSeparation.
highp vec2 pathGlass(highp float dstFromEdge)
{
    highp float inside = (dstFromEdge - g_GlassStart) * g_ShapeRadius;
    highp float separation = (g_GlassSeparation - g_GlassStart) * g_ShapeRadius;

    if (separation <= 0.0)
        return vec2(inside, g_Bevel);

    // The ring is rounded on both sides.
    if (inside < separation)
        return vec2(min(inside, separation - inside), min(g_Bevel, separation * 0.5));

    return vec2(inside - separation, g_Bevel);
}

// The height of the glass at an offset from the current pixel, based on the distance relative to the radius stored in the frame buffer.
highp float pathGlassHeight(highp vec2 offset)
{
    highp vec2 glass = pathGlass(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + offset / g_DrawSize).r);
    return liquidGlassHeight(glass.x, glass.y);
}

void main(void)
{
    // Same as sh_Path.fs.
    mediump float dstFromEdge = texture(sampler2D(m_Texture, m_Sampler), v_TexCoord).r;
    lowp vec4 pathCol = texture(sampler2D(m_Texture1, m_Sampler1), TexRect1.xy + vec2(dstFromEdge, 0.0) * TexRect1.zw, -0.9);
    lowp vec4 texel = vec4(pathCol.rgb, pathCol.a * float(dstFromEdge > 0.0));

    lowp vec4 backdrop;
    lowp vec3 highlight = vec3(0.0);

    if (g_Bevel > 0.0)
    {
        highp vec2 delta = vec2(LIQUID_GLASS_STEP, 0.0);

        highp vec2 heightGradient = vec2(
            pathGlassHeight(delta.xy) - pathGlassHeight(-delta.xy),
            pathGlassHeight(delta.yx) - pathGlassHeight(-delta.yx)) / (2.0 * LIQUID_GLASS_STEP);

        highp vec2 glass = pathGlass(dstFromEdge);
        highp vec2 refractionOffset;
        highp vec2 dispersionOffset;
        liquidGlass(heightGradient, glass.x, glass.y, g_Refraction, g_ChromaticAberration, g_Highlight, refractionOffset, dispersionOffset, highlight);

        backdrop = sampleLiquidGlassBackdrop(refractionOffset, dispersionOffset);
    }
    else
    {
        backdrop = texture(sampler2D(m_Texture2, m_Sampler2), v_TexCoord);
    }

    o_Colour = blendWithBackdrop(texel, backdrop, v_TexCoord, g_MaskCutoff, g_BackdropOpacity, g_BackdropTintStrength, g_BackdropUnderlay, highlight);
}

#endif
