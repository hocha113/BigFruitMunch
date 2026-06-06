// BetelWithdrawalScreen.fx
// 戒断用的屏幕后处理。uIntensity 控制负面效果，uFlash 叠加刚嚼下去的暖色残留。

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
    // 轻微晕眩扭曲。
    float2 uv = coords;
    if (uWaveAmp > 0.0001) {
        uv.x += sin(coords.y * uWaveFreq + uTime * 3.0) * uWaveAmp;
        uv.y += sin(coords.x * (uWaveFreq * 0.7) + uTime * 2.3) * uWaveAmp * 0.8;
    }

    // 横向色散。
    float ca = uChromatic * 0.004;
    float4 cR = tex2D(uImage0, uv + float2(ca, 0.0));
    float4 cG = tex2D(uImage0, uv);
    float4 cB = tex2D(uImage0, uv - float2(ca, 0.0));

    float3 rgb = float3(cR.r, cG.g, cB.b);
    float  a   = cG.a;

    // 戒断会把颜色压灰。
    float lum = dot(rgb, float3(0.299, 0.587, 0.114));
    rgb = lerp(rgb, lum.xxx, saturate(uDesat));

    // 叠一层戒断色调。
    rgb = lerp(rgb, rgb * uTint, saturate(uIntensity));

    // 中后期再加入心跳感。
    if (uIntensity > 0.45) {
        float pulseStrength = (uIntensity - 0.45) * 0.5 * uPulse;
        float3 hbColor = float3(0.9, 0.25, 0.25) * lum + rgb * 0.3;
        rgb = lerp(rgb, hbColor, pulseStrength);
    }

    // 重度戒断噪点。
    if (uNoise > 0.001) {
        float n = hash21(coords * uTexSize + uTime * 60.0);
        rgb += (n - 0.5) * uNoise;
    }

    // 重度戒断的错位重影。
    if (uIntensity > 0.7) {
        float ghost = (uIntensity - 0.7) * 1.6;
        float2 ghostOff = float2(sin(uTime * 1.3) * 0.012, cos(uTime * 1.1) * 0.009) * ghost;
        float3 g = tex2D(uImage0, uv + ghostOff).rgb;
        rgb = lerp(rgb, max(rgb, g), saturate(ghost) * 0.45);
    }

    // 暗角。
    float2 d = coords - 0.5;
    float r2 = dot(d, d);
    float vig = 1.0 - saturate(r2 * uVignette * 2.5);
    float vigMix = saturate(uIntensity * 0.8 + (uVignette - 0.2) * 0.2);
    rgb *= lerp(1.0, vig, vigMix);

    // 刚嚼下去时保留一层暖色反馈。
    if (uFlash > 0.001) {
        float warmth = uFlash;
        float3 warmCol = float3(1.05, 0.95, 0.78);
        rgb = lerp(rgb, rgb * warmCol, warmth * 0.5);

        // 中央提亮一点，避免反馈太平。
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
