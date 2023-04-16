layout(location = 0) out vec4 outColor;

uniform sampler2D tex;

noperspective in vec4 colorInterp;
noperspective in vec2 texCoordsInterp;

void main()
{   
    outColor.rgb = colorInterp.rgb;
    outColor.a = colorInterp.a * texture(tex, texCoordsInterp).r;
}
