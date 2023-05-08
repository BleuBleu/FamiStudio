layout(location = 0) out vec4 outColor;

noperspective in vec4 colorInterp;
noperspective in float dashInterp;

void main()
{   
    float dashAlpha = mod(dashInterp, 2.0f) < 1.0f ? 1.0f : 0.0f;

    outColor = colorInterp;
#if 0 // We dont use thick dashed line on desktop.
    outColor.a *= dashAlpha;
#endif
}
