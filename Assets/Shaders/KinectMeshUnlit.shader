Shader "Unlit/KinectMeshUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_DepthTexture ("Depth Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            
            #include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			StructuredBuffer<int> depthCoordinates;

            v2f vert (appdata v)
            {
                v2f o;

				// v.vertex.z = THE DEPTH DATA

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

			// geom shader to remove triangles

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }

            ENDCG
        }
    }
}
