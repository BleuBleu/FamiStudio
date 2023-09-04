uniform sampler2D tex;

noperspective in vec4 colorInterp;
noperspective in vec2 texCoordsInterp;

void main()
{   
    gl_FragColor = texture(tex, texCoordsInterp) * colorInterp;
}
