﻿
struct a2fd
{
	half3 positionOS:POSITION;
	half3 normalOS:NORMAL;
	half4 tangentOS:TANGENT;
	half4 color:COLOR;
#if defined(_ALPHACLIP)
	float2 uv:TEXCOORD0;
#endif
#if defined(A2V_ADDITIONAL)
	A2V_ADDITIONAL
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2fd
{
	float4 positionCS:SV_POSITION;
	half3 normalWS:NORMAL;
#if defined(_ALPHACLIP)
	float2 uv:TEXCOORD0;
#endif
#if defined(V2F_ADDITIONAL)
	V2F_ADDITIONAL
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2fd DepthVertex(a2fd v)
{
	v2fd o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v,o);
	o.normalWS=TransformObjectToWorldNormal(v.normalOS);
#if defined(GET_POSITION_WS)
	float3 positionWS= GET_POSITION_WS(v,o);
#else
	float3 positionWS=TransformObjectToWorld(v.positionOS);
#endif 
	o.positionCS=TransformWorldToHClip(positionWS);
#if defined(_ALPHACLIP)
	o.uv = TRANSFORM_TEX_INSTANCE(v.uv,_MainTex);
#endif
#if defined(V2F_ADDITIONAL_TRANSFER)
	V2F_ADDITIONAL_TRANSFER(v,o)
#endif

	return o;
}

float4 DepthFragment(v2fd i) :SV_TARGET
{
	#if defined(_ALPHACLIP)
		clip(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a*INSTANCE(_Color.a)-INSTANCE(_AlphaCutoff));
	#endif
	return i.positionCS.z;
}