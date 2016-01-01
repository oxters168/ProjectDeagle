Shader "Custom/AtlasTilingVertex" {
	Properties {
		//_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {}
		_uv1Offsetx ("UV1 Offset X", float) = 0
		_uv1Offsety ("UV1 Offset Y", float) = 0
		_uv2Offsetx ("UV2 Offset X", float) = 0
		_uv2Offsety ("UV2 Offset Y", float) = 0
		_uv3Offsetx ("UV3 Offset X", float) = 0
		_uv3Offsety ("UV3 Offset Y", float) = 0
		//_BlendTex ("Blend Texture", 2D) = "white" {}
		//_Glossiness ("Smoothness", Range(0,1)) = 0.5
		//_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		//Tags { "RenderType"="Opaque" }
		//LOD 200
		
		Pass {
			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct v2f members uv1Offsetx,uv2Offsetx,uv3Offsetx)
#pragma exclude_renderers d3d11 xbox360
			// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct v2f members texColor)
			//#pragma exclude_renderers d3d11 xbox360
			#pragma vertex vert
			#pragma fragment frag
			//#pragma target 5.0

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float2 uv3 : TEXCOORD2;
			};

			struct v2f {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float2 uv3 : TEXCOORD2;
			};

			sampler2D _MainTex;
			//sampler2D _BlendTex;
			float _uv1Offsetx;
			float _uv1Offsety;
			float _uv2Offsetx;
			float _uv2Offsety;
			float _uv3Offsetx;
			float _uv3Offsety;

			v2f vert (appdata IN) {
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.uv = IN.uv;
				OUT.uv2 = IN.uv2;
				OUT.uv3 = IN.uv3;
				return OUT;
			}
		
			half4 frag(v2f IN) : COLOR {
				float2 mainUV = float2((frac((IN.uv.x + _uv1Offsetx)) * (IN.uv3.x + _uv3Offsetx)) + (IN.uv2.x + _uv2Offsetx), (frac((IN.uv.y + _uv1Offsety)) * (IN.uv3.y + _uv3Offsety)) + (IN.uv2.y + _uv2Offsety));
				fixed4 texColor = tex2D(_MainTex, mainUV);
				return texColor;
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
