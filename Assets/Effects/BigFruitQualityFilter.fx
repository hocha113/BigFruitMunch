//============================================================================
// BigFruitQualityFilter.fx —— 大果系列物品的品质滤镜（Inventory/World 通用）
//
// 一份 shader 通过若干参数表现 7 种品质：
//   uTint           品质主色（与原图相乘做色相）
//   uTexSize        图像像素尺寸（用于邻域采样换算）
//   uTime           累积时间（秒）
//   uIntensity      整体滤镜强度 0~1
//   uDesaturate     去饱和度 0~1（1=完全灰）  —— 干瘪用
//   uOutlineStrength 描边发光强度 0~1.5      —— 普通=0，越好越亮
//   uShimmerStrength 流光带强度 0~1          —— 史诗以上才开
//   uPulseSpeed     脉冲速度（rad/s）
//   uChromatic      色散偏移 0~1             —— 神话满
//
// 该 shader 不要求精确像素尺寸，对任意 alpha-mask 贴图都能稳定工作。
//============================================================================

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

    // —— 色散：分离 R/G/B 三通道做小幅水平偏移 —— //
    float ca = 1.4 * texel.x * uChromatic * uIntensity;
    float4 cR = tex2D(uImage0, coords + float2(ca, 0.0));
    float4 cG = tex2D(uImage0, coords);
    float4 cB = tex2D(uImage0, coords - float2(ca, 0.0));
    float a = cG.a;

    float3 rgb = float3(cR.r, cG.g, cB.b);

    // —— 去饱和：用感知亮度 lerp 回原色 —— //
    float lum = dot(rgb, float3(0.299, 0.587, 0.114));
    rgb = lerp(rgb, lum.xxx, saturate(uDesaturate * uIntensity));

    // —— 色相调整：朝品质主色偏移（保留亮度细节）—— //
    rgb = rgb * lerp(float3(1, 1, 1), uTint, uIntensity);

    // —— 描边发光：用 alpha 邻域差检测边缘 —— //
    float aL = tex2D(uImage0, coords - float2(texel.x, 0.0)).a;
    float aR = tex2D(uImage0, coords + float2(texel.x, 0.0)).a;
    float aU = tex2D(uImage0, coords - float2(0.0, texel.y)).a;
    float aD = tex2D(uImage0, coords + float2(0.0, texel.y)).a;
    float edge = saturate(4.0 * a - aL - aR - aU - aD);

    // 脉冲呼吸（描边亮度随时间起伏）
    float pulse = 0.65 + 0.35 * sin(uTime * uPulseSpeed);
    rgb += uTint * edge * uOutlineStrength * pulse * uIntensity;

    // —— 流光带：斜向移动的明亮条纹（仅在贴图实体范围内显示）—— //
    if (uShimmerStrength > 0.001) {
        float band = sin((coords.x + coords.y) * 6.2831 + uTime * 2.5);
        float shimmer = saturate(band - 0.7) * uShimmerStrength * uIntensity;
        rgb += uTint * shimmer * a * 1.4;
    }

    // —— 神话级随机闪点：极其稀疏的高亮像素 —— //
    if (uChromatic > 0.5) {
        float twinkle = step(0.985, hash21(floor(coords * uTexSize) + floor(uTime * 6.0)));
        rgb += uTint * twinkle * a * 0.5 * uIntensity;
    }

    // 输出：保留原始 alpha，乘以顶点颜色（兼容 Terraria 染色与 mask）
    return float4(rgb, a) * vertexColor;
}

technique Technique1
{
    pass BigFruitQualityFilterPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
