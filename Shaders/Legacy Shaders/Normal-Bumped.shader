Shader "VacuumShaders/Vertex Color/Legacy Shaders/Bumped Diffuse" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 300

		CGPROGRAM
		#pragma surface surf Lambert
		//input limit (8) exceeded, shader uses 9
		#pragma exclude_renderers d3d11_9x

		sampler2D _MainTex;
		sampler2D _BumpMap;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;

			fixed4 color : COLOR;
		};
			

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb * IN.color.rgb;
			o.Alpha = c.a;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		}
		ENDCG  
	}

	FallBack "VacuumShaders/Vertex Color/Legacy Shaders/Diffuse" 
}
