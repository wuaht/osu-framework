#ifndef BACKDROP_BLUR_UTILS_H
#define BACKDROP_BLUR_UTILS_H

#include "sh_Masking.h"

// Combines content with the blurred backdrop behind it.
// texel: the content colour (straight alpha), without the draw colour applied.
// backdrop: the blurred backdrop colour, as sampled from the effect buffer.
lowp vec4 blendWithBackdrop(lowp vec4 texel, lowp vec4 backdrop, mediump vec2 texCoord, lowp float maskCutoff, lowp float backdropOpacity, lowp float backdropTintStrength)
{
    // Regions of the content which are too transparent (e.g. shadows) do not receive a backdrop.
    if (texel.a <= maskCutoff)
        return getRoundedColor(texel, texCoord);

    lowp vec4 foreground = texel * v_Colour;
    lowp vec4 background = backdrop * backdropOpacity;

    lowp vec4 result;

    if (background.a > 0.0)
    {
        // Tint the backdrop by the content colour. The content colour is not premultiplied, so it must not be divided by its alpha
        // (doing so would over-brighten the backdrop of very transparent content).
        lowp vec3 backgroundColour = background.rgb / background.a;
        background.rgb = mix(backgroundColour, backgroundColour * foreground.rgb, backdropTintStrength * foreground.a) * background.a;

        lowp float alpha = background.a + (1.0 - background.a) * foreground.a;
        result = vec4(mix(background.rgb, foreground.rgb, foreground.a) / alpha, alpha);
    }
    else
    {
        result = foreground;
    }

    // Apply masking. The draw colour is already part of the result, so only the alpha factor of the masking is applied.
    if (g_IsMasking || v_BlendRange != vec2(0.0))
        result.a *= v_Colour.a > 0.0 ? getRoundedColor(vec4(1.0), texCoord).a / v_Colour.a : 0.0;

    return result;
}

#endif
