Shader "Custom/UI_Spotlight"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _CursorPosition ("Cursor Position", Vector) = (0.5,0.5,0,0)
        _SpotlightRadius ("Spotlight Radius", Float) = 0.2
        _EdgeSoftness ("Edge Softness", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _CursorPosition;
            float _SpotlightRadius;
            float _EdgeSoftness;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // distance from cursor in UV space
                float dist = distance(i.uv, _CursorPosition.xy);
                
                // alpha based on distance, smooth edges
                float alpha = 1.0 - smoothstep(_SpotlightRadius, _SpotlightRadius - _EdgeSoftness, dist);

                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}