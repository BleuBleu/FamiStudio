layout(location = 0) out vec4 outColor;

noperspective in vec4 colorInterp;
#if 0 // We dont use thick dashed line on desktop.
noperspective in float dashInterp;
#endif

void main()
{   
#if 0 // We dont use thick dashed line on desktop.
    float dashAlpha = mod(dashInterp, 2.0f) < 1.0f ? 1.0f : 0.0f;
#endif
    outColor = colorInterp;
#if 0 // We dont use thick dashed line on desktop.
    outColor.a *= dashAlpha;
#endif
}
