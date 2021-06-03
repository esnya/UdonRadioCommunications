// Upgrade NOTE: upgraded instancing buffer 'URCTMP_Offset' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "URC/TMP_Offset"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[HDR]_Color("Tint", Color) = (1,1,1,1)
		_MainTex("Main Tex", 2D) = "white" {}
		_OffsetFactor("Offset Factor", Float) = -1
		_OffsetUnits("Offset Units", Float) = -1
		_ScaleRotioA("Scale Rotio A", Float) = 1
		_FaceDilate("Face Dilate", Range( -1 , 1)) = 0
		_OutlineSoftness("Outline Softness", Range( 0 , 1)) = 0
		_GradientScale("Gradient Scale", Float) = 5
		_Sharpness("Sharpness", Range( -1 , 1)) = 0
		_WeightNormal("Weight Normal", Float) = 0
		_WeightBold("Weight Bold", Float) = 0.5
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" }
		Cull Back
		Offset  [_OffsetFactor] , [_OffsetUnits]
		CGPROGRAM
		#include "UnityCG.cginc"
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			float3 worldPos;
			float2 uv2_texcoord2;
		};

		uniform float _OffsetFactor;
		uniform float _OffsetUnits;
		uniform sampler2D _MainTex;
		uniform float _GradientScale;
		uniform float _Sharpness;
		uniform float _OutlineSoftness;
		uniform float _ScaleRotioA;
		uniform float _WeightNormal;
		uniform float _WeightBold;
		uniform float _FaceDilate;
		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(URCTMP_Offset)
			UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
#define _Color_arr URCTMP_Offset
			UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
#define _MainTex_ST_arr URCTMP_Offset
		UNITY_INSTANCING_BUFFER_END(URCTMP_Offset)

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 _Color_Instance = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
			float4 temp_output_3_0 = ( i.vertexColor * _Color_Instance );
			o.Albedo = (temp_output_3_0).rgb;
			o.Alpha = 1;
			float4 _MainTex_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_MainTex_ST_arr, _MainTex_ST);
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST_Instance.xy + _MainTex_ST_Instance.zw;
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float4 unityObjectToClipPos21 = UnityObjectToClipPos( ase_vertex3Pos );
			float2 appendResult23 = (float2(unityObjectToClipPos21.w , unityObjectToClipPos21.w));
			float2 temp_output_24_0 = ( appendResult23 / abs( (mul( unity_CameraProjection, _ScreenParams )).xy ) );
			float dotResult20 = dot( temp_output_24_0 , temp_output_24_0 );
			float temp_output_30_0 = ( rsqrt( dotResult20 ) * abs( i.uv2_texcoord2.y ) * _GradientScale * ( _Sharpness + 1.0 ) );
			float temp_output_36_0 = ( temp_output_30_0 / ( 1.0 + ( _OutlineSoftness * _ScaleRotioA * temp_output_30_0 ) ) );
			float lerpResult53 = lerp( _WeightNormal , _WeightBold , step( i.uv2_texcoord2.y , 0.0 ));
			clip( ( temp_output_3_0.a * saturate( ( ( tex2D( _MainTex, uv_MainTex ).a * temp_output_36_0 ) - ( ( ( 0.5 - ( ( ( lerpResult53 / 4.0 ) + _FaceDilate ) * _ScaleRotioA * 0.5 ) ) * temp_output_36_0 ) - 0.5 ) ) ) ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18909
0;1163;1490;917;4306.261;1271.016;2.845027;True;True
Node;AmplifyShaderEditor.CommentaryNode;40;-3887.127,-1264.217;Inherit;False;2207;972.6912;Comment;21;20;22;21;23;24;25;27;28;26;29;19;32;31;30;35;34;36;33;37;38;39;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CameraProjectionNode;27;-3786.498,-943.9919;Inherit;False;unity_CameraProjection;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.ScreenParams;28;-3837.127,-782.2168;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-3521.355,-907.4948;Inherit;False;2;2;0;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PosVertexDataNode;22;-3389.127,-1214.217;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;29;-3293.127,-910.2168;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.UnityObjToClipPosHlpNode;21;-3197.127,-1214.217;Inherit;False;1;0;FLOAT3;0,0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;57;-3067.731,-196.8188;Inherit;False;1872.381;943.2318;bias;15;56;50;51;55;53;54;48;45;47;41;43;44;15;49;46;;1,1,1,1;0;0
Node;AmplifyShaderEditor.AbsOpNode;25;-2973.127,-974.2169;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;23;-2973.127,-1214.217;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;24;-2813.127,-1214.217;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;56;-3017.731,400.6013;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;32;-2946.512,-789.3613;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;34;-2657.828,-580.0363;Inherit;False;Property;_Sharpness;Sharpness;9;0;Create;False;0;0;0;False;0;False;0;0;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;20;-2557.127,-1086.217;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;55;-2645.557,337.332;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-2721.433,208.413;Inherit;False;Property;_WeightBold;Weight Bold;11;0;Create;False;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;50;-2754.433,133.413;Inherit;False;Property;_WeightNormal;Weight Normal;10;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;53;-2479.433,168.413;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RSqrtOpNode;19;-2221.127,-1086.217;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;35;-2232.786,-620.5271;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;31;-2614.423,-843.6843;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-2370.331,-766.6684;Inherit;False;Property;_GradientScale;Gradient Scale;8;0;Create;False;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-2429.433,432.4131;Inherit;False;Property;_FaceDilate;Face Dilate;6;0;Create;False;0;0;0;False;0;False;0;0;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;39;-2342.823,-417.6486;Inherit;False;Property;_OutlineSoftness;Outline Softness;7;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-2080.04,-1029.48;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-2602.411,-72.84601;Inherit;False;Property;_ScaleRotioA;Scale Rotio A;5;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;54;-2150.433,138.413;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-2153.433,631.4131;Inherit;False;Constant;_Float0;Float 0;5;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;45;-2122.433,342.4131;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-2028.189,-431.4031;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-1980.432,405.4131;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;37;-1880.329,-424.5257;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;41;-1752.902,65.84184;Inherit;False;2;0;FLOAT;0.5;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;36;-1837.127,-1022.217;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-1557.045,97.60242;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;4;-885.1426,227.7841;Inherit;True;Property;_MainTex;Main Tex;2;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleSubtractOpNode;44;-1410.914,211.411;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;12;-1264,-512;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-536.1645,230.6563;Inherit;True;Property;_TextureSample0;Texture Sample 0;2;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;2;-640,0;Inherit;False;InstancedProperty;_Color;Tint;1;1;[HDR];Create;False;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RelayNode;15;-1349.35,-146.8188;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-100.4211,705.4033;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;1;-664.1598,-223.0264;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-267.6095,-10.61243;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;18;128,704;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;17;242.8815,568.6089;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;7;-174.2836,221.3349;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;10;-668.772,768.9983;Inherit;False;Property;_OffsetUnits;Offset Units;4;0;Create;True;0;0;0;True;0;False;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;14;-1264,-352;Inherit;False;1;0;OBJECT;;False;1;OBJECT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;92.90007,396.1264;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;6;-54.9906,-52.47089;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-684.3057,658.0616;Inherit;False;Property;_OffsetFactor;Offset Factor;3;0;Create;True;0;0;0;True;0;False;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;13;-1264,-432;Inherit;False;1;0;OBJECT;;False;1;OBJECT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;320.0286,17.04295;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;URC/TMP_Offset;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;True;0;True;9;0;True;10;False;0;Custom;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;26;0;27;0
WireConnection;26;1;28;0
WireConnection;29;0;26;0
WireConnection;21;0;22;0
WireConnection;25;0;29;0
WireConnection;23;0;21;4
WireConnection;23;1;21;4
WireConnection;24;0;23;0
WireConnection;24;1;25;0
WireConnection;20;0;24;0
WireConnection;20;1;24;0
WireConnection;55;0;56;2
WireConnection;53;0;50;0
WireConnection;53;1;51;0
WireConnection;53;2;55;0
WireConnection;19;0;20;0
WireConnection;35;0;34;0
WireConnection;31;0;32;2
WireConnection;30;0;19;0
WireConnection;30;1;31;0
WireConnection;30;2;33;0
WireConnection;30;3;35;0
WireConnection;54;0;53;0
WireConnection;45;0;54;0
WireConnection;45;1;46;0
WireConnection;38;0;39;0
WireConnection;38;1;48;0
WireConnection;38;2;30;0
WireConnection;47;0;45;0
WireConnection;47;1;48;0
WireConnection;47;2;49;0
WireConnection;37;1;38;0
WireConnection;41;1;47;0
WireConnection;36;0;30;0
WireConnection;36;1;37;0
WireConnection;43;0;41;0
WireConnection;43;1;36;0
WireConnection;44;0;43;0
WireConnection;12;0;36;0
WireConnection;5;0;4;0
WireConnection;5;7;4;1
WireConnection;15;0;44;0
WireConnection;16;0;5;4
WireConnection;16;1;12;0
WireConnection;3;0;1;0
WireConnection;3;1;2;0
WireConnection;18;0;16;0
WireConnection;18;1;15;0
WireConnection;17;0;18;0
WireConnection;7;0;3;0
WireConnection;8;0;7;3
WireConnection;8;1;17;0
WireConnection;6;0;3;0
WireConnection;0;0;6;0
WireConnection;0;10;8;0
ASEEND*/
//CHKSM=013CECFABE9AF7A2DF3ED30F35BCE8EC923236FF