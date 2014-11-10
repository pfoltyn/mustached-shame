Shader "Particles/ColorTint" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_FieldNumber ("Field Number", Float) = 0.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	AlphaTest Greater .01
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}

	// ---- Fragment program cards
	SubShader {
		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_particles

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _TintColor;
			float _FieldNumber;

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : COLOR
			{	
				float4 baseColor = tex2D(_MainTex, i.texcoord);
				float x0 = 0.0;
				float x1 = 0.0;
				if (_FieldNumber < 1.0) {
					return baseColor;
				} else if (_FieldNumber < 2.0) {
					x0 = 0.0;
					x1 = 0.25;
				} else if (_FieldNumber < 3.0) {
					x0 = 0.25;
					x1 = 0.5;
				} else if (_FieldNumber < 4.0) {
					x0 = 0.5;
					x1 = 0.75;
				} else if (_FieldNumber < 5.0) {
					x0 = 0.75;
					x1 = 1.0;
				}
				if (i.texcoord.x >= x0 && i.texcoord.x <= x1) {
					baseColor = _TintColor;
				}
				return baseColor;
			}
			ENDCG 
		}
	}
}
}