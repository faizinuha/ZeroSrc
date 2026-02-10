// Simple Live2D Shader for DX11
cbuffer PerObject : register(b0)
{
    float4x4 ProjectionMatrix;
};

struct VS_INPUT
{
    float2 Pos : POSITION;
    float2 UV  : TEXCOORD0;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV  : TEXCOORD0;
};

Texture2D mainTexture : register(t0);
SamplerState mainSampler : register(s0);

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    output.Pos = mul(float4(input.Pos, 0.0f, 1.0f), ProjectionMatrix);
    output.UV = input.UV;
    return output;
}

float4 PS(PS_INPUT input) : SV_Target
{
    float4 color = mainTexture.Sample(mainSampler, input.UV);
    // Live2D textures are usually Premultiplied Alpha
    return color;
}
