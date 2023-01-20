attribute vec2  inPosition;
attribute vec4  inColor;
attribute vec2  inTexCoords;
attribute float inDepth;

uniform vec4 screenScaleBias;

varying vec4 colorInterp; 
varying vec2 texCoordsInterp;

void main()
{
    // MATTT : Review this 0.5, move to scale/bias.
    gl_Position = vec4((inPosition.xy + 0.5f) * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorInterp = inColor;
    texCoordsInterp = inTexCoords;
}
