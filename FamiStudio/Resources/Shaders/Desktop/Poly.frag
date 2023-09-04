noperspective in vec4 colorInterp;
#if 0 // We dont use thick dashed line on desktop.
noperspective in float dashInterp;
#endif

void main()
{   
#if 0 // We dont use thick dashed line on desktop.
    float dashAlpha = mod(dashInterp, 2.0f) < 1.0f ? 1.0f : 0.0f;
#endif
    gl_FragColor = colorInterp;
#if 0 // We dont use thick dashed line on desktop.
    gl_FragColor.a *= dashAlpha;
#endif
}
