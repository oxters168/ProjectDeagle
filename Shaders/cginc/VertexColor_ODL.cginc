#ifndef VACUUM_SHADERS_VC_ODL_CGINC
#define VACUUM_SHADERS_VC_ODL_CGINC


fixed4 _Color;
#ifdef V_VC_MAIN_COLORS_ON		
	sampler2D _MainTex;
	float4 _MainTex_ST;
#endif

#ifdef V_VC_REFLECTION_ON
	fixed4 _ReflectColor;
	samplerCUBE _Cube;
#endif

#ifdef V_VC_IBL_ON
	samplerCUBE _V_VC_IBL_Cube;
	fixed _V_VC_IBL_Cube_Intensity;
	fixed _V_VC_IBL_Cube_Contrast;
	fixed _V_VC_IBL_Light_Intensity;
#endif

#ifdef V_VC_EMISSION_ON
	fixed4 _V_VC_EmissionColor;
	half _V_VC_EmissionStrength;
#endif

#ifdef V_VC_ALPHATEST
	fixed _Cutoff;
#endif


struct vInput
{
	float4 vertex : POSITION;

	#ifdef V_VC_MAIN_COLORS_ON
		float4 texcoord : TEXCOORD0;
	#endif

	#ifndef LIGHTMAP_OFF
		float4 texcoord1 : TEXCOORD1;
	#endif

	#if defined(V_VC_REFLECTION_ON) || defined(V_VC_IBL_ON) || defined(LIGHTMAP_OFF)
		float3 normal : NORMAL;
	#endif

	fixed4 color : COLOR;
};

struct vOutput
{
	float4 pos :SV_POSITION;

	#ifdef V_VC_MAIN_COLORS_ON
		float2 texcoord : TEXCOORD0;
	#endif

	#ifdef V_VC_REFLECTION_ON
		float3 worldRefl : TEXCOORD1;
	#endif

	#if defined(V_VC_IBL_ON) || defined(LIGHTMAP_OFF)
		float3 worldNormal : TEXCOORD2;
	#endif

	#ifdef LIGHTMAP_OFF
		fixed3 vlight : TEXCOORD3;
		SHADOW_COORDS(4)
	#else
		float2 lmap : TEXCOORD4;
	#endif

	
	UNITY_FOG_COORDS(5)
						
	fixed4 color : COLOR;		
};


vOutput vert(vInput v)
{
	vOutput o;

	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

	#ifdef V_VC_MAIN_COLORS_ON
		o.texcoord = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
	#endif

	#ifdef V_VC_REFLECTION_ON
		float3 viewDir = -ObjSpaceViewDir(v.vertex);
		float3 viewRefl = reflect (viewDir, v.normal);
		o.worldRefl = mul ((float3x3)_Object2World, viewRefl);
	#endif

				 
	#if defined(V_VC_IBL_ON) || defined(LIGHTMAP_OFF)
		o.worldNormal = UnityObjectToWorldNormal(v.normal);
	#endif

	#ifndef LIGHTMAP_OFF
		o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#endif


	// SH/ambient and vertex lights
	#ifdef LIGHTMAP_OFF
		#ifdef UNITY_SHOULD_SAMPLE_SH
			float3 shlight = ShadeSH9 (float4(o.worldNormal, 1.0));
			o.vlight = shlight;
					
			#ifdef VERTEXLIGHT_ON
				float3 worldPos = mul(_Object2World, v.vertex).xyz;

				o.vlight += Shade4PointLights (unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
											   unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
											   unity_4LightAtten0, worldPos, o.worldNormal );
			#endif // VERTEXLIGHT_ON
		#endif
	#endif // LIGHTMAP_OFF

	o.color = v.color;
	o.color.a = 1.0;

	
	#ifdef LIGHTMAP_OFF
		TRANSFER_SHADOW(o);
	#endif
	
	UNITY_TRANSFER_FOG(o,o.pos); 

	return o;
}

fixed4 frag(vOutput i) : SV_Target 
{		
	fixed4 albedo = i.color;

	#ifdef V_VC_MAIN_COLORS_ON
		albedo *= tex2D(_MainTex, i.texcoord) * _Color;
	#endif


	#ifdef V_VC_ALPHATEST
		clip (albedo.a - _Cutoff);
	#endif


	
	#ifdef LIGHTMAP_OFF
		fixed3 diff = _LightColor0.rgb * max (0, dot (i.worldNormal, _WorldSpaceLightPos0.xyz)) * LIGHT_ATTENUATION(i) + i.vlight.rgb;
	#else
		fixed3 diff = DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap.xy));
	#endif 

	
	#ifdef V_VC_IBL_ON
		fixed3 ibl = ((texCUBE(_V_VC_IBL_Cube, i.worldNormal).rgb - 0.5) * _V_VC_IBL_Cube_Contrast + 0.5) * _V_VC_IBL_Cube_Intensity;
					
		diff += _V_VC_IBL_Light_Intensity + ibl;
	#endif

	 
	fixed4 c = 0;
	c.rgb = albedo * diff;


	#ifdef V_VC_REFLECTION_ON
		fixed4 reflcol = texCUBE (_Cube, i.worldRefl);

		c.rgb += reflcol.rgb * _ReflectColor.rgb * albedo.a;
	#endif

	#ifdef V_VC_EMISSION_ON
		c.rgb += albedo.rgb * albedo.a * _V_VC_EmissionColor.rgb * _V_VC_EmissionStrength;
	#endif
			

	c.a = albedo.a;	


	UNITY_APPLY_FOG(i.fogCoord, c); // apply fog
	#ifndef V_VC_TRANSPARENT
		 UNITY_OPAQUE_ALPHA(c.a);
	#endif

	return c;
}

#endif