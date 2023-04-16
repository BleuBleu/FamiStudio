attribute vec2  inPosition;
attribute vec4  inColor;
attribute float inDepth;

uniform vec4 screenScaleBias;

varying vec4 colorVertToFrag; 

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorVertToFrag = inColor;
}
