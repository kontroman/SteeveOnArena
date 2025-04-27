#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    [InitializeOnLoad]
    public class GgEditorCallbacks
    {
        #region Variables

        [Tooltip("Called when an open scene has been changed 'dirtied' without any more specific information available.")]
        public static event Action<GgEventArgs_SceneData> OnSceneUpdated;

        [Tooltip("Called when a GameObject has been created, possibly with additional objects below it in the hierarchy.")]
        public static event Action<GgEventArgs_GameObject> OnGameObjectCreated;

        [Tooltip("Called when a GameObject has been destroyed, including any parented to it in the hierarchy.")]
        public static event Action<GgEventArgs_GameObjectDestroyed> OnGameObjectDestroyed;

        [Tooltip("Called when the structure of a GameObject has changed and any GameObject in the hierarchy below it might have changed.")]
        public static event Action<GgEventArgs_GameObject> OnGameObjectHierarchyUpdated;

        [Tooltip("Called when the structure of a GameObject has changed.")]
        public static event Action<GgEventArgs_GameObject> OnGameObjectStructureUpdated;

        [Tooltip("Called when the parent or parent scene of a GameObject has changed.")]
        public static event Action<GgEventArgs_GameObjectChangedParent> OnGameObjectParentUpdated;

        [Tooltip("Called when the properties of a GameObject has changed.")]
        public static event Action<GgEventArgs_GameObject> OnGameObjectPropertiesUpdated;

        [Tooltip("Called when the properties of a Component has changed.")]
        public static event Action<GgEventArgs_Component> OnComponentPropertiesUpdated;

        [Tooltip("Called when an asset object has been created.")]
        public static event Action<GgEventArgs_AssetObject> OnAssetObjectCreated;

        [Tooltip("Called when an asset object has been destroyed.")]
        public static event Action<GgEventArgs_AssetObjectDestroyed> OnAssetObjectDestroyed;

        [Tooltip("Called when a property of an asset object in memory has changed.")]
        public static event Action<GgEventArgs_AssetObject> OnAssetObjectPropertiesUpdated;

        [Tooltip("Called when a prefab instance in an open scene has been updated due to a change to the source prefab.")]
        public static event Action<GgEventArgs_PrefabInstance> OnPrefabInstanceUpdated;

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Constructor

        static GgEditorCallbacks()
        {
            ObjectChangeEvents.changesPublished -= ObjectChangeEvents_ChangesPublished;
            ObjectChangeEvents.changesPublished += ObjectChangeEvents_ChangesPublished;
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private static void ObjectChangeEvents_ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                ObjectChangeKind type = stream.GetEventType(i);
                switch (type)
                {
                    case ObjectChangeKind.ChangeScene:
                        stream.GetChangeSceneEvent(i, out ChangeSceneEventArgs changeSceneEvent);
                        OnSceneUpdated?.Invoke(new GgEventArgs_SceneData(changeSceneEvent.scene));
                        //Debug.Log("OnSceneUpdated");
                        break;

                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs createGameObjectHierarchyEvent);
                        GameObject newGameObject = EditorUtility.InstanceIDToObject(createGameObjectHierarchyEvent.instanceId) as GameObject;
                        OnGameObjectCreated?.Invoke(new GgEventArgs_GameObject(newGameObject));
                        //Debug.Log("OnGameObjectCreated");
                        break;

                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                        stream.GetDestroyGameObjectHierarchyEvent(i, out DestroyGameObjectHierarchyEventArgs destroyGameObjectHierarchyEvent);
                        GameObject destroyParentGo = EditorUtility.InstanceIDToObject(destroyGameObjectHierarchyEvent.parentInstanceId) as GameObject;
                        OnGameObjectDestroyed?.Invoke(new GgEventArgs_GameObjectDestroyed(destroyGameObjectHierarchyEvent.instanceId, destroyParentGo));
                        //Debug.Log("OnGameObjectDestroyed");
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out ChangeGameObjectStructureHierarchyEventArgs changeGameObjectStructureHierarchy);
                        GameObject gameObject = EditorUtility.InstanceIDToObject(changeGameObjectStructureHierarchy.instanceId) as GameObject;
                        OnGameObjectHierarchyUpdated?.Invoke(new GgEventArgs_GameObject(gameObject));
                        //Debug.Log("OnGameObjectHierarchyUpdated");
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructure:
                        stream.GetChangeGameObjectStructureEvent(i, out ChangeGameObjectStructureEventArgs changeGameObjectStructure);
                        GameObject gameObjectStructure = EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) as GameObject;
                        OnGameObjectStructureUpdated?.Invoke(new GgEventArgs_GameObject(gameObjectStructure));
                        //Debug.Log("OnGameObjectStructureUpdated");
                        break;

                    case ObjectChangeKind.ChangeGameObjectParent:
                        stream.GetChangeGameObjectParentEvent(i, out ChangeGameObjectParentEventArgs changeGameObjectParent);
                        GameObject gameObjectChanged = EditorUtility.InstanceIDToObject(changeGameObjectParent.instanceId) as GameObject;
                        GameObject newParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.newParentInstanceId) as GameObject;
                        GameObject previousParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.previousParentInstanceId) as GameObject;
                        OnGameObjectParentUpdated?.Invoke(new GgEventArgs_GameObjectChangedParent(gameObjectChanged, previousParentGo, newParentGo, new SceneData(changeGameObjectParent.previousScene), new SceneData(changeGameObjectParent.newScene)));
                        //Debug.Log("OnGameObjectParentUpdated");
                        break;

                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out ChangeGameObjectOrComponentPropertiesEventArgs changeGameObjectOrComponent);
                        Object goOrComponent = EditorUtility.InstanceIDToObject(changeGameObjectOrComponent.instanceId);
                        switch (goOrComponent)
                        {
                            case GameObject go:
                                OnGameObjectPropertiesUpdated?.Invoke(new GgEventArgs_GameObject(go));
                                //Debug.Log("OnGameObjectPropertiesUpdated");
                                break;
                            case Component component:
                                OnComponentPropertiesUpdated?.Invoke(new GgEventArgs_Component(component));
                                //Debug.Log("OnGameObjectPropertiesUpdated");
                                break;
                        }
                        break;

                    case ObjectChangeKind.CreateAssetObject:
                        stream.GetCreateAssetObjectEvent(i, out CreateAssetObjectEventArgs createAssetObjectEvent);
                        Object createdAsset = EditorUtility.InstanceIDToObject(createAssetObjectEvent.instanceId);
                        string createdAssetPath = AssetDatabase.GUIDToAssetPath(createAssetObjectEvent.guid);
                        OnAssetObjectCreated?.Invoke(new GgEventArgs_AssetObject(createdAsset, createdAssetPath));
                        //Debug.Log("OnAssetObjectCreated");
                        break;

                    case ObjectChangeKind.DestroyAssetObject:
                        stream.GetDestroyAssetObjectEvent(i, out DestroyAssetObjectEventArgs destroyAssetObjectEvent);
                        OnAssetObjectDestroyed?.Invoke(new GgEventArgs_AssetObjectDestroyed(destroyAssetObjectEvent.instanceId, destroyAssetObjectEvent.guid));
                        //Debug.Log("OnAssetObjectDestroyed");
                        break;

                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        stream.GetChangeAssetObjectPropertiesEvent(i, out ChangeAssetObjectPropertiesEventArgs changeAssetObjectPropertiesEvent);
                        Object changeAsset = EditorUtility.InstanceIDToObject(changeAssetObjectPropertiesEvent.instanceId);
                        string changeAssetPath = AssetDatabase.GUIDToAssetPath(changeAssetObjectPropertiesEvent.guid);
                        OnAssetObjectPropertiesUpdated?.Invoke(new GgEventArgs_AssetObject(changeAsset, changeAssetPath));
                        //Debug.Log("OnAssetObjectPropertiesUpdated");
                        break;

                    case ObjectChangeKind.UpdatePrefabInstances:
                        stream.GetUpdatePrefabInstancesEvent(i, out UpdatePrefabInstancesEventArgs updatePrefabInstancesEvent);
                        foreach (int instanceID in updatePrefabInstancesEvent.instanceIds)
                        {
                            OnPrefabInstanceUpdated?.Invoke(new GgEventArgs_PrefabInstance(instanceID, new SceneData(updatePrefabInstancesEvent.scene)));
                        }
                        //Debug.Log("OnPrefabInstanceUpdated");
                        break;
                }
            }
        }
        
        #endregion
        
    } // class end
}

#endif