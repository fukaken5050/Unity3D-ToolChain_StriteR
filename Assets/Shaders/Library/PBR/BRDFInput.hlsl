﻿#define DIELETRIC_SPEC half4(0.04h,0.04h,0.04h,0.96h)
struct BRDFSurface
{
    half3 diffuse;
    half3 specular;
    half3 emission;
    
    half metallic;
    half ao;
    half grazingTerm;
    half perceptualRoughness;
    half smoothness;
    half roughness;
    half roughness2;
    half roughnessT;
    half roughnessB;
    
    half3 normal;
    half3 tangent;
    half3 biTangent;
    half3 viewDir;
    half3 reflectDir;
    half NDV;
};

BRDFSurface BRDFSurface_Ctor(half3 albedo,half3 emission, half smoothness, half metallic,half ao, half3 normal, half3 tangent,half3 biTangent, half3 viewDir,half anisotropic)
{
    BRDFSurface surface;
    
    half oneMinusReflectivity = DIELETRIC_SPEC.a - metallic * DIELETRIC_SPEC.a;
    half reflectivity = 1.0h - oneMinusReflectivity;

    surface.diffuse = albedo * oneMinusReflectivity;
    surface.emission = emission;
    surface.specular = lerp(DIELETRIC_SPEC.rgb, albedo, metallic);
    surface.metallic = metallic;
    surface.ao = ao;
    
    surface.normal = normal;
    surface.tangent = tangent;
    surface.biTangent = biTangent;
    surface.viewDir = viewDir;
    surface.reflectDir = normalize(reflect(-surface.viewDir, surface.normal));
    surface.NDV = dot(surface.normal,surface.viewDir);

    surface.smoothness = smoothness;
    surface.grazingTerm = saturate(smoothness + reflectivity);
    surface.perceptualRoughness = 1.0h - smoothness;
    surface.roughness = max(HALF_MIN_SQRT, surface.perceptualRoughness * surface.perceptualRoughness);
    surface.roughness2 = max(HALF_MIN, surface.roughness * surface.roughness);
    
    float anisotropicAspect = sqrt(1.0h - anisotropic * 0.9h);
    surface.roughnessT = max(.001, surface.roughness / anisotropicAspect) * 5;
    surface.roughnessB = max(.001, surface.roughness * anisotropicAspect) * 5;
    return surface;
}

struct BRDFLightInput
{
    half3 lightDirection;
    half3 lightColor;
    half shadowAttenuation;
    half distanceAttenuation;
    half NDL;
    half VDH;
    half LDV;
    half NDH;
    half LDH;
    half TDH;
    half BDH;
};

BRDFLightInput BRDFLightInput_Ctor(BRDFSurface surface,half3 lightDir,half3 lightColor,half shadowAttenuation,half distanceAttenuation)
{
    half3 viewDir = surface.viewDir;
    half3 normal = surface.normal;
    float3 halfDir = SafeNormalize(float3(viewDir) + float3(lightDir));

    BRDFLightInput input;
    input.lightDirection=lightDir;
    input.lightColor=lightColor;
    input.shadowAttenuation=shadowAttenuation;
    input.distanceAttenuation=distanceAttenuation;
    input.NDL = max(0,dot(normal, lightDir));
    input.VDH = dot(viewDir, halfDir);
    input.LDV = dot(lightDir, viewDir);
    input.NDH = dot(normal, halfDir);
    input.LDH = dot(lightDir, halfDir);
    input.TDH = dot(surface.tangent,halfDir);
    input.BDH = dot(surface.biTangent,halfDir);
    return input;
}

BRDFLightInput BRDFLightInput_Ctor(BRDFSurface surface,Light light)
{
    return BRDFLightInput_Ctor(surface,light.direction,light.color,light.shadowAttenuation,light.distanceAttenuation);
}