Shader "VacuumShaders/Vertex Color/Legacy Shaders/Self-Illumin/Bumped Diffuse" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_Illum ("Illumin (A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
		_EmissionLM ("Emission (Lightmapper)", Float) = 0
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 300

		CGPROGRAM
		#pragma surface surf Lambert
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _Illum;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_Illum;
			float2 uv_BumpMap;

			fixed4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 c = tex * _Color;
			o.Albedo = c.rgb * IN.color.rgb;
			o.Emission = c.rgb * tex2D(_Illum, IN.uv_Illum).a;
			o.Alpha = c.a;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		}
		ENDCG
	} 

	FallBack "VacuumShaders/Vertex Color/Legacy Shaders/Self-Illumin/Diffuse" 
}
