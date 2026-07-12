Shader "PenguinRun/GradientSkybox"
{
    Properties
    {
        _HorizonColor ("Horizon Color", Color) = (0.18, 0.42, 0.68, 1)
        _ZenithColor ("Zenith Color", Color) = (0.015, 0.05, 0.16, 1)
        _StarIntensity ("Star Intensity", Range(0, 1)) = 0.35
        _AuroraColor ("Aurora Color", Color) = (0.2, 0.95, 0.8, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _HorizonColor;
            fixed4 _ZenithColor;
            fixed4 _AuroraColor;
            float _StarIntensity;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.direction = v.vertex.xyz; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.direction);
                float vertical = saturate(dir.y * 0.5 + 0.5);
                fixed3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, smoothstep(0.1, 0.9, vertical));
                float starNoise = frac(sin(dot(floor(dir.xz * 520.0), float2(12.9898, 78.233))) * 43758.5453);
                float stars = step(0.9975, starNoise) * _StarIntensity * saturate(vertical * 1.5 - 0.35);
                float aurora = sin(dir.x * 8.0 + dir.z * 3.0) * 0.5 + 0.5;
                aurora *= smoothstep(0.25, 0.75, vertical) * smoothstep(0.05, 0.55, 1.0 - vertical) * 0.1;
                return fixed4(color + stars + _AuroraColor.rgb * aurora, 1);
            }
            ENDCG
        }
    }
}
