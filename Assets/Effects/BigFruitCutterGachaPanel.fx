// BigFruitCutterGachaPanel.fx
// 榨汁机抽取界面的背景效果。
// 这里用 ps_3_0，给旋涡和后续 UI 效果留足指令空间。

sampler uImage0 : register(s0);

float uTime;
float uUpgraded;
float uRolling;
float2 uTexSize;

float hash21(float2 p) {
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p + 19.19);
    return frac(p.x * p.y);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 uv = coords;
    float2 p = uv * 2.0 - 1.0;
    p.x *= uTexSize.x / max(1.0, uTexSize.y);

    float r = length(p);
    float a = atan2(p.y, p.x);
    float spin = a + r * (5.0 + uRolling * 7.0) - uTime * (0.8 + uRolling * 2.4);
    float pulp = 0.5 + 0.5 * sin(spin * 4.0 + sin(r * 14.0 - uTime * 2.0));
    float vein = smoothstep(0.72, 1.0, pulp) * (1.0 - smoothstep(0.75, 1.2, r));
    float ring = 0.5 + 0.5 * sin(r * 30.0 - uTime * (3.0 + uRolling * 4.0));
    float glow = smoothstep(0.82, 1.0, ring) * (1.0 - smoothstep(0.15, 1.0, r));

    float3 baseA = lerp(float3(0.20, 0.10, 0.07), float3(0.12, 0.07, 0.22), uUpgraded);
    float3 baseB = lerp(float3(0.80, 0.32, 0.13), float3(1.00, 0.55, 0.18), uUpgraded);
    float3 rgb = lerp(baseA, baseB, vein * 0.55 + glow * 0.25);

    float chroma = (0.002 + uRolling * 0.004) * (0.3 + glow);
    float nR = hash21(uv * 80.0 + uTime);
    float nB = hash21(uv * 90.0 - uTime);
    rgb.r += chroma * 16.0 * nR;
    rgb.b += chroma * 10.0 * nB * uUpgraded;

    float vignette = smoothstep(1.25, 0.15, r);
    rgb *= 0.40 + 0.85 * vignette;
    rgb += vertexColor.rgb * 0.08;

    return float4(rgb, vertexColor.a);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
