ATTRIB_IN vec2 inPosition;
ATTRIB_IN vec2 inTexCoords;

uniform vec4 screenScaleBias;

INTERP_OUT vec4 colorInterp; 
INTERP_OUT vec2 texCoordsInterp;

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, 0, 1);
    texCoordsInterp = inTexCoords;
}
