Shader "Custom/Atlas Tiling" {

    Properties {

      _MainTex ("Main Texture", 2D) = "white" {}
	  _BlendTex ("Blend Texture", 2D) = "white" {}
	  _NormalTex ("Normal Texture", 2D) = "white" {}
	  _uv1FracOffset ("Frac Offset", Float) = 0
	  _uv1Offset ("UV1 Offset", Float) = 0
	  _uv2Offset ("UV2 Offset", Float) = 0
	  _uv3Offset ("UV3 Offset", Float) = 0
    }

    SubShader {

      Tags { "RenderType" = "Opaque" }
      CGPROGRAM
      #pragma surface surf Standard fullforwardshadows
	  #pragma target 3.0

      struct Input {

          float2 uv_MainTex;
		  float2 uv2_BlendTex;
		  float2 uv3_NormalTex;
      };

      sampler2D _MainTex;
	  sampler2D _BlendTex;
	  sampler2D _NormalTex;
	  float _uv1FracOffset;
	  float _uv1Offset;
	  float _uv2Offset;
	  float _uv3Offset;
      void surf (Input IN, inout SurfaceOutputStandard o) {
			
			float2 mainUV = float2(((frac(IN.uv_MainTex.x + _uv1Offset) + _uv1FracOffset) * (IN.uv3_NormalTex.x + _uv3Offset)) + (IN.uv2_BlendTex.x + _uv2Offset), ((frac(IN.uv_MainTex.y + _uv1Offset) + _uv1FracOffset) * (IN.uv3_NormalTex.y + _uv3Offset)) + (IN.uv2_BlendTex.y + _uv2Offset));
			o.Albedo = tex2Dgrad(_MainTex, mainUV, ddx(mainUV), ddy(mainUV));
      }
      ENDCG
    } 
    Fallback "Diffuse"
  }