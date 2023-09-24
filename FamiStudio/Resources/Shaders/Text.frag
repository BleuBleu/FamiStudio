uniform sampler2D tex;

INTERP_IN vec4 colorInterp;
INTERP_IN vec2 texCoordsInterp;

void main()
{   
    gl_FragColor.rgb = colorInterp.rgb;
    gl_FragColor.a = colorInterp.a * TEX(tex, texCoordsInterp).r;
}
