layout(location = 0) out vec4 outColor;

noperspective in vec4 colorInterp;
noperspective in float lineDistInterp; 

void main()
{   
    float alpha = clamp(lineDistInterp, 0.0f, 1.0f);

    outColor = colorInterp;
    outColor.a *= alpha;
}
