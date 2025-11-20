Shader "ImphenziaPixPal"
{
	Properties
	{
		_Attributes("Attributes", 2D) = "white" {}
		_BaseColor("BaseColor", 2D) = "white" {}
		_Emission("Emission", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _BaseColor;
		uniform sampler2D _Attributes;
		uniform float4 _Attributes_ST;
		uniform sampler2D _Emission;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Attributes = i.uv_texcoord * _Attributes_ST.xy + _Attributes_ST.zw;
			float4 tex2DNode1 = tex2D( _Attributes, uv_Attributes );
			float4 appendResult11 = (float4(i.uv_texcoord.x , ( i.uv_texcoord.y - ( tex2DNode1.b * _Time.y ) ) , 0.0 , 0.0));
			o.Albedo = tex2D( _BaseColor, appendResult11.xy ).rgb;
			o.Emission = ( tex2D( _Emission, appendResult11.xy ) * 20.0 ).rgb;
			o.Metallic = tex2DNode1.r;
			o.Smoothness = tex2DNode1.g;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}
