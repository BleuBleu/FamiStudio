attribute vec2  inPosition;
attribute vec4  inColor;
attribute float inDash;
attribute float inDepth;

uniform vec4 screenScaleBias;
uniform float uniformDashScale;

noperspective out vec4 colorInterp; 
#if 0 // We dont use thick dashed line on desktop.
noperspective out float dashInterp;
#endif

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorInterp = inColor;

#if 0 // We dont use thick dashed line on desktop.
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
#endif
}
