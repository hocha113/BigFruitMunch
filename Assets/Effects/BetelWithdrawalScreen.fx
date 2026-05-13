//============================================================================
// BetelWithdrawalScreen.fx
//   屏幕级后处理 —— "槟榔戒断"渐进式视觉异常 + "刚嚼食"舒爽残留覆层
//
// 输入：
//   uImage0     当前场景 RT（由 SpriteBatch 自动绑定）
//   uTime       累计时间（秒）
//   uIntensity  戒断总强度 0..1
//   uFlash      刚嚼食的舒爽残留 0..1
//   uPulse      心跳脉冲 0..1（CPU 端已经做正弦驱动并传入）
//   uTint       戒断染色（暖红 -> 青灰渐变）
//   uVignette   暗角强度
//   uChromatic  色散偏移
//   uWaveAmp    采样扭曲振幅
//   uWaveFreq   扭曲频率
//   uNoise      噪点强度
//   uDesat      去饱和度
//   uTexSize    屏幕分辨率
//
// 设计要点：
//   * 所有效果均按区段渐进开启，强度 0 时几乎无操作（CPU 端再做短路）
//   * 戒断（intensity）与爽感（flash）独立计算后叠加，互不冲突
//   * 心跳脉冲只在中后期戒断介入，避免初期画面过于诡异
//============================================================================

sampler uImage0 : register(s0);

float  uTime;
float  uIntensity;
float  uFlash;
float  uPulse;
float3 uTint;
float  uVignette;
float  uChromatic;
float  uWaveAmp;
float  uWaveFreq;
float  uNoise;
float  uDesat;
float2 uTexSize;

float hash21(float2 p) {
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    // —— 屏幕坐标扭曲：横向纵向各一组 sin 波，模拟"晕眩" —— //
    float2 uv = coords;
    if (uWaveAmp > 0.0001) {
        uv.x += sin(coords.y * uWaveFreq + uTime * 3.0) * uWaveAmp;
        uv.y += sin(coords.x * (uWaveFreq * 0.7) + uTime * 2.3) * uWaveAmp * 0.8;
    }

    // —— 色散：水平方向分离 RGB —— //
    float ca = uChromatic * 0.004;
    float4 cR = tex2D(uImage0, uv + float2(ca, 0.0));
    float4 cG = tex2D(uImage0, uv);
    float4 cB = tex2D(uImage0, uv - float2(ca, 0.0));

    float3 rgb = float3(cR.r, cG.g, cB.b);
    float  a   = cG.a;

    // —— 去饱和 —— //
    float lum = dot(rgb, float3(0.299, 0.587, 0.114));
    rgb = lerp(rgb, lum.xxx, saturate(uDesat));

    // —— 戒断染色：颜色朝 uTint 偏移 —— //
    rgb = lerp(rgb, rgb * uTint, saturate(uIntensity));

    // —— 心跳脉冲（中后期戒断介入）—— //
    if (uIntensity > 0.45) {
        float pulseStrength = (uIntensity - 0.45) * 0.5 * uPulse;
        float3 hbColor = float3(0.9, 0.25, 0.25) * lum + rgb * 0.3;
        rgb = lerp(rgb, hbColor, pulseStrength);
    }

    // —— 噪点（重度戒断）—— //
    if (uNoise > 0.001) {
        float n = hash21(coords * uTexSize + uTime * 60.0);
        rgb += (n - 0.5) * uNoise;
    }

    // —— 暗角：屏幕中心向四周衰减 —— //
    float2 d = coords - 0.5;
    float r2 = dot(d, d);
    float vig = 1.0 - saturate(r2 * uVignette * 2.5);
    float vigMix = saturate(uIntensity * 0.8 + (uVignette - 0.2) * 0.2);
    rgb *= lerp(1.0, vig, vigMix);

    // —— 嚼食后"舒爽"正向覆层（与戒断同帧并存）—— //
    if (uFlash > 0.001) {
        float warmth = uFlash;
        float3 warmCol = float3(1.05, 0.95, 0.78);
        rgb = lerp(rgb, rgb * warmCol, warmth * 0.5);

        // 屏幕中央泛起一圈暖色提亮，模拟"嚼下去那一口"的爽
        float center = 1.0 - saturate(r2 * 1.8);
        rgb += float3(0.18, 0.12, 0.04) * warmth * center;
    }

    return float4(rgb, a);
}

technique Technique1
{
    pass BetelWithdrawalScreenPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
