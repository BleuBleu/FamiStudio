INTERP_IN vec4 colorInterp;
#if FAMISTUDIO_ANDROID
INTERP_IN float dashInterp;
#endif

void main()
{   
#if FAMISTUDIO_ANDROID
    float dashAlpha = mod(dashInterp, 2.0) < 1.0 ? 1.0 : 0.0;
#endif
    gl_FragColor = colorInterp;
#if FAMISTUDIO_ANDROID
    gl_FragColor.a *= dashAlpha;
#endif
}
