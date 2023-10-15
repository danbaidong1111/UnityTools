Shader "Character/LitPBRToon"
{
    Properties
    {
        [FoldoutBegin(_FoldoutTexEnd)]_FoldoutTex("Textures", float) = 0
            _BaseColor  ("BaseColor", Color)    = (0,0,0,1)
            _BaseMap    ("BaseMap_d", 2D)       = "white" {}
            [NoScaleOffset]_PBRMask         ("PBRMask(metal smooth ao)", 2D) = "white" {}
            [NoScaleOffset]_ILMMapSpecType  ("ILMMapSpecType", 2D)          = "white" {}
            [NoScaleOffset]_ILMMapAO        ("ILMMapAO", 2D)                = "white" {}
            [NoScaleOffset]_ILMMapSpecMask  ("ILMMapSpecMask", 2D)          = "white" {}
            [NoScaleOffset]_NormalMap       ("NormalMap", 2D)               = "bump" {}
            _NormalScale("NormalScale",Range(0,1)) = 1
        [FoldoutEnd]_FoldoutTexEnd("_FoldoutEnd", float) = 0

        [FoldoutBegin(_FoldoutPBRPropEnd)]_FoldoutPBRProp("PBR Properties", float) = 0
            _Metallic("Metallic",Range(0,1)) = 0
            _Smoothness("Smoothness",Range(0,1)) = 1
            _Occlusion("Occlusion",Range(0,1)) = 1
            _NdotVAdd("NdotVAdd(Leather Reflect)",Range(0,2)) = 0
        [FoldoutEnd]_FoldoutPBRPropEnd("_FoldoutPBRPropEnd", float) = 0

		[FoldoutBegin(_FoldoutDirectLightEnd)]_FoldoutDirectLight("Direct Light", float) = 0
            [HDR]_SelfLight("SelfLight", Color) = (1,1,1,1)
            _MainLightColorLerp("Unity Light or SelfLight", Range(0,1)) = 0
            _DirectOcclusion("DirectOcclusion",Range(0,1)) = 0.1
            
            [Title(Diffuse)]
            [Title(Shadow)]
            _ShadowColor        ("ShadowColor", Color)                      = (0,0,0,1)
            _ShadowOffset       ("ShadowOffset",Range(-1,1))                = 0.0
            _ShadowSmooth       ("ShadowSmooth", Range(0,1))                = 0.0
            _ShadowStrength     ("ShadowStrength", Range(0,1))              = 1.0
            _SecShadowColor     ("SecShadowColor (ILM texture AO)", Color)  = (0.5,0.5,0.5,1)
            _SecShadowStrength  ("SecShadowStrength", Range(0,1))           = 1.0



            // [Title(Specular)]
            // [Toggle(_DIRECT_BLINNPHONG)]_DIRECT_BLINNPHONG("_DIRECT_BLINNPHONG", Float) = 0
            // _BlinnPhongSpecStrength("SpecStrength", Range(0, 20))               = 1.5

        [FoldoutEnd]_FoldoutDirectLightEnd("_FoldoutEnd", float) = 0


        [FoldoutBegin(_FoldoutShadowRampEnd, _SHADOW_RAMP)]_FoldoutShadowRamp("ShadowRamp", float) = 0
        [HideInInspector]_SHADOW_RAMP("_SHADOW_RAMP", float) = 0
            [Ramp]_ShadowRampTex("ShadowRampTex", 2D) = "white" { }
            
        [FoldoutEnd]_FoldoutShadowRampEnd("_FoldoutEnd", float) = 0


		[FoldoutBegin(_FoldoutIndirectLightEnd)]_FoldoutIndirectLight("Indirect Light", float) = 0
            [Title(Diffuse)]
            [HDR]_SelfEnvColor  ("SelfEnvColor", Color) = (0.5,0.5,0.5,0.5)
            _EnvColorLerp       ("Unity SH or SelfEnv", Range(0,1)) = 0.5
            _IndirDiffUpDirSH   ("IndirDiffUpDirSH", Range(0,1))	= 0.0
            _IndirDiffIntensity ("IndirDiffIntensity", Range(0,1))	= 0.3
            [Title(Specular)]
            [Toggle(_INDIR_CUBEMAP)]_INDIR_CUBEMAP("_INDIR_CUBEMAP", Float) = 0
            _IndirSpecCubemap   ("SpecCube", cube) = "black" {}
            [Toggle(_INDIR_MATCAP)]_INDIR_MATCAP("_INDIR_MATCAP", Float) = 0
            _IndirSpecMatcap    ("Matcap", 2D) = "black" {}

            _IndirSpecMatcapTile("MatcapTile", float)               	= 1.0
            _IndirSpecLerp      ("Unity Reflect or Self Map", Range(0,1))		= 0.3
            _IndirSpecIntensity ("IndirSpecIntensity", Range(0.01,10))	= 1.0

        [FoldoutEnd]_FoldoutIndirectLightEnd("_FoldoutEnd", float) = 0

        [FoldoutBegin(_FoldoutOutlineEnd)]_FoldoutOutline("Outline", float) = 0
            _OutlineWidth("OutlineWidth", Range(0, 10))         = 1.0
            _OutlineClampScale("OutlineClampScale", Range(0.01, 5)) = 1
            _OutlineColor("Outline Color", Color)               = (0, 0, 0, 0.8)
        [FoldoutEnd]_FoldoutOutlineEnd("_FoldoutEnd", float) = 0

		_TestValue("TestValue", float)	= 1.0
        [Enum(UnityEngine.Rendering.CullMode)] 
        _Cull("Cull Mode", Float) =2
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
			"RenderPipeline" = "UniversalPipeline"
			"Queue"="Geometry"
			"IgnoreProjector" = "True"
			"UniversalMaterialType" = "CharacterLit"
        }
        LOD 300

        Cull [_Cull]
        ZWrite On

        HLSLINCLUDE
        #pragma target 2.0

        ENDHLSL


		Pass
        {
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#pragma exclude_renderers d3d11_9x

			#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _HBAO_SCREENSPACE_SHADOWS
			#pragma multi_compile _ _CLOUDLAYER_SHADOWS
			#pragma multi_compile _ Anti_Aliasing_ON
		//	#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_LIGHTS_CLUSTER_CULL
			//#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			//#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			//#pragma multi_compile _ _SHADOWS_SOFT
			
			//#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			//#pragma multi_compile _ SHADOWS_SHADOWMASK

			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON

			// #pragma shader_feature_local _DIRECT_BLINNPHONG
            #pragma shader_feature_local _SHADOW_RAMP
			#pragma shader_feature_local _INDIR_CUBEMAP
			#pragma shader_feature_local _INDIR_MATCAP

			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/UnityGBuffer.hlsl"
			#include "Assets/GameRes/Shaders/CustomPBR/URPPBRFunctions.hlsl"
           
            #pragma vertex vert
            #pragma fragment frag


            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;

			half4 _SelfLight;
			half _MainLightColorLerp;

			half3 _BaseColor;

			half3 _SkinColor;
			half _NormalScale;

			half _Metallic;
			half _Smoothness;
			half _Occlusion;
			half _NdotVAdd;

			half _DirectOcclusion;

			// Shadow
			half4  _ShadowColor;
			float   _ShadowOffset;
			float   _ShadowSmooth;
			float   _ShadowStrength;
			half4 	_SecShadowColor;
			float   _SecShadowStrength;
			half4   _HighCol; 
			half4   _MidCol;
			half4   _LowCol;  
			half    _HighOffset;
			half    _HighSharp;
			half    _MidOffset; 
			half    _MidSharp; 
			half    _LowOffset; 
			half    _LowSharp; 

			// Specular
			half	_BlinnPhongSpecStrength;


			// Indirect
            half4 _SelfEnvColor;
            half _EnvColorLerp;
			half _IndirDiffUpDirSH;
			half _IndirDiffIntensity;
			half _IndirSpecLerp;
			half _IndirSpecMatcapTile;
			half _IndirSpecIntensity;

			half _TestValue;
            CBUFFER_END

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

            TEXTURE2D(_PBRMask);
			SAMPLER(sampler_PBRMask);
		    TEXTURE2D(_NormalMap);
			SAMPLER(sampler_NormalMap);	
			
			// TEXTURE2D(_OcclusionMap);
			// SAMPLER(sampler_OcclusionMap);

			TEXTURE2D(_ILMMapSpecType);
			TEXTURE2D(_ILMMapAO);
			SAMPLER(sampler_ILMMapAO);
			TEXTURE2D(_ILMMapSpecMask);

			TEXTURE2D(_FaceMap);
			SAMPLER(sampler_FaceMap);

            TEXTURE2D(_ShadowRampTex);
			SAMPLER(sampler_ShadowRampTex);

			TEXTURECUBE(_IndirSpecCubemap);

			TEXTURE2D(_IndirSpecMatcap);
			SAMPLER(sampler_IndirSpecMatcap);

			struct a2v 
			{
				float4 vertex 	:POSITION;
				float3 normal 	:NORMAL;
				float4 tangent 	:TANGENT;
				float4 color  	:COLOR;
				float4 uv0 		:TEXCOORD0;
				float4 uv1 		:TEXCOORD1;
				float4 uv2 		:TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID 
			};
			struct v2f 
			{
				float4 positionHCS		:SV_POSITION;
                float3 positionWS   	:TEXCOORD0;
                float3 normalWS     	:TEXCOORD1;
                float3 tangentWS    	:TEXCOORD2;
                float3 biTangentWS  	:TEXCOORD3;
				float4 color 			:TEXCOORD4;
				float2 uv				:TEXCOORD5;
				float2 uv1				:TEXCOORD6;
				float2 uv2				:TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


            v2f vert (a2v v)
			{
				v2f o;
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_TRANSFER_INSTANCE_ID(v,o); 

				o.positionHCS = TransformObjectToHClip(v.vertex);
                o.positionWS = TransformObjectToWorld(v.vertex);

				o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.tangentWS = TransformObjectToWorldDir(v.tangent.xyz);
                o.biTangentWS = cross(o.normalWS,o.tangentWS) * v.tangent.w * GetOddNegativeScale();
				o.color = v.color;

				o.uv  = v.uv0;
				o.uv1 = v.uv1;
				o.uv2 = v.uv2;

				return o;
			}


            FragmentOutput frag(v2f i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half2  UV = i.uv;
				float3 positionWS = i.positionWS;
				float4 shadowCoords = TransformWorldToShadowCoord(positionWS);
				Light mainLight = GetMainLight();
				mainLight.color = lerp(mainLight.color, _SelfLight.rgb, _MainLightColorLerp);

				// Tex Sample
                half4 mainTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, UV);
                half4 pbrMask = SAMPLE_TEXTURE2D(_PBRMask, sampler_PBRMask, UV);
				half3 bumpTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,UV), _NormalScale);
				// half  occlusionTex = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, UV).r;
				half  imlSpecMask = SAMPLE_TEXTURE2D(_ILMMapSpecMask, sampler_LinearClamp, UV).r;
				half  ilmAO = SAMPLE_TEXTURE2D(_ILMMapAO, sampler_ILMMapAO, UV).r;
				ilmAO = lerp(1 - _SecShadowStrength, 1, ilmAO);


				// VectorPrepare
				float3 lightDirWS = SafeNormalize(mainLight.direction);
				float3 camDirWS = GetCameraPositionWS();
				float3 viewDirWS = SafeNormalize(camDirWS - positionWS);
				float3 normalWS = SafeNormalize(i.normalWS);

				float3x3 TBN = float3x3(i.tangentWS, i.biTangentWS, i.normalWS);
				float3 bumpWS = TransformTangentToWorld(bumpTS,TBN);
				normalWS = SafeNormalize(bumpWS);

				float3 halfDir = SafeNormalize(lightDirWS + viewDirWS);
				float halfLambert = dot(normalWS, lightDirWS) * 0.5 + 0.5;
				float NdotL = saturate(dot(normalWS, lightDirWS));
				float NdotV = saturate(dot(normalWS, viewDirWS));
				float NdotH = saturate(dot(normalWS, halfDir));
				float HdotV = saturate(dot(halfDir,  viewDirWS));



				// Property prepare
					half emission			= 1 - mainTex.a;
					half metallic  			= lerp(0, _Metallic, pbrMask.r);
					half smoothness 		= lerp(0, _Smoothness, pbrMask.g);
					half occlusion  		= lerp(1 - _Occlusion, 1, pbrMask.b);
					half directOcclusion  	= lerp(1 - _DirectOcclusion, 1, pbrMask.b);
					half3 albedo = mainTex.rgb * _BaseColor.rgb;

					// NPR diffuse
					float shadowArea = sigmoid(1 - halfLambert, _ShadowOffset, _ShadowSmooth * 10) * _ShadowStrength;
					half3 shadowRamp = lerp(1, _ShadowColor.rgb, shadowArea);
                    //Remap NdotL for PBR Spec
                    half NdotLRemap = 1 - shadowArea;
                #if _SHADOW_RAMP
                    float2 shadowRampUV = float2(1 - shadowArea, 0.125);
                    half4 shadowRampCol = SAMPLE_TEXTURE2D(_ShadowRampTex, sampler_ShadowRampTex, shadowRampUV);


                    shadowRamp = shadowRampCol.rgb;
                #endif
					
                    // NdotV modify fresnel
					NdotV += _NdotVAdd;

					// CustomShadow
					shadowRamp.rgb = lerp(_SecShadowColor.rgb, shadowRamp.rgb, ilmAO);




				// Direct

					// Diffuse TODO: nose Specular + hair Specular
					// albedo.rgb = albedo.rgb * shadowRamp;


					float3 directDiffColor = albedo.rgb;



					float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
					float roughness           = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
					float roughnessSquare     = max(roughness * roughness, HALF_MIN);
					float3 F0 = lerp(0.04, albedo, metallic);

					float NDF = DistributionGGX(NdotH, roughnessSquare);
					float G = GeometrySmith(NdotLRemap, NdotV, pow(roughness + 1.0, 2.0) / 8.0);
					float3 F = fresnelSchlick(HdotV, F0);

                    
					// GGX specArea remap;
					NDF = NDF * imlSpecMask;


					float3 kSpec = F;
					// (1.0 - F) Diff too dark
					float3 kDiff = ((1.0 - F) * 0.5 + 0.5) * (1.0 - metallic);

					float3 nom = NDF * G * F;
					float3 denom = 4.0 * NdotV * NdotLRemap + 0.0001;
					float3 BRDFSpec = nom / denom;

					directDiffColor = kDiff * albedo;
					float3 directSpecColor = BRDFSpec * PI;


				// #if _DIRECT_BLINNPHONG
				// 	// Blinn-Phong
				// 	// smoothness to Blinn-Phong Specular "Gloss" from CalculateBlinnPhong in Lighting.hlsl
				// 	half blinnPhongSpec = pow(NdotH, exp2(10 * smoothness + 1));

				// 	directSpecColor = F0 * blinnPhongSpec * _BlinnPhongSpecStrength * imlSpecMask;
				// #endif /* _DIRECT_BLINNPHONG */

                #if _SHADOW_RAMP
                    half blinnPhongSpec = pow(NdotH, exp2(7 * smoothness + 1));
                    float2 specRampUV = float2(saturate(NDF * G / denom.x), 0.375);
                    half4 specRampCol = SAMPLE_TEXTURE2D(_ShadowRampTex, sampler_ShadowRampTex, specRampUV);
                    directSpecColor = clamp(specRampCol.rgb * 3 + BRDFSpec * PI / F, 0, 10) * F * shadowRamp;
                #endif

					// Compose direct lighting
					float3 directLightResult = (directDiffColor * shadowRamp + directSpecColor * NdotLRemap)
												* mainLight.color * mainLight.shadowAttenuation * directOcclusion;


				// Indirect
					// Diffuse
					float3 SHNormal = lerp(normalWS, float3(1,1,1), _IndirDiffUpDirSH);
					float3 SHColor = SampleSH(SHNormal);
                    float3 envColor = lerp(SHColor, _SelfEnvColor, _EnvColorLerp);
					
					float3 indirKs = fresnelSchlickIndirect(NdotV, F0, roughness);
					float3 indirKd = (1 - indirKs) * (1 - metallic);
					float3 indirDiffColor = envColor * indirKd * albedo * occlusion;


					// Specular
					float3 indirSpecCubeColor = IndirSpeCube(normalWS, viewDirWS, roughness, occlusion);
					float3 indirSpecCubeFactor = IndirSpeFactor(roughness, smoothness, BRDFSpec, F0, NdotV);
					half3 additionalIndirSpec = 0;
				#if _INDIR_CUBEMAP // Additional cubemap
					float3 reflectDirWS = reflect(-viewDirWS, normalWS);
					roughness = roughness * (1.7 - 0.7 * roughness);
					float mipLevel= roughness * 6;
					additionalIndirSpec = SAMPLE_TEXTURECUBE_LOD(_IndirSpecCubemap, sampler_LinearRepeat, reflectDirWS, mipLevel);
				#elif _INDIR_MATCAP // Additional matcap
					float3 normalVS = TransformWorldToViewNormal(normalWS);
					normalVS = SafeNormalize(normalVS);
					float2 matcapUV = (normalVS.xy * _IndirSpecMatcapTile) * 0.5 + 0.5;
					additionalIndirSpec = SAMPLE_TEXTURE2D(_IndirSpecMatcap, sampler_IndirSpecMatcap, matcapUV);
				#endif /* _INDIR_CUBEMAP _INDIR_MATCAP */

					float3 indirSpecColor = lerp(indirSpecCubeColor, additionalIndirSpec, _IndirSpecLerp) * indirSpecCubeFactor;


					float3 indirectColor = indirDiffColor * _IndirDiffIntensity + indirSpecColor * _IndirSpecIntensity;


				//RimDir 
				// float4 Rim = float4(0,0,0,0); 
				// half Vec = 1-i.Ver.x;
				// float rim = 1.0 - NdotV;//法线与视线垂直的地方边缘光强度最强 
				//       rim = smoothstep(1-_RimWidth, 1, rim); 
				//       rim = smoothstep(0, _RimSmoothness, rim); 
				//       Rim = rim * _RimColor * _RimIntensity;
				// 	  Rim = Rim *(1-NdotL)*saturate(normalWS.y)*saturate(pow(Vec,2)/2) * Occlusion * shadow * smoothness;



				half3 lightingResult = directLightResult + indirectColor + emission * albedo * 2;

				return CharacterDataToGbuffer(albedo, lightingResult, normalWS, smoothness, indirectColor, occlusion);
            }
            ENDHLSL

        }
        
        // Outline
		UsePass "Character/Outline/GBufferOutline"

        // ShadowCaster
   		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off

			HLSLPROGRAM
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_SHADOWCASTER

			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float3 _LightDirection;

			VertexOutput VertexFunction( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

				float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

				#if UNITY_REVERSED_Z
					clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#else
					clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionHCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = clipPos;
				return o;
			}

			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif

			half4 frag(	VertexOutput IN 
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );
				
	
				return 0;
			}

			ENDHLSL
		}

        // DepthOnly
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.danbaidong/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
		
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				v.ase_normal = v.ase_normal;
				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionHCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionHCS = positionHCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = positionHCS;
				return o;
			}

			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}

			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif
			half4 frag(	VertexOutput IN 
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				return 0;
			}
			ENDHLSL
		}

        

    }

    CustomEditor "UnityEditor.DanbaidongGUI.DanbaidongGUI"
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}