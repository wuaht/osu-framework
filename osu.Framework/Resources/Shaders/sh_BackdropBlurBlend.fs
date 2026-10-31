#ifndef BACKDROP_BLUR_BLEND_FS
#define BACKDROP_BLUR_BLEND_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_BackdropBlurUtils.h"

layout(location = 2) in mediump vec2 v_TexCoord;

// Content
layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

// Blurred backdrop
layout(set = 1, binding = 0) uniform lowp texture2D m_Texture1;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler1;

layout(std140, set = 2, binding = 0) uniform m_BackdropBlendParameters
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
    lowp vec4 backdrop = texture(sampler2D(m_Texture1, m_Sampler1), uv);

    if (g_ChromaticAberration > 0.0)
    {
        highp vec2 dispersion = dispersionOffset / g_DrawSize;
        backdrop.r = texture(sampler2D(m_Texture1, m_Sampler1), clamp(uv + dispersion, 0.0, 1.0)).r;
        backdrop.b = texture(sampler2D(m_Texture1, m_Sampler1), clamp(uv - dispersion, 0.0, 1.0)).b;
    }

    backdrop.rgb *= 1.0 - g_Darkness;
    return backdrop;
}

highp float insideShape(highp vec2 position)
{
    return roundedRectInside(position, g_ShapeRect, g_ShapeRadius);
}

void main(void)
{
    lowp vec4 texel = texture(sampler2D(m_Texture, m_Sampler), v_TexCoord);
    lowp vec4 backdrop;
    lowp vec3 highlight = vec3(0.0);

    if (g_Bevel > 0.0)
    {
        highp vec2 position = v_TexCoord * g_DrawSize;
        highp vec2 delta = vec2(LIQUID_GLASS_STEP, 0.0);

        highp vec2 heightGradient = vec2(
            liquidGlassHeight(insideShape(position + delta.xy), g_Bevel) - liquidGlassHeight(insideShape(position - delta.xy), g_Bevel),
            liquidGlassHeight(insideShape(position + delta.yx), g_Bevel) - liquidGlassHeight(insideShape(position - delta.yx), g_Bevel)) / (2.0 * LIQUID_GLASS_STEP);

        highp vec2 refractionOffset;
        highp vec2 dispersionOffset;
        liquidGlass(heightGradient, insideShape(position), g_Bevel, g_Refraction, g_ChromaticAberration, g_Highlight, refractionOffset, dispersionOffset, highlight);

        backdrop = sampleLiquidGlassBackdrop(refractionOffset, dispersionOffset);
    }
    else
    {
        backdrop = texture(sampler2D(m_Texture1, m_Sampler1), v_TexCoord);
    }

    o_Colour = blendWithBackdrop(texel, backdrop, v_TexCoord, g_MaskCutoff, g_BackdropOpacity, g_BackdropTintStrength, g_BackdropUnderlay, highlight);
}

#endif
