INTERP_IN vec4 colorInterp;
INTERP_IN float dashInterp;

void main()
{       
    float dashAlpha = mod(dashInterp, 2.0) < 1.0 ? 1.0 : 0.0;

    gl_FragColor = colorInterp;
    gl_FragColor.a *= dashAlpha;
}
