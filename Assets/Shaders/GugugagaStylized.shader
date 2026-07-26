Shader "PenguinRun/Gugugaga Stylized"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _NormalMap ("Normal Detail", 2D) = "bump" {}
        _MaskMap ("Metallic Roughness Mask", 2D) = "white" {}
        _RimColor ("Rim Color", Color) = (0.18,0.65,0.9,1)
        _RimPower ("Rim Power", Range(1,8)) = 3.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                float4 color : COLOR;
                float2 uv : TEXCOORD2;
            };

            fixed4 _Tint;
            fixed4 _RimColor;
            float _RimPower;
            sampler2D _MaskMap;

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.color = input.color * _Tint;
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 normal = normalize(input.worldNormal);
                float3 viewDirection = normalize(UnityWorldSpaceViewDir(input.worldPosition));
                // Key light is fixed in VIEW space, not world space, so whichever side of
                // the avatar the camera is on is the side that gets lit.
                //
                // This used to be a world-space constant of (0.35, 0.8, 0.45). Its +Z faces
                // down the track, and during a run the player only ever sees the avatar's
                // back (normals near -Z), so every camera-facing pixel clamped to the 0.38
                // wrap floor: the scarf, goggles and flippers collapsed into one dark mass
                // and only the orange feet stayed legible. The lobby had the mirror problem,
                // since the mascot faces -Z there. No single world direction lights both.
                //
                // mul(v, M) is a row-vector product, i.e. transpose(V) * v, and for the
                // orthonormal view rotation transpose(V) is view->world. +Z in view space
                // points back at the camera, so this reads as an over-the-shoulder key.
                float3 lightDirection = normalize(mul(float3(0.35, 0.55, 0.75), (float3x3)UNITY_MATRIX_V));
                float diffuse = saturate(dot(normal, lightDirection));
                float wrappedDiffuse = diffuse * 0.62 + 0.38;
                float rim = pow(1.0 - saturate(dot(normal, viewDirection)), _RimPower);
                float mask = tex2D(_MaskMap, input.uv).g;
                float specular = pow(saturate(dot(reflect(-lightDirection, normal), viewDirection)), 24.0) * mask * 0.22;
                float3 color = input.color.rgb * wrappedDiffuse;
                color += _RimColor.rgb * rim * 0.18 + specular;
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
