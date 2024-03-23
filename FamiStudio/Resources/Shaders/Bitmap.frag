uniform sampler2D tex;

INTERP_IN             vec4 colorInterp;
INTERP_PERSPECTIVE_IN vec3 texCoordsInterp;

void main()
{   
    FRAG_COLOR = TEXPROJ(tex, vec4(texCoordsInterp.xy, 0, texCoordsInterp.z)) * colorInterp;
}
