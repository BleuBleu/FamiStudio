attribute vec2  inPosition;
attribute vec4  inColor;
attribute float inLineDist;
attribute float inDepth;

uniform vec4 screenScaleBias;
uniform vec2 windowSize;

noperspective out vec4 colorInterp; 
noperspective out float lineDistInterp; 

void main()
{
    gl_Position = vec4(inPosition * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    
    colorInterp = inColor;
    lineDistInterp = inLineDist * 127.5f;
}
