// BigFruitQualityFilter.fx
// 大果物品的品质滤镜，背包和世界绘制共用。

sampler uImage0 : register(s0);

float  uTime;
float3 uTint;
float2 uTexSize;
float  uIntensity;
float  uDesaturate;
float  uOutlineStrength;
float  uShimmerStrength;
float  uPulseSpeed;
float  uChromatic;

float hash21(float2 p) {
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 texel = 1.0 / max(uTexSize, float2(1.0, 1.0));

    // 高品质时给一点水平色散。
    float ca = 1.4 * texel.x * uChromatic * uIntensity;
    float4 cR = tex2D(uImage0, coords + float2(ca, 0.0));
    float4 cG = tex2D(uImage0, coords);
    float4 cB = tex2D(uImage0, coords - float2(ca, 0.0));
    float a = cG.a;

    float3 rgb = float3(cR.r, cG.g, cB.b);

    // 干瘪品质会更灰。
    float lum = dot(rgb, float3(0.299, 0.587, 0.114));
    rgb = lerp(rgb, lum.xxx, saturate(uDesaturate * uIntensity));

    // 按品质色调偏移。
    rgb = rgb * lerp(float3(1, 1, 1), uTint, uIntensity);

    // 用 alpha 邻域差找边缘。
    float aL = tex2D(uImage0, coords - float2(texel.x, 0.0)).a;
    float aR = tex2D(uImage0, coords + float2(texel.x, 0.0)).a;
    float aU = tex2D(uImage0, coords - float2(0.0, texel.y)).a;
    float aD = tex2D(uImage0, coords + float2(0.0, texel.y)).a;
    float edge = saturate(4.0 * a - aL - aR - aU - aD);

    // 描边亮度随时间起伏。
    float pulse = 0.65 + 0.35 * sin(uTime * uPulseSpeed);
    rgb += uTint * edge * uOutlineStrength * pulse * uIntensity;

    // 斜向流光，只显示在贴图本体上。
    if (uShimmerStrength > 0.001) {
        float band = sin((coords.x + coords.y) * 6.2831 + uTime * 2.5);
        float shimmer = saturate(band - 0.7) * uShimmerStrength * uIntensity;
        rgb += uTint * shimmer * a * 1.4;
    }

    // 神话品质再加少量闪点。
    if (uChromatic > 0.5) {
        float twinkle = step(0.985, hash21(floor(coords * uTexSize) + floor(uTime * 6.0)));
        rgb += uTint * twinkle * a * 0.5 * uIntensity;
    }

    // 保留原 alpha，同时吃 Terraria 的顶点颜色。
    return float4(rgb, a) * vertexColor;
}

technique Technique1
{
    pass BigFruitQualityFilterPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
