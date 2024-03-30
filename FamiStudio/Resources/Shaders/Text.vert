ATTRIB_IN vec2  inPosition;
ATTRIB_IN vec4  inColor;
ATTRIB_IN vec2  inTexCoords;
ATTRIB_IN float inDepth;

uniform vec4 screenScaleBias;

INTERP_OUT vec4 colorInterp; 
INTERP_OUT vec2 texCoordsInterp;

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorInterp = inColor;
    texCoordsInterp = inTexCoords;
}
