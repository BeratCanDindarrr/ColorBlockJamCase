// Inverted hull: the mesh grown along its normals (world units) with front faces culled, so only a rim shows round
// the real mesh drawn over it. Used for the held block's outline.
Shader "ColorBlockJam/Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Width ("Width (world units)", Float) = 0.04
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Width;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            float4 vert (appdata v) : SV_POSITION
            {
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                world += normalize(UnityObjectToWorldNormal(v.normal)) * _Width;
                return mul(UNITY_MATRIX_VP, float4(world, 1));
            }

            fixed4 frag () : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
