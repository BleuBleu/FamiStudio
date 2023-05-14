uniform float uniformDashScale;

varying vec4 colorInterp;
varying float dashInterp;

void main()
{   
    float dashAlpha = mod(dashInterp, 2.0) < 1.0 ? 1.0 : 0.0;

    gl_FragColor = colorInterp;
    gl_FragColor.a *= dashAlpha;
}
