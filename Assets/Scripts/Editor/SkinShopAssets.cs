using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MineArena.Buildings;
using MineArena.Cosmetics;
using MineArena.Items;
using MineArena.Platform;
using Devotion.SDK.Managers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class SkinShopBuilder
    {
        private const string Art = "Assets/Art/UI/Skins/";
        private const string BuildingPath = "Assets/ScriptableObjects/Configs/Buildings/SkinShopBuilding.asset";
        private const string ActivePlayerPath = "Assets/Scenes/PlayerV2.0.prefab";
        private const string ActiveBodyName = "SimplePlayer.Body.Layer1";
        private static readonly Vector3 Site = new Vector3(10, .49f, 9);

        [MenuItem("MineArena/Cosmetics/Build Skin Shop Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory("Assets/Prefabs/Buildings/SkinShop");
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>("Assets/Resources/UI/SkinCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SkinCatalog>();
                AssetDatabase.CreateAsset(catalog, "Assets/Resources/UI/SkinCatalog.asset");
                string[] files = Directory.GetFiles("Assets/Models", "skin_*.png").OrderBy(f => f, StringComparer.Ordinal).ToArray();
                var currency = AssetDatabase.LoadAssetAtPath<MineArena.UI.ShopCatalog>("Assets/Resources/UI/ShopCatalog.asset").Currency;
                catalog.Skins.Add(new CharacterSkin { Id = "default", DisplayName = "Путешественник", Source = SkinSource.Default, SourceDescription = "Твой первый образ. Доступен с начала игры." });
                string[] names = { "Странник", "Солнечный утёнок", "Каменный", "Алхимик", "Капитан", "Тень", "Пилот", "Лесной" };
                SkinSource[] sources = { SkinSource.Currency, SkinSource.Reward, SkinSource.Currency, SkinSource.Quest, SkinSource.Quest, SkinSource.IAP, SkinSource.Offer, SkinSource.Currency };
                for (int i = 0; i < files.Length; i++)
                {
                    string path = files[i].Replace('\\', '/');
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                    var source = sources[i % sources.Length];
                    catalog.Skins.Add(new CharacterSkin { Id = "skin_" + (i + 1).ToString("00"), DisplayName = names[i % names.Length],
                        Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path), Source = source, Currency = source == SkinSource.Currency ? currency : null,
                        Price = 20 + i * 10, QuestId = i == 3 ? 0 : 1, RewardId = source == SkinSource.Reward ? "daily_cycle_complete" : "",
                        ProductId = source == SkinSource.IAP ? "skin_shadow" : source == SkinSource.Offer ? "pilot_style_bundle" : "",
                        SourceDescription = source switch {
                            SkinSource.Currency => "Постоянный образ за игровую валюту. После покупки можно менять бесплатно.",
                            SkinSource.Quest => i == 3 ? "Заверши и забери награду первого задания на добычу. Затем получи этот образ здесь." : "Заверши и забери награду второго задания на добычу. Затем получи этот образ здесь.",
                            SkinSource.Reward => "Награда за полный цикл ежедневных подарков.",
                            SkinSource.IAP => "Образ из премиальной коллекции. Приобретается отдельно.",
                            SkinSource.Offer => "Входит в набор «Стиль пилота» вместе с игровой валютой.", _ => "" }
                    });
                }
            }
            BuildPlayer(catalog);
            var building = BuildModel(catalog);
            var config = BuildConfig(building);
            catalog.Building = config; EditorUtility.SetDirty(catalog);
            var database = AssetDatabase.LoadAssetAtPath<BuildingsDatabase>("Assets/ScriptableObjects/Configs/Game/BuildingsDatabase.asset");
            Append(database, "allItems", config);
            BuildWindow(catalog);
            BuildProducts(catalog);
            BuildSite(config);
            AssetDatabase.SaveAssets();
            Validate(catalog);
            ValidateFlow(catalog);
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Append(Object target, string field, Object value)
        {
            var so = new SerializedObject(target); var array = so.FindProperty(field);
            for (int i = 0; i < array.arraySize; i++) if (array.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
            array.InsertArrayElementAtIndex(array.arraySize); array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPlayer(SkinCatalog catalog)
        {
            const string path = ActivePlayerPath;
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var body = player.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == ActiveBodyName);
                Set(player.GetComponent<PlayerSkinView>() ?? player.AddComponent<PlayerSkinView>(), "body", body);
                foreach (var skin in catalog.Skins)
                {
                    string previewPath = Art + skin.Id + ".png";
                    BakeSkin(body, skin.Texture, previewPath);
                    skin.Preview = Import(previewPath);
                }
                PrefabUtility.SaveAsPrefabAsset(player, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }

        private static Sprite Import(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void BakeSkin(SkinnedMeshRenderer body, Texture texture, string path)
        {
            var mesh = new Mesh(); body.BakeMesh(mesh);
            var utility = new PreviewRenderUtility();
            var material = new Material(Shader.Find("Hidden/MineArena/SkinPreview")) { mainTexture = texture != null ? texture : body.sharedMaterial.mainTexture };
            try
            {
                var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, -15, 0), Vector3.one) * body.transform.root.worldToLocalMatrix * body.localToWorldMatrix;
                var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);
                var normals=mesh.normals;
                for(int i=0;i<normals.Length;i++)normals[i]=matrix.MultiplyVector(normals[i]).normalized;
                mesh.normals=normals;
                mesh.vertices=vertices;mesh.RecalculateBounds();
                if(matrix.determinant<0)
                {
                    var triangles=mesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3){int a=triangles[i];triangles[i]=triangles[i+1];triangles[i+1]=a;}
                    mesh.triangles=triangles;
                }
                var bounds=mesh.bounds;
                utility.camera.orthographic = true; utility.camera.orthographicSize = bounds.size.y * .59f;
                utility.camera.transform.position = bounds.center + new Vector3(0, .15f, -10);
                utility.camera.transform.LookAt(bounds.center);
                utility.camera.nearClipPlane = .01f; utility.camera.farClipPlane = 100;
                utility.camera.clearFlags = CameraClearFlags.SolidColor; utility.camera.backgroundColor = new Color(.12f,.19f,.2f,0);
                utility.BeginStaticPreview(new Rect(0,0,384,448));
                utility.DrawMesh(mesh, Matrix4x4.identity, material, 0);
                utility.camera.Render(); var rendered = utility.EndStaticPreview();
                File.WriteAllBytes(path, rendered.EncodeToPNG()); Object.DestroyImmediate(rendered);
            }
            finally { utility.Cleanup(); Object.DestroyImmediate(mesh); Object.DestroyImmediate(material); }
        }

        private static Material Tint(string name, Color color, Material template)
        {
            string path = "Assets/Prefabs/Buildings/SkinShop/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(template); AssetDatabase.CreateAsset(material, path); }
            material.color = color; EditorUtility.SetDirty(material); return material;
        }
        private static GameObject Block(Transform root, string name, Vector3 position, Vector3 scale, Material material, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root,false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static GameObject BuildModel(SkinCatalog catalog)
        {
            var root = new GameObject("SkinShop");
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Materials/Wood/OakPlanks.mat");
            var log = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Materials/Wood/DarkLog.mat");
            var teal = Tint("TealCloth", new Color(.16f,.55f,.53f), wood);
            var cream = Tint("CreamCloth", new Color(1,.91f,.66f), wood);
            var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Materials/Stone/Cobblestone.mat");
            try
            {
                Block(root.transform,"Foundation",new Vector3(0,.12f,0),new Vector3(6,.24f,4.6f),stone);
                Block(root.transform,"EntranceStep",new Vector3(0,.08f,-2.6f),new Vector3(3.5f,.16f,.65f),stone);
                // One-metre blocks retain the same texture density as village buildings.
                for (int x=-2;x<=2;x++) for (int y=0;y<3;y++)
                    Block(root.transform,"Oak back wall",new Vector3(x,y+.74f,1.8f),new Vector3(1,1,.35f),wood);
                foreach (int x in new[]{-1,1})
                {
                    for (int z=-1;z<=1;z++) for (int y=0;y<3;y++)
                        Block(root.transform,"Oak side wall",new Vector3(x*2.5f,y+.74f,z),new Vector3(.35f,1,1),wood);
                    foreach (float z in new[]{-1.85f,1.85f})
                        Block(root.transform,"Dark oak post",new Vector3(x*2.6f,1.8f,z),new Vector3(.42f,3.4f,.42f),log);
                }
                for(int tier=0;tier<3;tier++)
                    Block(root.transform,"Stepped roof",new Vector3(0,3.4f+tier*.35f,.1f),new Vector3(6.15f-tier*.8f,.38f,4.5f),teal);
                for(int x=-3;x<3;x++)
                    Block(root.transform,"Striped shop awning",new Vector3(x+.5f,3.03f,-2.35f),new Vector3(1,.25f,1.1f),x%2==0?teal:cream);
                Block(root.transform,"Sign board",new Vector3(0,3.68f,-2.2f),new Vector3(2.3f,.85f,.18f),log,false);
                // Pixel shirt emblem on the facade.
                Block(root.transform,"Shirt torso",new Vector3(0,3.66f,-2.32f),new Vector3(.48f,.52f,.1f),cream,false);
                Block(root.transform,"Shirt sleeves",new Vector3(0,3.84f,-2.32f),new Vector3(.9f,.23f,.1f),cream,false);
                Block(root.transform,"Shirt neck",new Vector3(0,3.94f,-2.38f),new Vector3(.18f,.12f,.04f),teal,false);
                foreach(int side in new[]{-1,1})
                {
                    Block(root.transform,"Display foundation",new Vector3(side*1.85f,.12f,-3.15f),new Vector3(1.35f,.24f,1.3f),stone);
                    Block(root.transform,"Display plinth",new Vector3(side*1.85f,.5f,-3.15f),new Vector3(1.15f,.6f,1.15f),log);
                    Mannequin(root.transform, new Vector3(side*1.85f,.82f,-3.15f), side<0 ? catalog.Skins[1] : catalog.Skins[7]);
                }
                var prefab = PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Buildings/SkinShop/SkinShop.prefab");
                // Reuse the project's existing preview baker without rebuilding unrelated UI.
                typeof(GameUiBuilder).GetMethod("BakeBuilding",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{prefab,Art+"building.png"});
                Import(Art+"building.png");
                return prefab;
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void Mannequin(Transform root, Vector3 position, CharacterSkin skin)
        {
            var player=PrefabUtility.LoadPrefabContents(ActivePlayerPath);
            var source=player.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name==ActiveBodyName);
            var mesh=new Mesh();source.BakeMesh(mesh);
            var sourceMaterial=source.sharedMaterial;
            var matrix=player.transform.worldToLocalMatrix*source.localToWorldMatrix;
            var vertices=mesh.vertices;var normals=mesh.normals;var triangles=mesh.triangles;
            var bounds=new Bounds(matrix.MultiplyPoint3x4(vertices[0]),Vector3.zero);
            for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);bounds.Encapsulate(vertices[i]);}
            var origin=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-origin)*(1.8f/bounds.size.y);
            for(int i=0;i<normals.Length;i++)normals[i]=matrix.MultiplyVector(normals[i]).normalized;
            if(matrix.determinant<0)
                for(int i=0;i<triangles.Length;i+=3){int a=triangles[i];triangles[i]=triangles[i+1];triangles[i+1]=a;}
            string path = "Assets/Prefabs/Buildings/SkinShop/Mannequin.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){saved=new Mesh();AssetDatabase.CreateAsset(saved,path);}
            saved.Clear();saved.name="Mannequin";saved.vertices=vertices;saved.normals=normals;saved.uv=mesh.uv;saved.triangles=triangles;saved.RecalculateBounds();EditorUtility.SetDirty(saved);
            Object.DestroyImmediate(mesh);PrefabUtility.UnloadPrefabContents(player);
            var go = new GameObject("Display " + skin.DisplayName,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(root,false);
            go.transform.localPosition=position; go.transform.localRotation=Quaternion.identity;
            go.GetComponent<MeshFilter>().sharedMesh=saved;
            string matPath="Assets/Prefabs/Buildings/SkinShop/"+skin.Id+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(sourceMaterial);AssetDatabase.CreateAsset(material,matPath);}
            material.mainTexture=skin.Texture; EditorUtility.SetDirty(material); go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        private static BuildingConfig BuildConfig(GameObject model)
        {
            var config=AssetDatabase.LoadAssetAtPath<BuildingConfig>(BuildingPath);
            if(config==null){config=ScriptableObject.CreateInstance<BuildingConfig>();AssetDatabase.CreateAsset(config,BuildingPath);}
            var so=new SerializedObject(config);so.FindProperty("_buildingName").stringValue="Магазин скинов";
            so.FindProperty("_currentLevel").intValue=0;so.FindProperty("_buildingRotation").quaternionValue=Quaternion.identity;
            var levels=so.FindProperty("_levels");levels.arraySize=1;var level=levels.GetArrayElementAtIndex(0);
            level.FindPropertyRelative("_level").intValue=1;level.FindPropertyRelative("_modelPrefab").objectReferenceValue=model;
            level.FindPropertyRelative("_preview").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"building.png");
            var costs=level.FindPropertyRelative("_requiredResources");costs.arraySize=2;
            string[] resources={"Planks","Stone"};
            for(int i=0;i<2;i++){var cost=costs.GetArrayElementAtIndex(i);cost.FindPropertyRelative("_resource").objectReferenceValue=AssetDatabase.LoadAssetAtPath<StackableItemConfig>("Assets/ScriptableObjects/Configs/Drops/"+resources[i]+".asset");cost.FindPropertyRelative("_amount").intValue=i==0?24:16;}
            level.FindPropertyRelative("_unlocks").arraySize=0;level.FindPropertyRelative("_expeditionRewardBonusPercent").intValue=0;
            so.ApplyModifiedPropertiesWithoutUndo();return config;
        }

        private static void BuildProducts(SkinCatalog catalog)
        {
            var products=AssetDatabase.LoadAssetAtPath<YandexPurchaseCatalog>("Assets/Resources/UI/YandexPurchaseCatalog.asset");
            foreach(var skin in catalog.Skins.Where(s=>s.Source==SkinSource.IAP||s.Source==SkinSource.Offer))
            {
                if(products.Products.Any(p=>p.ProductId==skin.ProductId))continue;
                var product=new YandexProduct{ProductId=skin.ProductId,SkinIds=new System.Collections.Generic.List<string>{skin.Id}};
                if(skin.Source==SkinSource.Offer){product.Item=AssetDatabase.LoadAssetAtPath<StackableItemConfig>("Assets/ScriptableObjects/Configs/Drops/GoldOre.asset");product.Amount=30;}
                products.Products.Add(product);
            }
            EditorUtility.SetDirty(products);
        }
    }
}
