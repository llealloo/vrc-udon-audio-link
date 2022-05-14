Shader "Unlit/ScreenColorDebug"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "/Assets/AudioLink/Shaders/AudioLink.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 grid_dimensions = float2(4,4);
                float2 uv = i.uv * grid_dimensions;
                uint2 grid_index = floor(uv);
                grid_index.y = (grid_dimensions.y-1) - grid_index.y;
                // Note: The following is a bit of a abuse of undocumented relationships between
                // ALPASS_THEME_COLOR0
                // ALPASS_THEME_COLOR1
                // ALPASS_THEME_COLOR2
                // ALPASS_THEME_COLOR3
                return AudioLinkData(ALPASS_THEME_COLOR0 + uint2(grid_index.x, 0));
            }
            ENDCG
        }
    }
}
