layout(location = 0) out vec4 outColor;

noperspective in vec4 colorVertToFrag;

void main()
{   
    outColor = colorVertToFrag;
}
