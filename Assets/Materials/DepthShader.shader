Shader "Custom/DepthShader" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_DepthTex("DepthTexture", 2D) = "white"{}
	}

	SubShader{
		Pass{
			CGPROGRAM
	#pragma vertex vert_img
	#pragma fragment frag
	#include "UnityCG.cginc" // required for v2f_img

			 //Properties
			uniform Texture2D _MainTex;
			float4 _MainTex_TexelSize;
			uniform SamplerState sampler_MainTex;

			uniform Texture2D _DepthTex;
			float4 _DepthTex_TexelSize;
			uniform SamplerState sampler_DepthTex;

			struct vertexOutput {
				float4 pos: SV_POSITION;
				float4 tex0: TEXCOORD0;
				float4 tex1: TEXCOORD1;
			};

			float4 frag(vertexOutput input) : COLOR{
				float4 depth = _DepthTex.Sample(sampler_DepthTex, input.tex0);

				//return cgDepth;
				return float4(depth.x, depth.x, depth.x, 1.0f);
				//return webcam;
			}
			ENDCG
		}
	}
}
