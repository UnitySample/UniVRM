using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UniGLTF
{
    public static class ScriptedImporterImpl
    {
        /// <summary>
        /// glb をパースして、UnityObject化、さらにAsset化する
        /// </summary>
        /// <param name="scriptedImporter"></param>
        /// <param name="context"></param>
        /// <param name="reverseAxis"></param>
        public static void Import(ScriptedImporter scriptedImporter, AssetImportContext context, Axises reverseAxis)
        {
#if VRM_DEVELOP            
            Debug.Log("OnImportAsset to " + context.assetPath);
#endif

            var loaded = Load(context.assetPath,
                scriptedImporter.GetExternalObjectMap().Select(kv => (kv.Key.name, kv.Value)),
                reverseAxis
            );

            AddSubAssets(context, loaded);
        }

        /// <summary>
        /// Parse して、UnityObject 化する。
        /// 
        /// TODO: すべての UnityObject を ImporterContext が所有する。(Disposeで削除できる)
        /// </summary>
        /// <param name="assetPath">glbのパス</param>
        /// <param name="externalObjectMap">ScriptedImporter外部に作成済みのAssetへの参照</param>
        /// <param name="reverseAxis">gltf から unityへの座標変換オプション</param>
        /// <returns></returns>
        static ImporterContext Load(string assetPath, IEnumerable<(string, UnityEngine.Object)> externalObjectMap, Axises reverseAxis)
        {
            //
            // Parse(parse glb, parser gltf json)
            //
            var parser = new GltfParser();
            parser.ParsePath(assetPath);

            //
            // Import(create unity objects)
            //
            var context = new ImporterContext(parser, null, externalObjectMap);
            context.InvertAxis = reverseAxis;
            context.Load();
            context.ShowMeshes();

            return context;
        }

        /// <summary>
        /// UnityObjectをSubAsset化する。
        /// 
        /// TODO: SubAsset化されると、ImporterContext から所有権を除去する
        /// </summary>
        /// <param name="context"></param>
        /// <param name="loaded"></param>
        static void AddSubAssets(AssetImportContext context, ImporterContext loaded)
        {
            // Texture
            foreach (var info in loaded.TextureFactory.Textures)
            {
                if (info.IsSubAsset)
                {
                    var texture = info.Texture;
                    context.AddObjectToAsset(texture.name, texture);
                }
            }

            // Material
            foreach (var info in loaded.MaterialFactory.Materials)
            {
                if (info.IsSubAsset)
                {
                    var material = info.Asset;
                    context.AddObjectToAsset(material.name, material);
                }
            }

            // Mesh
            foreach (var mesh in loaded.Meshes.Select(x => x.Mesh))
            {
                // all mesh is subasset
                context.AddObjectToAsset(mesh.name, mesh);
            }

            // Animation
            foreach (var clip in loaded.AnimationClips)
            {
                // all animation is subasset
                context.AddObjectToAsset(clip.name, clip);
            }

            // Root GameObject is main object
            context.AddObjectToAsset(loaded.Root.name, loaded.Root);
            context.SetMainObject(loaded.Root);
        }
    }
}
