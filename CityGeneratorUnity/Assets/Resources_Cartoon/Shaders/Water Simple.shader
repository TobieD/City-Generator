Shader "Water Flow/Water Simple" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_MainTexMoveSpeedU ("U Move Speed", Range(-6,6)) = 0.5
		_MainTexMoveSpeedV ("V Move Speed", Range(-6,6)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		fixed _MainTexMoveSpeedU;
		fixed _MainTexMoveSpeedV;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
		
			fixed2 MainTexMoveScrolledUV = IN.uv_MainTex;
			
			fixed MainTexMoveU = _MainTexMoveSpeedU * _Time;
			fixed MainTexMoveV = _MainTexMoveSpeedV * _Time;
			
			MainTexMoveScrolledUV += fixed2(MainTexMoveU, MainTexMoveV);
		
			half4 c = tex2D (_MainTex, MainTexMoveScrolledUV);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 
}