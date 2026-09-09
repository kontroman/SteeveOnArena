using System;
using System.Collections.Generic;
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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class SkinShopBuilder
    {
        private static void ValidateFlow(SkinCatalog catalog)
        {
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var scene=EditorSceneManager.NewPreviewScene();var log=new StringBuilder();
            var previousRoot=GameRoot.Instance;
            void Check(bool condition,string message){if(!condition)throw new Exception(message);log.AppendLine("PASS "+message);}
            try
            {
                var go=new GameObject("Skin test fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                var root=go.AddComponent<GameRoot>();typeof(GameRoot).GetProperty("Instance").SetValue(null,root);
                var progress=new PlayerProgress("skin-flow-fixture");progress.TutorialProgress.Initialized=true;
                var gameConfig=AssetDatabase.LoadAssetAtPath<MineArena.Structs.GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                typeof(GameRoot).GetField("gameConfig",flags).SetValue(root,gameConfig);typeof(GameRoot).GetField("playerProgress",flags).SetValue(root,progress);
                var inventory=go.AddComponent<InventoryManager>();
                var managers=(Dictionary<Type,BaseManager>)typeof(GameRoot).GetField("_managers",flags).GetValue(root);managers[typeof(InventoryManager)]=inventory;
                inventory.InitManager();
                var coinSkin=catalog.Skins.First(s=>s.Source==SkinSource.Currency);
                Check(!SkinService.Buy(coinSkin.Id) && !SkinService.Owns(coinSkin.Id),"Insufficient funds do not unlock or charge");
                inventory.AddItemById(coinSkin.Currency.Name,coinSkin.Price+5);
                Check(SkinService.Buy(coinSkin.Id) && inventory.GetItemAmount(coinSkin.Currency.Name)==5,"Currency purchase charges exactly once and unlocks");
                Check(!SkinService.Buy(coinSkin.Id) && inventory.GetItemAmount(coinSkin.Currency.Name)==5,"Repeat purchase does not charge");
                var premium=catalog.Skins.First(s=>s.Source==SkinSource.IAP);
                Check(!SkinService.Buy(premium.Id) && !SkinService.Equip(premium.Id),"Premium skins cannot be bought with currency or equipped while locked");
                var questSkin=catalog.Skins.First(s=>s.Source==SkinSource.Quest);
                Check(!SkinService.ClaimQuest(questSkin.Id),"Incomplete quest cannot grant a skin");
                progress.AchievementProgress.Achievements[questSkin.QuestId]=new AchievementSaveData(questSkin.QuestId,100,false,true);
                Check(!SkinService.ClaimQuest(questSkin.Id),"Unclaimed quest reward cannot grant a skin");
                progress.AchievementProgress.Achievements[questSkin.QuestId].IsCompleted=true;
                SkinService.GrantQuestRewards(questSkin.QuestId);
                Check(SkinService.Owns(questSkin.Id) && !SkinService.ClaimQuest(questSkin.Id),"Completed quest grants once");
                Check(!SkinService.GrantReward("unknown_reward"),"Unknown reward cannot unlock a skin");
                Check(SkinService.GrantReward("daily_cycle_complete") && !SkinService.GrantReward("daily_cycle_complete"),"Daily cycle reward grants once");
                Check(SkinService.Equip(coinSkin.Id),"Purchased skin can be equipped");
                var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ActivePlayerPath),scene);
                var view=player.GetComponent<PlayerSkinView>();var body=player.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name==ActiveBodyName);
                var material=body.sharedMaterial;typeof(PlayerSkinView).GetMethod("Awake",flags).Invoke(view,null);view.Apply(coinSkin.Id);
                var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);
                Check(block.GetTexture("_MainTex")==coinSkin.Texture && body.sharedMaterial==material,"Skin changes only the body texture without mutating shared materials");
                view.Apply("default");body.GetPropertyBlock(block);Check(block.GetTexture("_MainTex")==material.mainTexture,"Default appearance can be restored");
                var roundTrip=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress));
                Check(roundTrip.CosmeticsProgress.Equipped==coinSkin.Id && roundTrip.InventoryProgress.SavedResources[coinSkin.Currency.Name]==5,"Purchase balance and equipped appearance survive save/load together");
                var windowPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/SkinShopWindow.prefab");
                SkinShopWindow activeWindow=null;
                try
                {
                    GameUiBuilder.Render(windowPrefab,"Documentation/UI/SkinShopWindow.png",windowObject=>
                    {
                        activeWindow=windowObject.GetComponent<SkinShopWindow>();
                        typeof(SkinShopWindow).GetMethod("Awake",flags).Invoke(activeWindow,null);
                        typeof(SkinShopWindow).GetMethod("OnEnable",flags).Invoke(activeWindow,null);
                        var cards=(Transform)typeof(SkinShopWindow).GetField("cards",flags).GetValue(activeWindow);
                        Check(cards.childCount==catalog.Skins.Count,"Shop renders every catalog entry");
                        var action=(Button)typeof(SkinShopWindow).GetField("action",flags).GetValue(activeWindow);
                        Check(!action.interactable,"Equipped skin action is disabled");
                        typeof(SkinShopWindow).GetMethod("OnDisable",flags).Invoke(activeWindow,null);
                    });
                }
                finally { if(activeWindow!=null)typeof(SkinShopWindow).GetMethod("OnDisable",flags).Invoke(activeWindow,null); }
                var uiManager=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab").GetComponent<UIManager>();
                var windows=new SerializedObject(uiManager).FindProperty("_windows");
                Check(Enumerable.Range(0,windows.arraySize).Count(i=>windows.GetArrayElementAtIndex(i).objectReferenceValue is SkinShopWindow)==1,"Shop is registered exactly once in UIManager");
                ValidateSite(catalog,log);
                File.AppendAllText("Documentation/skin-shop-validation.txt",log.ToString());
            }
            finally
            {
                typeof(GameRoot).GetProperty("Instance").SetValue(null,previousRoot);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void ValidateSite(SkinCatalog catalog,StringBuilder log)
        {
            const string copy="Assets/SkinShopSceneValidation.unity";
            File.Copy("Assets/Scenes/SampleScene.unity",copy,true);AssetDatabase.ImportAsset(copy);
            var scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Additive);
            GameObject model=null;
            try
            {
                var zones=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BuildingZone>(true)).Where(z=>z.Config==catalog.Building).ToArray();
                if(zones.Length!=1 || Vector3.Distance(zones[0].transform.position,Site)>.01f)throw new Exception("Saved scene has missing/duplicate/misplaced shop site");
                log.AppendLine("PASS Saved scene loads with exactly one shop construction site at "+Site);
                var activePlayer=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MineArena.Controllers.Player>(true)).First(p=>p.gameObject.activeInHierarchy);
                if(activePlayer.GetComponent<PlayerSkinView>()==null)throw new Exception("Active scene player has no skin view");
                log.AppendLine("PASS Active scene player has the cosmetic renderer binding");
                var ground=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>(true)).First(c=>c.name=="BigSpawn");
                foreach(var offset in new[]{Vector3.zero,new Vector3(-3,0,-3.8f),new Vector3(3,0,-3.8f),new Vector3(-3,0,2.3f),new Vector3(3,0,2.3f),new Vector3(0,0,-4.2f)})
                {
                    var ray=new Ray(Site+offset+Vector3.up*20,Vector3.down);
                    if(!ground.Raycast(ray,out var hit,30)||Mathf.Abs(hit.point.y-Site.y)>.035f)throw new Exception("Shop site ground mismatch at "+offset+" ground="+hit.point);
                }
                log.AppendLine("PASS Building footprint and player construction point are on level ground");
                model=(GameObject)PrefabUtility.InstantiatePrefab(catalog.Building.GetCurrentLevel().ModelPrefab,scene);model.transform.position=Site;
                var bounds=new Bounds(Site+new Vector3(0,2,-.75f),new Vector3(6,3.9f,6.1f));
                foreach(var collider in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>(true)))
                {
                    if(collider==ground||collider.isTrigger||collider.transform.IsChildOf(model.transform)||collider.transform.IsChildOf(zones[0].transform))continue;
                    if(collider.bounds.Intersects(bounds))throw new Exception("Shop intersects "+collider.name+" "+collider.bounds);
                }
                log.AppendLine("PASS Shop footprint does not intersect other solid colliders");
                var sign=(GameObject)new SerializedObject(zones[0]).FindProperty("signObject").objectReferenceValue;sign.SetActive(false);
                RenderSite(scene);
            }
            finally{if(model!=null)Object.DestroyImmediate(model);EditorSceneManager.CloseScene(scene,true);AssetDatabase.DeleteAsset(copy);}
        }

        private static void RenderSite(UnityEngine.SceneManagement.Scene scene)
        {
            var go=new GameObject("Skin shop preview camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
            var camera=go.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.8f;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
                foreach(var light in root.GetComponentsInChildren<Light>(true))light.enabled=false;
            }
            camera.cullingMask=1<<31;
            camera.transform.position=Site+new Vector3(9,7,-12);camera.transform.LookAt(Site+new Vector3(0,1.6f,-.7f));
            camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            var rt=new RenderTexture(1280,900,24);var old=RenderTexture.active;var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();File.WriteAllBytes("Documentation/UI/SkinShop-building.png",texture.EncodeToPNG());}
            finally{RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
        }
    }
}
