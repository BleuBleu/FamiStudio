layout(location = 0) out vec4 outColor;

uniform sampler2D tex;

noperspective in vec4 colorInterp;
noperspective in vec2 texCoordsInterp;

void main()
{   
    outColor = texture(tex, texCoordsInterp) * colorInterp;
}
