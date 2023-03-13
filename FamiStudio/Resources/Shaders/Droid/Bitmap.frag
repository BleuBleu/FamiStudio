uniform sampler2D tex;

varying vec4 colorInterp;
varying vec2 texCoordsInterp;

void main()
{   
    gl_FragColor = texture2D(tex, texCoordsInterp) * colorInterp;
}
