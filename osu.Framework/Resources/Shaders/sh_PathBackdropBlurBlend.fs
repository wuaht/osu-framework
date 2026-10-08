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
};

layout(std140, set = 4, binding = 0) uniform m_BackdropBlendParameters
{
	lowp float g_MaskCutoff;
	lowp float g_BackdropOpacity;
	lowp float g_BackdropTintStrength;
};

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    // Same as sh_Path.fs.
    mediump float dstFromEdge = texture(sampler2D(m_Texture, m_Sampler), v_TexCoord).r;
    lowp vec4 pathCol = texture(sampler2D(m_Texture1, m_Sampler1), TexRect1.xy + vec2(dstFromEdge, 0.0) * TexRect1.zw, -0.9);
    lowp vec4 texel = vec4(pathCol.rgb, pathCol.a * float(dstFromEdge > 0.0));

    lowp vec4 backdrop = texture(sampler2D(m_Texture2, m_Sampler2), v_TexCoord);

    o_Colour = blendWithBackdrop(texel, backdrop, v_TexCoord, g_MaskCutoff, g_BackdropOpacity, g_BackdropTintStrength);
}

#endif
