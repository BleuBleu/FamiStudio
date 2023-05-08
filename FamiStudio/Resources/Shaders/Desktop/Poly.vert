layout(location = 0) in vec2  inPosition;
layout(location = 1) in vec4  inColor;
layout(location = 2) in float inDash;
layout(location = 3) in float inDepth;

uniform vec4 screenScaleBias;
uniform float uniformDashScale;

noperspective out vec4 colorInterp; 
noperspective out float dashInterp;

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
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
