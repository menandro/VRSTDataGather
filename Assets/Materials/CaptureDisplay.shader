Shader "Custom/CaptureDisplay" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_SpatialMapTex("Spatial Mapping Depth Texture", 2D) = "white"{}
		_CgDepthTex("Cg Depth Texture", 2D) = "white"{}
		_CgColorTex("Cg Color Texture", 2D) = "white"{}
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

		uniform Texture2D _SpatialMapTex;
		float4 _SpatialMapTex_TexelSize;
		uniform SamplerState sampler_SpatialMapTex;

		uniform Texture2D _CgDepthTex;
		float4 _CgDepthTex_TexelSize;
		uniform SamplerState sampler_CgDepthTex;

		uniform Texture2D _WebcamTex;
		float4 _WebcamTex_TexelSize;
		SamplerState sampler_WebcamTex;

		uniform Texture2D _CgColorTex;
		float4 _CgColorTex_TexelSize;
		uniform SamplerState sampler_CgColorTex;

		struct vertexInput {
			float4 pos : POSITION;
			float4 tex0 : TEXCOORD0;
			float4 tex1 : TEXCOORD1;
		};

		struct vertexOutput {
			float4 pos: SV_POSITION;
			float4 tex0: TEXCOORD0;
			float4 tex1: TEXCOORD1;
		};

		float4 frag(vertexOutput input) : COLOR{
			float4 base = _MainTex.Sample(sampler_MainTex, input.tex0);
			float4 cgDepth = _CgDepthTex.Sample(sampler_CgDepthTex, input.tex0);
			float4 cgColor = _CgColorTex.Sample(sampler_CgColorTex, input.tex0);
			float4 sceneDepth = _SpatialMapTex.Sample(sampler_SpatialMapTex, input.tex0);
			float4 webcam = _WebcamTex.Sample(sampler_WebcamTex, input.tex0);
			
			//return cgDepth;
			//return float4(3*sceneDepth.x, 3 * sceneDepth.x, 3 * sceneDepth.x, 1.0f);
			return webcam;
		}
		ENDCG
	}
	}
}
