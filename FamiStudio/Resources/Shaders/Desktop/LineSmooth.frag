noperspective in vec4 colorInterp;
noperspective in float lineDistInterp; 

void main()
{   
    float alpha = clamp(lineDistInterp, 0.0f, 1.0f);

    gl_FragColor = colorInterp;
    gl_FragColor.a *= alpha;
}
