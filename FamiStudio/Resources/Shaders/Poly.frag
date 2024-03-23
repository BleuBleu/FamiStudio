INTERP_IN vec4 colorInterp;
#if FAMISTUDIO_ANDROID
INTERP_IN float dashInterp;
#endif

void main()
{   
#if FAMISTUDIO_ANDROID
    float dashAlpha = mod(dashInterp, 2.0) < 1.0 ? 1.0 : 0.0;
#endif
    FRAG_COLOR = colorInterp;
#if FAMISTUDIO_ANDROID
    FRAG_COLOR.a *= dashAlpha;
#endif
}
