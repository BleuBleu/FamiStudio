INTERP_IN vec4 colorInterp;
INTERP_IN float lineDistInterp; 

void main()
{   
    float alpha = clamp(lineDistInterp, 0.0, 1.0);

    FRAG_COLOR = colorInterp;
    FRAG_COLOR.a *= alpha;
}
