uniform sampler2D tex;

INTERP_IN vec4 colorInterp;
INTERP_IN vec2 texCoordsInterp;

void main()
{   
    FRAG_COLOR.rgb = colorInterp.rgb;
    FRAG_COLOR.a = colorInterp.a * TEX(tex, texCoordsInterp).r;
}
