#ifndef BACKDROP_BLUR_UTILS_H
#define BACKDROP_BLUR_UTILS_H

#include "sh_Masking.h"

// The height of the edge of liquid glass (a quarter circle) at a distance from the edge of the glass.
highp float liquidGlassHeight(highp float inside, highp float bevel)
{
    highp float d = clamp(inside, 0.0, bevel);
    return sqrt(d * (2.0 * bevel - d));
}

// The distance of a point inside a rounded rectangle from its edge (negative outside).
highp float roundedRectInside(highp vec2 position, highp vec4 rect, highp float cornerRadius)
{
    highp vec2 halfSize = rect.zw * 0.5;
    highp float radius = min(cornerRadius, min(halfSize.x, halfSize.y));
    highp vec2 q = abs(position - rect.xy - halfSize) - halfSize + radius;
    return radius - min(max(q.x, q.y), 0.0) - length(max(q, 0.0));
}

// The step used to determine the slope of liquid glass, in pixels.
const highp float LIQUID_GLASS_STEP = 1.5;

// Refracts and lights the backdrop behind liquid glass, based on the slope of its surface.
// heightGradient: the gradient of liquidGlassHeight(), pointing towards the inside of the glass.
// inside: the distance from the edge of the glass, in pixels.
// refractionOffset, dispersionOffset: in pixels, where the backdrop should be sampled relative to the current pixel. The red and blue channels are offset by +/- dispersionOffset.
// highlight: the light reflected by the glass, to be added onto the result.
void liquidGlass(highp vec2 heightGradient, highp float inside, highp float bevel, mediump float refraction, mediump float chromaticAberration, mediump float highlightStrength,
                 out highp vec2 refractionOffset, out highp vec2 dispersionOffset, out lowp vec3 highlight)
{
    mediump vec3 normal = normalize(vec3(-heightGradient, 1.0));
    mediump float edge = 1.0 - smoothstep(0.0, bevel, inside);

    // The backdrop is pulled inwards like through a convex lens.
    refractionOffset = heightGradient * refraction * bevel * 0.5;
    dispersionOffset = normal.xy * chromaticAberration * bevel * 0.25 * (edge * 0.7 + 0.3);

    // Specular reflections of a light at the top left and a weaker one at the bottom right (in screen space, y pointing down).
    const mediump vec3 view = vec3(0.0, 0.0, 1.0);
    mediump vec3 halfway1 = normalize(normalize(vec3(-0.4, -0.7, 1.0)) + view);
    mediump vec3 halfway2 = normalize(normalize(vec3(0.3, 0.5, 1.0)) + view);
    mediump float specular = pow(max(dot(normal, halfway1), 0.0), 60.0) * 0.9 + pow(max(dot(normal, halfway2), 0.0), 40.0) * 0.35;

    // Steep parts of the surface reflect more light (fresnel), and a thin stroke outlines the glass.
    mediump float fresnel = pow(1.0 - normal.z, 2.0) * 0.3;
    mediump float stroke = (1.0 - smoothstep(0.0, LIQUID_GLASS_STEP, inside)) * 0.4;

    highlight = vec3((specular + fresnel + stroke) * highlightStrength * smoothstep(-0.5, 0.5, inside));
}

// Combines content with the blurred backdrop behind it.
// texel: the content colour (straight alpha), without the draw colour applied.
// backdrop: the blurred backdrop colour, as sampled from the effect buffer.
// backdropUnderlay: the colour behind the backdrop (premultiplied alpha), which shows through where the backdrop is transparent.
// highlight: light added on top of the result (see liquidGlass()).
lowp vec4 blendWithBackdrop(lowp vec4 texel, lowp vec4 backdrop, mediump vec2 texCoord, lowp float maskCutoff, lowp float backdropOpacity, lowp float backdropTintStrength, lowp vec4 backdropUnderlay, lowp vec3 highlight)
{
    // Regions of the content which are too transparent (e.g. shadows) do not receive a backdrop.
    if (texel.a <= maskCutoff)
        return getRoundedColor(texel, texCoord);

    lowp vec4 foreground = texel * v_Colour;
    lowp vec4 background = (backdrop + (1.0 - backdrop.a) * backdropUnderlay) * backdropOpacity;

    lowp vec4 result;

    if (background.a > 0.0)
    {
        // Tint the backdrop with the hue and saturation of the content colour, while keeping the brightness of the backdrop,
        // such that the content colour is clearly visible while the shapes behind it remain recognisable.
        // The tint is intentionally independent of the alpha of the content, such that transparent content still tints the backdrop.
        const lowp vec3 luma = vec3(0.299, 0.587, 0.114);

        lowp vec3 backgroundColour = background.rgb / background.a;
        lowp vec3 tintedColour = min(foreground.rgb * dot(backgroundColour, luma) / max(dot(foreground.rgb, luma), 0.001), vec3(1.0));

        background.rgb = mix(backgroundColour, tintedColour, backdropTintStrength) * background.a;

        lowp float alpha = background.a + (1.0 - background.a) * foreground.a;
        result = vec4(mix(background.rgb, foreground.rgb, foreground.a) / alpha, alpha);
    }
    else
    {
        result = foreground;
    }

    result.rgb = min(result.rgb + highlight * backdropOpacity, vec3(1.0));

    // Apply masking. The draw colour is already part of the result, so only the alpha factor of the masking is applied.
    if (g_IsMasking || v_BlendRange != vec2(0.0))
        result.a *= v_Colour.a > 0.0 ? getRoundedColor(vec4(1.0), texCoord).a / v_Colour.a : 0.0;

    return result;
}

#endif
