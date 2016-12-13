// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

#ifndef VACUUM_SHADERS_VC_UNLIT_CGINC
#define VACUUM_SHADERS_VC_UNLIT_CGINC


#include "UnityCG.cginc"

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

	#if defined(V_VC_REFLECTION_ON) || defined(V_VC_IBL_ON)
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

	#ifdef V_VC_IBL_ON
		float3 worldNormal : TEXCOORD2;
	#endif					
					
	UNITY_FOG_COORDS(3)


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
		o.worldRefl = mul ((float3x3)unity_ObjectToWorld, viewRefl);
	#endif
				 
	#ifdef V_VC_IBL_ON
		o.worldNormal = UnityObjectToWorldNormal(v.normal);
	#endif

	UNITY_TRANSFER_FOG(o,o.pos);


	o.color = v.color;
	o.color.a = 1.0;

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


	#ifdef V_VC_IBL_ON
		fixed3 ibl = ((texCUBE(_V_VC_IBL_Cube, i.worldNormal).rgb - 0.5) * _V_VC_IBL_Cube_Contrast + 0.5) * _V_VC_IBL_Cube_Intensity;
					
		albedo.rgb = albedo.rgb * (_V_VC_IBL_Light_Intensity + ibl);
	#endif

	#ifdef V_VC_REFLECTION_ON
		fixed4 reflcol = texCUBE (_Cube, i.worldRefl);
		reflcol *= albedo.a;

		albedo.rgb += reflcol.rgb * _ReflectColor.rgb;
	#endif

	#ifdef V_VC_EMISSION_ON
		albedo.rgb += albedo.rgb * albedo.a * _V_VC_EmissionColor.rgb * _V_VC_EmissionStrength;
	#endif


	UNITY_APPLY_FOG(i.fogCoord, albedo);

	#ifndef V_VC_TRANSPARENT
		UNITY_OPAQUE_ALPHA(albedo.a);
	#endif

	return albedo;
}

#endif