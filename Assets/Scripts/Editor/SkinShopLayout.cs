using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Buildings;
using MineArena.Cosmetics;
using MineArena.Managers;
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
        private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name,typeof(RectTransform)); var rect=go.GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        private static Image Panel(string name,Transform parent,float x,float y,float w,float h,string sprite="craft_panel")
        {
            var rect=Rect(name,parent,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();
            string kind=sprite=="craft_panel_inset"?"inset":sprite=="craft_button_green"?"button":sprite=="craft_button_selected"?"selected":name=="SkinCard"?"card":"panel";
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Expedition/beige-"+kind+".png");image.type=Image.Type.Sliced;return image;
        }
        private static TextMeshProUGUI Label(string name,Transform parent,string text,float x,float y,float w,float h,int size=24)
        {
            var rect=Rect(name,parent,x,y,w,h);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset") ?? TMP_Settings.defaultFontAsset;
            label.text=text;label.fontSize=size;label.color=new Color32(72,62,47,255);label.raycastTarget=false;
            label.enableWordWrapping=true;label.overflowMode=TextOverflowModes.Ellipsis;return label;
        }
        private static Button Button(string name,Transform parent,string text,float x,float y,float w,float h)
        {
            var panel=Panel(name,parent,x,y,w,h,"craft_button_green");var button=panel.gameObject.AddComponent<Button>();
            button.targetGraphic=panel;var label=Label("Label",panel.transform,text,8,4,w-16,h-8,22);label.alignment=TextAlignmentOptions.Center;return button;
        }
        private static void BuildWindow(SkinCatalog catalog)
        {
            var card=Panel("SkinCard",null,0,0,190,224);var cardButton=card.gameObject.AddComponent<Button>();
            var portrait=Panel("Portrait",card.transform,15,10,160,154);portrait.sprite=catalog.Skins[0].Preview;portrait.type=Image.Type.Simple;portrait.preserveAspect=true;portrait.raycastTarget=false;
            var title=Label("Name",card.transform,"Путешественник\nБазовый",8,168,174,50,19);title.alignment=TextAlignmentOptions.Center;
            title.enableAutoSizing=true;title.fontSizeMin=14;title.fontSizeMax=19;
            var cardAsset=PrefabUtility.SaveAsPrefabAsset(card.gameObject,"Assets/Resources/UI/SkinCard.prefab").GetComponent<Button>();Object.DestroyImmediate(card.gameObject);
            var root=new GameObject("SkinShopWindow",typeof(RectTransform),typeof(Image));
            try
            {
                root.GetComponent<Image>().color=new Color(0,0,0,.65f);
                var rr=root.GetComponent<RectTransform>();rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.offsetMin=rr.offsetMax=Vector2.zero;
                var frame=Panel("Frame",root.transform,0,0,1240,800);
                var fr=frame.rectTransform;fr.anchorMin=fr.anchorMax=fr.pivot=new Vector2(.5f,.5f);fr.anchoredPosition=Vector2.zero;
                var ribbon=Panel("Header",fr,18,18,1204,90,"craft_button_selected");
                ribbon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Expedition/beige-button.png");ribbon.color=new Color32(110,169,164,255);
                Label("Title",ribbon.transform,"МАГАЗИН СКИНОВ",26,12,850,44,34).color=new Color32(255,250,233,255);
                Label("Subtitle",ribbon.transform,"Собирай образы · меняй стиль",28,55,820,30,20).color=new Color32(255,250,233,255);
                var close=Button("Close",fr,"×",1150,33,60,60);
                var wallet=Label("Balance",fr,"Коллекция: 1 / 9",30,120,640,40,22);
                var viewport=Panel("Viewport",fr,28,167,624,541,"craft_panel_inset");viewport.gameObject.AddComponent<RectMask2D>();
                var content=Rect("Cards",viewport.transform,12,12,600,708);
                var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(190,224);grid.spacing=new Vector2(9,12);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;
                var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
                var detail=Panel("Detail",fr,670,123,542,585,"craft_panel_inset");
                var name=Label("SkinName",detail.transform,catalog.Skins[0].DisplayName,22,16,498,44,29);
                var image=Panel("Preview",detail.transform,125,64,290,307);image.sprite=catalog.Skins[0].Preview;image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;
                var description=Label("Description",detail.transform,"Только внешний вид · без бонусов к характеристикам\n\n"+catalog.Skins[0].SourceDescription,24,382,494,178,21);
                var action=Button("Action",fr,"Надет",687,727,510,52);
                var restore=Button("Restore",fr,"Восстановить покупки",32,730,380,48);
                Label("Hint",fr,"Листай коллекцию ↓",37,712,570,27,18);
                var window=root.AddComponent<SkinShopWindow>();
                Set(root.AddComponent<MineArena.Windows.SelectLevel.LevelWindowFit>(),"panel",fr);
                root.AddComponent<MineArena.SDK.UI.LocalizedTextScope>();
                Set(window,"cards",content);Set(window,"cardPrefab",cardAsset);Set(window,"preview",image);Set(window,"skinName",name);
                Set(window,"description",description);Set(window,"balance",wallet);Set(window,"action",action);Set(window,"actionLabel",action.GetComponentInChildren<TMP_Text>());
                Set(window,"close",close);Set(window,"restore",restore);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/UI/SkinShopWindow.prefab");
                const string managerPath="Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab";
                var manager=PrefabUtility.LoadPrefabContents(managerPath);
                try{Append(manager.GetComponent<UIManager>(),"_windows",prefab.GetComponent<SkinShopWindow>());PrefabUtility.SaveAsPrefabAsset(manager,managerPath);}
                finally{PrefabUtility.UnloadPrefabContents(manager);}
            }
            finally{Object.DestroyImmediate(root);}
        }

        private static void BuildSite(BuildingConfig config)
        {
            var root=new GameObject("BuildingZoneSkinShop",typeof(BoxCollider),typeof(BuildingZone));
            try
            {
                root.transform.position=Site;
                var trigger=root.GetComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,1,-2.65f);trigger.size=new Vector3(2.8f,2,1.4f);
                var marker=new GameObject("ConstructionSign");marker.transform.SetParent(root.transform,false);
                var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Materials/Wood/OakPlanks.mat");
                Block(marker.transform,"Sign post",new Vector3(0,.8f,-2.15f),new Vector3(.16f,1.6f,.16f),wood,false);
                var board=Block(marker.transform,"Sign",new Vector3(0,1.65f,-2.15f),new Vector3(2.6f,.9f,.16f),wood,false);
                var label=new GameObject("Shop name",typeof(TextMeshPro));label.transform.SetParent(board.transform,false);
                label.transform.localPosition=new Vector3(0,0,-.6f);label.transform.localRotation=Quaternion.Euler(0,180,0);
                var text=label.GetComponent<TextMeshPro>();text.text="МАГАЗИН\nСКИНОВ";text.fontSize=3;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.2f,.14f,.09f);
                text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
                text.enableAutoSizing=true;text.fontSizeMin=.1f;text.fontSizeMax=1.4f;
                text.rectTransform.sizeDelta=new Vector2(.9f,.8f);
                var stand=new GameObject("PlayerPositionOnBuild");stand.transform.SetParent(root.transform,false);stand.transform.localPosition=new Vector3(0,0,-4.2f);
                Set(root.GetComponent<BuildingZone>(),"config",config);Set(root.GetComponent<BuildingZone>(),"signObject",marker);Set(root.GetComponent<BuildingZone>(),"playerPositionOnBuild",stand.transform);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Buildings/SkinShop/BuildingZoneSkinShop.prefab");
                AddSiteToScene(prefab);
            }
            finally{Object.DestroyImmediate(root);}
        }

        private static void AddSiteToScene(GameObject prefab)
        {
            const string path="Assets/Scenes/SampleScene.unity";
            string contents=File.ReadAllText(path);string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            if(!contents.Contains(guid))
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab.transform,out string _,out long transformId);
                const long instanceId=735860000001,rootId=735860000002;
                string block=$"--- !u!1001 &{instanceId}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {{fileID: 0}}\n    m_Modifications: []\n    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}\n--- !u!4 &{rootId} stripped\nTransform:\n  m_CorrespondingSourceObject: {{fileID: {transformId}, guid: {guid}, type: 3}}\n  m_PrefabInstance: {{fileID: {instanceId}}}\n  m_PrefabAsset: {{fileID: 0}}\n";
                const string roots="--- !u!1660057539 &9223372036854775807";
                contents=contents.Replace(roots,block+roots);
                contents+=$"  - {{fileID: {rootId}}}\n";
                File.WriteAllText(path,contents,new UTF8Encoding(false));
            }
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if(scene.isLoaded && !scene.GetRootGameObjects().Any(r=>r.name=="BuildingZoneSkinShop"))
            {
                // Keep unsaved editor changes: add the same instance without saving/reloading the user's scene.
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                Undo.RegisterCreatedObjectUndo(instance,"Add skin shop construction site");EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static void Validate(SkinCatalog catalog)
        {
            var log=new StringBuilder();void Check(bool ok,string message){if(!ok)throw new Exception(message);log.AppendLine("PASS "+message);}
            Check(catalog.Skins.Count>=7 && catalog.Skins.Select(s=>s.Id).Distinct().Count()==catalog.Skins.Count,"Unique catalog IDs and all acquisition sources");
            Check(catalog.Skins.All(s=>s.Preview!=null && (s.Id=="default" || s.Texture!=null)),"Every skin has a texture and rendered preview");
            var progress=new PlayerProgress("skin-fixture");
            Check(progress.CosmeticsProgress.Owns("default") && !progress.CosmeticsProgress.Equip("locked"),"Default skin available; locked skins cannot be equipped");
            Check(progress.CosmeticsProgress.Unlock("skin_01") && !progress.CosmeticsProgress.Unlock("skin_01"),"Duplicate grants are idempotent");
            Check(progress.CosmeticsProgress.Equip("skin_01"),"Owned skin can be equipped");
            var restored=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress));
            Check(restored.CosmeticsProgress.Equipped=="skin_01" && restored.CosmeticsProgress.Owns("skin_01"),"Ownership and equipped skin survive save round trip");
            var old=JsonUtility.FromJson<PlayerProgress>("{}");Check(old.CosmeticsProgress.Equipped=="default","Legacy saves initialize the default skin");
            Check(catalog.Building.GetCurrentLevel().ModelPrefab!=null && catalog.Building.GetCurrentLevel().ExpeditionRewardBonusPercent==0,"Building model assigned; no stat bonus");
            var products=Resources.Load<MineArena.Platform.YandexPurchaseCatalog>("UI/YandexPurchaseCatalog");
            Check(products.Products.All(p=>p.IsValid),"IAP and offer mappings resolve to valid rewards");
            log.AppendLine("Platform checkout requires product configuration and a platform sandbox test.");
            File.WriteAllText("Documentation/skin-shop-validation.txt",log.ToString());
        }
    }
}
