// BigFruitHighScreen.fx
// 嚼大果时的正向屏幕后处理，和戒断滤镜做出明显反差。

sampler uImage0 : register(s0);

float  uTime;
float  uIntensity;
float  uPulse;
float  uFlash;
float2 uTexSize;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 center = float2(0.5, 0.5);
    float2 d = coords - center;
    float r = length(d);

    // 中心轻微鼓起。
    float distortAmt = uIntensity * 0.12 + uFlash * 0.10;
    float2 uv = coords - d * r * distortAmt;

    // 强度越高，色散越明显。
    float ca = uIntensity * 0.003 + uFlash * 0.004;
    float4 cR = tex2D(uImage0, uv + float2(ca, 0.0));
    float4 cG = tex2D(uImage0, uv);
    float4 cB = tex2D(uImage0, uv - float2(ca, 0.0));

    float3 rgb = float3(cR.r, cG.g, cB.b);
    float  a   = cG.a;

    // 上头时颜色更满。
    float lum = dot(rgb, float3(0.299, 0.587, 0.114));
    float satBoost = 1.0 + uIntensity * 0.6 + uFlash * 0.3;
    rgb = lerp(lum.xxx, rgb, satBoost);

    // 嚼劲带一点暖色呼吸。
    float pulse = uPulse * uIntensity;
    rgb += float3(0.06, 0.04, 0.0) * pulse;

    // 边缘暖光晕。
    float edge = saturate((r - 0.25) * 1.6);
    float3 warm = float3(1.0, 0.75, 0.45);
    rgb = lerp(rgb, rgb * warm + warm * 0.08, edge * (uIntensity * 0.5 + uFlash * 0.35));

    // 刚嚼下去的中央提亮。
    if (uFlash > 0.001) {
        float center2 = 1.0 - saturate(r * 1.8);
        rgb += float3(0.20, 0.14, 0.05) * uFlash * center2;
    }

    return float4(saturate(rgb), a);
}

technique Technique1
{
    pass HighScreenPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
