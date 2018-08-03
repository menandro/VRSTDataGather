Shader "Custom/ColorShader" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	_ColorTex("Color Texture", 2D) = "white"{}
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

	uniform Texture2D _ColorTex;
	float4 _ColorTex_TexelSize;
	SamplerState sampler_ColorTex;

	struct vertexOutput {
		float4 pos: SV_POSITION;
		float4 tex0: TEXCOORD0;
		float4 tex1: TEXCOORD1;
	};

	float4 frag(vertexOutput input) : COLOR{
	float4 color = _ColorTex.Sample(sampler_ColorTex, input.tex0);

	//return cgDepth;
	//return float4(3*sceneDepth.x, 3 * sceneDepth.x, 3 * sceneDepth.x, 1.0f);
	return color;
	}
		ENDCG
	}
	}
}