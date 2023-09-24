ATTRIB_IN vec2  inPosition;
ATTRIB_IN vec4  inColor;
ATTRIB_IN float inDash;
ATTRIB_IN float inDepth;

uniform vec4  screenScaleBias;
uniform float dashScale;

INTERP_OUT vec4 colorInterp; 
#if FAMISTUDIO_ANDROID
INTERP_OUT float dashInterp;
#endif

void main()
{
    gl_Position = vec4(inPosition.xy * screenScaleBias.xy + screenScaleBias.zw, inDepth, 1);
    colorInterp = inColor;

#if FAMISTUDIO_ANDROID
    if (inDash != 0.0)
    {
        float dash = inDash * 0.25;
        float dashCoord = fract(dash) == 0.25 ? inPosition.y : inPosition.x;

        dashInterp = (dashCoord - floor(dash)) * dashScale;
    }
    else
    {
        dashInterp = 0.0;
    }
#endif
}
