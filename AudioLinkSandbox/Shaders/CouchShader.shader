Shader "cnlohr/CouchShader"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_TextureDetail ("Detail", Range(0,10))=2
		_TextureAnimation ("Animation Speed", Range(0,2))=.5
		_TANoiseTex ("TANoise", 2D) = "white" {}
		_NoisePow ("Noise Power", float ) = 1.8
		_RockAmbient ("Rock Ambient Boost", float ) = 0.1
		_EmissionMux( "Emission Mux", Color) = (.3, .3, .3, 1. )
		_InstanceID ("Instance ID", Vector ) = ( 0, 0, 0 ,0 )
	}
	SubShader
	{
		// shadow caster rendering pass, implemented manually
		// using macros from UnityCG.cginc
		Pass
		{
			Tags {"LightMode"="ShadowCaster"}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"
			#pragma multi_compile_instancing

			struct v2f { 
				V2F_SHADOW_CASTER;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}


		Tags { "RenderType"="Opaque" }
		LOD 200
		//"DisableBatching"="False"

		Cull Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		#pragma multi_compile_instancing
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 4.0
		#include "/Assets/AudioLinkSandbox/Shaders/tanoise/tanoise.cginc"
		#include "/Assets/AudioLink/Shaders/AudioLink.cginc"

		sampler2D _MainTex;
		
		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 viewDir;
			float3 objPos;
			float4 color;
			float4 extra;
		};

		struct appdata
		{
			float4 vertex    : POSITION;  // The vertex position in model space.
			float3 normal    : NORMAL;    // The vertex normal in model space.
			float4 texcoord  : TEXCOORD0; // The first UV coordinate.
			float4 texcoord1 : TEXCOORD1; // The second UV coordinate.
			float4 texcoord2 : TEXCOORD2; // The second UV coordinate.
			float4 tangent   : TANGENT;   // The tangent vector in Model Space (used for normal mapping).
			float4 color     : COLOR;     // Per-vertex color
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		half _Glossiness;
		half _TextureDetail;
		half _Metallic;
		half _TextureAnimation;
		half _NoisePow, _RockAmbient;
		half4 _EmissionMux;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP( float4, _InstanceID)
		UNITY_INSTANCING_BUFFER_END(Props)
		
		float densityat( float3 calcpos, float time )
		{
			float tim = time*_TextureAnimation;
		   // calcpos.y += tim * _TextureAnimation;
			float4 col =
				tanoise4_1d( float4( float3( calcpos*10. ), tim ) ) * 0.5 +
				tanoise4_1d( float4( float3( calcpos.xyz*30.1 ), tim ) ) * 0.3 +
				tanoise4_1d( float4( float3( calcpos.xyz*90.2 ), tim ) ) * 0.2 +
				tanoise4_1d( float4( float3( calcpos.xyz*320.5 ), tim ) ) * 0.1 +
				tanoise4_1d( float4( float3( calcpos.xyz*641. ), tim ) ) * .08 +
				tanoise4_1d( float4( float3( calcpos.xyz*1282. ), tim ) ) * .05;
			return col;
		}


        void vert (inout appdata v, out Input o ) {
            UNITY_INITIALIZE_OUTPUT(Input,o);
			UNITY_SETUP_INSTANCE_ID(v);

			float3 worldScale = float3(
				length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)), // scale x axis
				length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)), // scale y axis
				length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z))  // scale z axis
				);
            o.objPos = v.vertex*worldScale;
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
			int instanceid = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceID).x;
			o.extra = float4( unity_InstanceID, instanceid, 0, 1 );
#else
			o.extra = float4( 0, 0, 0, 1 );
#endif

        }
 
		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			float time = (
				AudioLinkDecodeDataAsSeconds( ALPASS_CHRONOTENSITY + uint2(2, 0) ) +
				AudioLinkDecodeDataAsSeconds( ALPASS_CHRONOTENSITY + uint2(2, 1) ) +
				AudioLinkDecodeDataAsSeconds( ALPASS_CHRONOTENSITY + uint2(2, 2) ) +
				AudioLinkDecodeDataAsSeconds( ALPASS_CHRONOTENSITY + uint2(2, 3) ) ) * .002;
		
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			float3 calcpos = IN.objPos.xyz * _TextureDetail;
			
			//Pretend different instances are different places in space.
			calcpos.y += IN.extra.y*10;
			float4 col = densityat( calcpos, time );
			c *= pow( col.xxxx, _NoisePow) + _RockAmbient;
			
			float4 normpert = tanoise4( float4( calcpos.xyz*320.5, time*_TextureAnimation ) ) * .4 +
				tanoise4( float4( calcpos.xyz*90.2, time*_TextureAnimation ) ) * .3;


            if( IN.viewDir.z < 0 )
			{
				c /= 5;
			}
			 
			o.Normal = normalize( float3( normpert.xy-.35, 1.5 ) );

			//c = frac( IN.extra.y*.25+.05 ).xxxx;

			o.Albedo = c.rgb * 1.2;
			o.Emission = c * _EmissionMux;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;// * clamp( col.z*10.-7., 0, 1 );
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}