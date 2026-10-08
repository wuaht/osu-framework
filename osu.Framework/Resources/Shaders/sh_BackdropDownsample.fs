#ifndef BACKDROP_DOWNSAMPLE_FS
#define BACKDROP_DOWNSAMPLE_FS

#include "sh_Utils.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_DownsampleParameters
{
	highp vec2 g_TexelSize;
	int g_TapsX;
	int g_TapsY;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

// Averages the block of source pixels covered by the destination pixel (box filter).
// Each bilinear tap is placed between four source pixels and thereby averages all of them, halving the required amount of taps per axis.
void main(void)
{
	highp vec2 tapCount = vec2(float(g_TapsX), float(g_TapsY));
	mediump vec4 sum = vec4(0.0);

	for (int x = 0; x < 8; x++)
	{
		if (x >= g_TapsX)
			break;

		for (int y = 0; y < 8; y++)
		{
			if (y >= g_TapsY)
				break;

			highp vec2 offset = (vec2(float(x), float(y)) - (tapCount - 1.0) * 0.5) * 2.0 * g_TexelSize;
			sum += texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + offset);
		}
	}

	o_Colour = sum / (tapCount.x * tapCount.y);
}

#endif
