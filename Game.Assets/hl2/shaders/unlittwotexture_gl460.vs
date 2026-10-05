#version 460
//	DYNAMIC: "COMPRESSED_VERTS"		"0..1"
//	DYNAMIC: "DOWATERFOG"			"0..1"
//	DYNAMIC: "SKINNING"				"0..1"

layout(location = 0) in vec3 v_Position;
layout(location = 1) in vec3 v_Normal;
layout(location = 7) in ivec4 v_BoneIndex;
layout(location = 8) in vec2 v_BoneWeights;
layout(location = 10) in vec4 v_TexCoord0;

layout(std140, binding = 0) uniform source_matrices {
    mat4 viewMatrix;
    mat4 projectionMatrix;
    mat4 modelMatrix;
};

layout(std140, binding = 2) uniform source_vertex_sharedUBO {
    int numBones;
    int lightCount;
    int vertexSharedPad0;
    int vertexSharedPad1;
    vec4 lightEnabled;
};

layout(std140, binding = 4) uniform source_bone_matrices {
    mat4 bones[256];
};

layout(std140, binding = 5) uniform source_vs_constants {
    vec4 vs_const[256];
};

const int VERTEX_SHADER_CAMERA_POS = 2;
const int VERTEX_SHADER_AMBIENT_LIGHT = 21;
const int VERTEX_SHADER_LIGHT_INFO = 27;
const int SHADER_SPECIFIC_CONST_0 = 48;
const int SHADER_SPECIFIC_CONST_2 = 50;

#include "common_gl460.vs"

const bool g_bSkinning	= SKINNING != 0;
const int  g_FogType	= DOWATERFOG;

#define cBaseTexCoordTransform0		vs_const[SHADER_SPECIFIC_CONST_0 + 0]
#define cBaseTexCoordTransform1		vs_const[SHADER_SPECIFIC_CONST_0 + 1]
#define cBaseTexCoordTransform2_0	vs_const[SHADER_SPECIFIC_CONST_2 + 0]
#define cBaseTexCoordTransform2_1	vs_const[SHADER_SPECIFIC_CONST_2 + 1]
#define cModulationColor			vs_const[47]

out vec2 vs_BaseTexCoord;
out vec2 vs_BaseTexCoord2;
out vec4 vs_WorldPos_ProjPosZ;
out vec4 vs_Color;
out vec4 vs_FogFactorW;

void main()
{
    vec4 vPosition = vec4(v_Position, 1.0);

    vec3 worldNormal, worldPos;
    SkinPositionAndNormal(
        g_bSkinning,
        vPosition, v_Normal,
        v_BoneIndex, v_BoneWeights,
        worldPos, worldNormal);

    vec4 vProjPos = projectionMatrix * viewMatrix * vec4(worldPos, 1.0);
    gl_Position = vProjPos;
    vs_FogFactorW.w = CalcFog(worldPos, vProjPos.xyz, g_FogType);

    vs_WorldPos_ProjPosZ = vec4(worldPos.xyz, vProjPos.z);

    vs_BaseTexCoord.x = dot(v_TexCoord0, cBaseTexCoordTransform0);
    vs_BaseTexCoord.y = dot(v_TexCoord0, cBaseTexCoordTransform1);

    vs_BaseTexCoord2.x = dot(v_TexCoord0, cBaseTexCoordTransform2_0);
    vs_BaseTexCoord2.y = dot(v_TexCoord0, cBaseTexCoordTransform2_1);

    vs_Color = cModulationColor;
}
