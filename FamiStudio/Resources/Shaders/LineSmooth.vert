ATTRIB_IN vec2  inPosition;
ATTRIB_IN vec4  inColor;
ATTRIB_IN float inLineDist;
ATTRIB_IN float inDepth;

uniform vec4 screenScaleBias;
uniform vec2 windowSize;

INTERP_OUT vec4 colorInterp; 
INTERP_OUT float lineDistInterp; 

void main()
{
    gl_Position = vec4(inPosition * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    
    colorInterp = inColor;
    lineDistInterp = inLineDist * 127.5;
}
