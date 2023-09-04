uniform sampler2D tex;

noperspective in vec4 colorInterp;
noperspective in vec2 texCoordsInterp;

void main()
{   
    gl_FragColor.rgb = colorInterp.rgb;
    gl_FragColor.a = colorInterp.a * texture(tex, texCoordsInterp).r;
}
