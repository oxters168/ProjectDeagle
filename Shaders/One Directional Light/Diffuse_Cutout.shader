// VacuumShaders 2015
// https://www.facebook.com/VacuumShaders

Shader "VacuumShaders/Vertex Color/One Directional Light/Cutout"
{ 
	Properties 
	{   
		[HideInInspector] _Color("Color", color) = (1, 1, 1, 1)
		[HideInInspector] _MainTex("Texture", 2D) = "white"{}
		
		[HideInInspector] _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
		[HideInInspector] _Cube ("Reflection Cubemap", Cube) = "_Skybox" { }
		
		[HideInInspector] _V_VC_IBL_Cube("IBL Cube", cube ) = ""{}  
		[HideInInspector] _V_VC_IBL_Cube_Intensity("IBL Cube Intensity", float) = 1
		[HideInInspector] _V_VC_IBL_Cube_Contrast("IBL Cube Contrast", float) = 1 
		[HideInInspector] _V_VC_IBL_Light_Intensity("IBL Light Intensity", Range(-1, 1)) = 0

		[HideInInspector] _V_VC_EmissionColor("Emission Color", color) = (1, 1, 1, 1)
		[HideInInspector] _V_VC_EmissionStrength("Emission Strength", float) = 1

		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	}

    SubShader 
    { 
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" "VertexColor"="True"}
		LOD 200

		Pass
	    {		
		
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }	  
			ColorMask RGB 

            CGPROGRAM 
		    #pragma vertex vert
	    	#pragma fragment frag
	    	#pragma multi_compile_fog
			#pragma multi_compile_fwdbase nodirlightmap nodynlightmap
			#define UNITY_PASS_FORWARDBASE
			#include "UnityCG.cginc"
			#include "Lighting.cginc" 
			#include "AutoLight.cginc"
				

			#pragma shader_feature V_VC_MAIN_COLORS_OFF V_VC_MAIN_COLORS_ON
			#pragma shader_feature V_VC_REFLECTION_OFF  V_VC_REFLECTION_ON
			#pragma shader_feature V_VC_IBL_OFF         V_VC_IBL_ON
			#pragma shader_feature V_VC_EMISSION_OFF    V_VC_EMISSION_ON

			#define V_VC_ALPHATEST
			

			#include "../cginc/VertexColor_ODL.cginc"

			ENDCG 

    	} //Pass
		
				 	 
        Pass 
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			// compile directives
			#pragma vertex vert_surf
			#pragma fragment frag_surf
			#pragma multi_compile_shadowcaster
			#define UNITY_PASS_SHADOWCASTER
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#pragma shader_feature V_VC_MAIN_COLORS_OFF V_VC_MAIN_COLORS_ON


			fixed4 _Color;
			#ifdef V_VC_MAIN_COLORS_ON		
				sampler2D _MainTex;
				float4 _MainTex_ST;
			#endif

			fixed _Cutoff;


			// vertex-to-fragment interpolation data
			struct v2f_surf 
			{
				V2F_SHADOW_CASTER;
				
				float3 worldPos : TEXCOORD1;
				#ifdef V_VC_MAIN_COLORS_ON
					float2 texcoord : TEXCOORD2; // _MainTex
				#endif
				
			};


			// vertex shader
			v2f_surf vert_surf (appdata_full v) 
			{
				v2f_surf o;
				UNITY_INITIALIZE_OUTPUT(v2f_surf,o);	
	
				o.worldPos =  mul(_Object2World, v.vertex).xyz;
				#ifdef V_VC_MAIN_COLORS_ON
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				#endif

				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
	
				return o;
			}

			 
			// fragment shader
			fixed4 frag_surf (v2f_surf IN) : SV_Target 
			{
				fixed alpha = 1.0;

				#ifdef V_VC_MAIN_COLORS_ON
					alpha *= tex2D(_MainTex, IN.texcoord).a * _Color.a;
				#endif

				// alpha test
				clip (alpha - _Cutoff);
				SHADOW_CASTER_FRAGMENT(IN)
			}
			 
			ENDCG
		}
    } //SubShader


	Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
	CustomEditor "VertexColor_MaterialEditor"

} //Shader
