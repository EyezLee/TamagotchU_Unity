// 7/24/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

Shader "Custom/DebugVertexColor"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // Vertex color input
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR; // Pass vertex color to fragment
            };

            float4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color; // Pass vertex color to fragment
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Multiply vertex color with a base color for debugging
                return i.color * _BaseColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}