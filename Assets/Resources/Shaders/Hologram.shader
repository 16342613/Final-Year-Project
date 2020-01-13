Shader "Unlit/Custom/Hologram"  // The directory path of the shader
{
	// No semicolons at the end of lines in properties declaration
    Properties
    {
		// NOTATION: VariableName("VariableName_in_Unity", Type) = DefaultValues
        _MainTex ("Texture", 2D) = "white" {}
		
		_TintColour("Tint Colour", Color) = (1, 1, 1, 1)	// Add a tint property that has a default value of white
		_CutoutThreshold("Cutout Threshold", Range(0.0, 1.0)) = 0
		_Distance("Distance", float) = 1
		_Amplitude("Amplitude", float) = 0
		_Speed("Speed", Range(0.0, 1.0)) = 1
		_Amount("Amount", float) = 1

		[HideInInspector]_ForceOrigin("Force Origin", vector) = (0, 0, 0)
		[HideInInspector]_Vertex("Vertex", vector) = (0, 0, 0)
		[HideInInspector]_VertexVelocity("VertexVelocity", vector) = (0, 0, 0)
    }
	// You can have different subshaders for different platforms
    SubShader
    {
		// Instructions for Unity on how to set up the renderer
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100	// The level of detail

		ZWrite Off	// Tells it not to render to the depth buffer
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
		// The actual code run on the GPU
            CGPROGRAM
            #pragma vertex vert		// We have a vertex function called vert
            #pragma fragment frag	// We have a fragment function called frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"	// We are adding Unity's CG helper functions

			// structs are data objects, but you don't have to use them.
			// You can pass the data to the functions themselves normally if you want.

			// The appdata struct passes in information
            struct appdata
            {
				// NOTATION: type name : semanticBinding
                float4 vertex : POSITION;	// This variable contains 4 floating point numbers [x, y, z, w]
                float2 uv : TEXCOORD0;		// The UV for the model
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;  // SV_POSITION is screen space position
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _TintColour;
			float _CutoutThreshold;
			float _Distance;
			float _Amplitude;
			float _Speed;
			float _Amount;
			
			vector _ForceOrigin;
			vector _Vertex;
			float _Test;

            v2f vert (appdata v)
            {
                v2f o;										// This is what we are going to return
				
				//_Distance = sqrt((_ForceOrigin.x - _Vertex.x)*(_ForceOrigin.x - _Vertex.x) + 
										//(_ForceOrigin.y - _Vertex.y)*(_ForceOrigin.y - _Vertex.y) + 
										//(_ForceOrigin.z - _Vertex.z)*(_ForceOrigin.z - _Vertex.z));

				//v.vertex.x += sin(_Time.y * _Speed + v.vertex.y * _Amplitude) * _Distance * _Amount;
                o.vertex = UnityObjectToClipPos(v.vertex);	// A Unity helper function that translates from object space to clip space
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);		// Transforming the texture of the model
                UNITY_TRANSFER_FOG(o,o.vertex);

                return o;
            }

			// The fragment colour takes in a v2f struct and is bound to the frame buffer's render target
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _TintColour;		// Draws the pixels
				if (col.r < _CutoutThreshold) {
					discard;
				}
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
