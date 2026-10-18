#ifndef TEXTURE_MONOCHROME_FS
#define TEXTURE_MONOCHROME_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

// Like sh_Texture.fs, but discards the hue of the texture before applying the vertex colour,
// so that the vertex colour is displayed as-is regardless of the colour of the texture.
void main(void)
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 texel = wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9);

    // the brightest channel is used rather than the luminance, so that saturated textures (e.g. pure blue) keep their brightness.
    texel.rgb = vec3(max(texel.r, max(texel.g, texel.b)));

    o_Colour = getRoundedColor(texel, wrappedCoord);
}

#endif
