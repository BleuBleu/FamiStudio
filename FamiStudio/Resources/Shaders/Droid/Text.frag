uniform sampler2D tex;

varying vec4 colorInterp;
varying vec2 texCoordsInterp;

void main()
{   
    gl_FragColor.rgb = colorInterp.rgb;
    gl_FragColor.a = colorInterp.a * texture2D(tex, texCoordsInterp).r;
}
