Shader "Unlit/TextureViewer"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			Texture3D<float> DisplayTexture;
			SamplerState samplerDisplayTexture;
			float depth;
			bool showBoundary;
			bool showExactValue;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};


			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 uv3 = float3(i.uv.xy, depth);
				float val = DisplayTexture.SampleLevel(samplerDisplayTexture, uv3, 0);
				float4 col;
				if (val < 0.001 && val > -0.001 && showBoundary) {
					col = float4(1, 0, 0, 1);
				}
				else {
					if (showExactValue) {
						col = val;
					}
					else {
						col = (val > 0);
					}
				}

				return col;
			}
			ENDCG
		}
	}
}
