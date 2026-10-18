#ifndef TEXTURE_TINT_FS
#define TEXTURE_TINT_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

// Like sh_Texture.fs, but replaces the hue of the texture with the vertex colour rather than multiplying both,
// so that coloured parts of the texture are displayed in the vertex colour, while white / grey parts stay as they are.
void main(void)
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 texel = wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9);

    lowp float value = max(texel.r, max(texel.g, texel.b));
    lowp float saturation = value > 0.0 ? (value - min(texel.r, min(texel.g, texel.b))) / value : 0.0;

    lowp vec3 colour = value * mix(vec3(1.0), v_Colour.rgb, saturation);

    o_Colour = getRoundedColorForContent(vec4(colour, texel.a * v_Colour.a), wrappedCoord);
}

#endif
