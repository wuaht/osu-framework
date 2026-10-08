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
};

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    lowp vec4 texel = texture(sampler2D(m_Texture, m_Sampler), v_TexCoord);
    lowp vec4 backdrop = texture(sampler2D(m_Texture1, m_Sampler1), v_TexCoord);

    o_Colour = blendWithBackdrop(texel, backdrop, v_TexCoord, g_MaskCutoff, g_BackdropOpacity, g_BackdropTintStrength);
}

#endif
