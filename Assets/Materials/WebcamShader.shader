Shader "Custom/WebcamShader" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_WebcamTex("Webcam Texture", 2D) = "white"{}
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

	uniform Texture2D _WebcamTex;
	float4 _WebcamTex_TexelSize;
	SamplerState sampler_WebcamTex;

	struct vertexOutput {
		float4 pos: SV_POSITION;
		float4 tex0: TEXCOORD0;
		float4 tex1: TEXCOORD1;
	};

	float4 frag(vertexOutput input) : COLOR{
		fixed2 wOffset = fixed2(0.18994f, 0.36f);
		fixed2 wScale = fixed2(0.62012f, 0.6246f);
		float4 webcam = _WebcamTex.Sample(sampler_WebcamTex, wScale *(input.tex0 + wOffset));
		//float4 webcam = _WebcamTex.Sample(sampler_WebcamTex, input.tex0);

		//return cgDepth;
		//return float4(3*sceneDepth.x, 3 * sceneDepth.x, 3 * sceneDepth.x, 1.0f);
		return webcam;
	}
		ENDCG
	}
	}
}