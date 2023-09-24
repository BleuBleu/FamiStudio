INTERP_IN vec4 colorInterp;
INTERP_IN float lineDistInterp; 

void main()
{   
    float alpha = clamp(lineDistInterp, 0.0, 1.0);

    gl_FragColor = colorInterp;
    gl_FragColor.a *= alpha;
}
