Shader "VacuumShaders/Vertex Color/Legacy Shaders/Reflective/Diffuse" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
		_MainTex ("Base (RGB) RefStrength (A)", 2D) = "white" {} 
		_Cube ("Reflection Cubemap", Cube) = "_Skybox" { }
	}

	SubShader 
	{
		LOD 200
		Tags { "RenderType"="Opaque" }
	
		CGPROGRAM
		#pragma surface surf Lambert
		//Too many output registers declared (12)
		#pragma exclude_renderers d3d9

		sampler2D _MainTex;
		samplerCUBE _Cube;

		fixed4 _Color;
		fixed4 _ReflectColor;

		struct Input 
		{
			float2 uv_MainTex;
			float3 worldRefl;

			fixed4 color:COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 c = tex * _Color;
			o.Albedo = c.rgb * IN.color.rgb;
	
			fixed4 reflcol = texCUBE (_Cube, IN.worldRefl);
			reflcol *= tex.a;
			o.Emission = reflcol.rgb * _ReflectColor.rgb;
			o.Alpha = reflcol.a * _ReflectColor.a;
		}
		ENDCG
	}
	
	FallBack "VacuumShaders/Vertex Color/Legacy Shaders/Diffuse"
} 
