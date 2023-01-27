layout(location = 0) in vec2  inPosition;
layout(location = 1) in vec4  inColor;
layout(location = 2) in vec2  inTexCoords;
layout(location = 3) in float inDepth;

uniform vec4 screenScaleBias;

noperspective out vec4 colorInterp; 
noperspective out vec2 texCoordsInterp;

void main()
{
    gl_Position = vec4((inPosition.xy + 0.5f) * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorInterp = inColor;
    texCoordsInterp = inTexCoords;
}
