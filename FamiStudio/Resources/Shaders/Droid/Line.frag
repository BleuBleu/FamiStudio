uniform sampler2D dashTexture;

varying vec4 colorInterp;
varying vec2 texCoordsInterp;

void main()
{       
    float dash = texture2D(dashTexture, texCoordsInterp).x;

    gl_FragColor = colorInterp;
    gl_FragColor.a *= dash;
}
