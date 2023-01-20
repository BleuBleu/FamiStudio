layout(location = 0) out vec4 outColor;

uniform sampler2D dashTexture;

noperspective in vec4 colorInterp;
noperspective in vec2 texCoordsInterp;

void main()
{       
    float dash = texture(dashTexture, texCoordsInterp).x;

    outColor = colorInterp;
    outColor.a *= dash;
}
