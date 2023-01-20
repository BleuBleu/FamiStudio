varying vec4 colorInterp;
varying float lineDistInterp; 

void main()
{   
    float alpha = clamp(lineDistInterp, 0.0f, 1.0f);

    gl_FragColor = colorInterp;
    gl_FragColor.a *= alpha;
}
