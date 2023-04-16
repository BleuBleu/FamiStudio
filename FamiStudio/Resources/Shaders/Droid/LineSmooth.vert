attribute vec2  inPosition;
attribute vec4  inColor;
attribute float inLineDist;
attribute float inDepth;

uniform vec4 screenScaleBias;
uniform vec2 windowSize;

varying vec4 colorInterp; 
varying vec2 centerPosInterp; 
varying float lineDistInterp; 

void main()
{
    gl_Position = vec4(inPosition * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    
    colorInterp = inColor;
    lineDistInterp = inLineDist * 127.5;
}
