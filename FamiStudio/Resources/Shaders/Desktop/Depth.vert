layout(location = 0) in vec2  inPosition;
layout(location = 1) in float inDepth;

uniform vec4 screenScaleBias;

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
}
