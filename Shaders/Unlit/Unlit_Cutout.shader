// VacuumShaders 2015
// https://www.facebook.com/VacuumShaders

Shader "VacuumShaders/Vertex Color/Unlit/Cutout"
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

		Pass
	    {			  
		    ColorMask RGB


            CGPROGRAM 
		    #pragma vertex vert
	    	#pragma fragment frag
	    	#pragma multi_compile_fog
				

			#pragma shader_feature V_VC_MAIN_COLORS_OFF V_VC_MAIN_COLORS_ON
			#pragma shader_feature V_VC_REFLECTION_OFF  V_VC_REFLECTION_ON
			#pragma shader_feature V_VC_IBL_OFF         V_VC_IBL_ON
			#pragma shader_feature V_VC_EMISSION_OFF    V_VC_EMISSION_ON

			#define V_VC_ALPHATEST

			
			#include "../cginc/VertexColor_Unlit.cginc"			  

			ENDCG 

    	} //Pass			 
        
    } //SubShader

	CustomEditor "VertexColor_MaterialEditor"

} //Shader
