layout(location = 0) in vec2  inPosition;
layout(location = 1) in vec4  inColor;
layout(location = 2) in float inLineDist;
layout(location = 3) in float inDepth;

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
