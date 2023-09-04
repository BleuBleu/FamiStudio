attribute vec2  inPosition;
attribute vec4  inColor;
attribute float inDash;
attribute float inDepth;

uniform vec4 screenScaleBias;
uniform float uniformDashScale;

noperspective out vec4 colorInterp; 
noperspective out float dashInterp;

void main()
{
    gl_Position = vec4((inPosition.xy + 0.5f) * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorInterp = inColor;

    if (inDash != 0.0)
    {
        float dash = inDash * 0.25f;
        float dashCoord = fract(dash) == 0.25f ? inPosition.y : inPosition.x;

        dashInterp = (dashCoord - floor(dash)) * uniformDashScale;
    }
    else
    {
        dashInterp = 0.0f;
    }
}
