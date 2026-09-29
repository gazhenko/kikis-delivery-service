using UnityEditor;
using UnityEngine;

namespace Koriko.Editor
{
    public sealed class ArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Koriko/Art/"))return;
            var importer=(TextureImporter)assetImporter;
            bool surface=assetPath.EndsWith("PaintedSurfaces.png")||assetPath.EndsWith("PaintedFilmSurfaces.png")||assetPath.EndsWith("EnvironmentSurfaces-v2.png")||assetPath.EndsWith("ShopPaintings.png");
            bool sky=assetPath.EndsWith("PaintedSky.png");
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;importer.maxTextureSize=2048;
            importer.mipmapEnabled=surface||sky;importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;importer.anisoLevel=surface?4:1;
            if(sky)importer.wrapModeU=TextureWrapMode.Repeat;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=!surface&&!sky;
        }
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/Koriko/Art/"))return;
            var importer=(ModelImporter)assetImporter;
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.importNormals=ModelImporterNormals.Import;
            importer.importBlendShapes=true;
            importer.importTangents=ModelImporterTangents.None;
            importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.isReadable=false;importer.addCollider=false;
        }
    }
}
