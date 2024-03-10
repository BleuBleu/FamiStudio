uniform sampler2D tex;
uniform vec4 blurKernel[30];
uniform vec2 blurCocBiasScale;

INTERP_IN vec2 texCoordsInterp;

void main()
{   
    vec4 blur = vec4(0, 0, 0, 0);
    float scale = clamp((texCoordsInterp.y + blurCocBiasScale.x) * blurCocBiasScale.y, 0.0, 1.0);

    vec3 t;
    float w;

    // Center
    {
        t = TEX(tex, texCoordsInterp).rgb;
        w = dot(t, vec3(0.3, 0.59, 0.11)) > 0.35 ? 1.0 : 0.1; // Fake "HDR" to get nice bokeh
        blur.rgb += t * w;
        blur.a += w;
    }

    for (int i = 0; i < 30; i++)
    {
        t = TEX(tex, texCoordsInterp + blurKernel[i].xy * scale).rgb;
        w = dot(t, vec3(0.3, 0.59, 0.11)) > 0.35 ? 1.0 : 0.1; // Fake "HDR" to get nice bokeh
        blur.rgb += t * w;
        blur.a += w;

        t = TEX(tex, texCoordsInterp + blurKernel[i].zw * scale).rgb;
        w = dot(t, vec3(0.3, 0.59, 0.11)) > 0.35 ? 1.0 : 0.1; // Fake "HDR" to get nice bokeh
        blur.rgb += t * w;
        blur.a += w;
    }
    
    FRAG_COLOR.rgb = blur.rgb / blur.a;
    FRAG_COLOR.a = 1.0;
}
