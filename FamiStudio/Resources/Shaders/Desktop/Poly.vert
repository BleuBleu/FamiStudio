layout(location = 0) in vec2  inPosition;
layout(location = 1) in vec4  inColor;
layout(location = 2) in float inDepth;

uniform vec4 screenScaleBias;

noperspective out vec4 colorVertToFrag; 

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorVertToFrag = inColor;
}
