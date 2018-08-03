Shader "Custom/MainShader" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	}

		SubShader{
		Pass{
		CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc" // required for v2f_img

		// Properties
		uniform Texture2D _MainTex;
	float4 _MainTex_TexelSize;
	uniform SamplerState sampler_MainTex;


	struct vertexOutput {
		float4 pos: SV_POSITION;
		float4 tex0: TEXCOORD0;
		float4 tex1: TEXCOORD1;
	};

	float4 frag(vertexOutput input) : COLOR{
	float4 base = _MainTex.Sample(sampler_MainTex, input.tex0);

	//return cgDepth;
	//return float4(3*sceneDepth.x, 3 * sceneDepth.x, 3 * sceneDepth.x, 1.0f);
	return base;
	}
		ENDCG
	}
	}
}