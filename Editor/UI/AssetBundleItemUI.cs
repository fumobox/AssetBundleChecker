using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UTJ
{
    public class AssetBundleItemUI : IDisposable
    {
        public delegate void OnDeleteAsset(AssetBundleItemUI itemUi);

        private Foldout advancedFold;

        private AssetBundle assetBundle;
        private readonly List<Object> assetBundleObjects;
        private readonly VisualElement element;
        private List<InstantiateGameObjectFromAb> instantiateObjects;
        private OnDeleteAsset onDeleteAsset;

        private readonly SerializedObject serializedObject;

        public AssetBundleItemUI(string abFilePath, VisualTreeAsset tree, OnDeleteAsset onDelete)
        {
            assetBundle = AssetBundle.LoadFromFile(abFilePath);
            if (assetBundle == null) return;
            serializedObject = new SerializedObject(assetBundle);
            element = tree.CloneTree();
            onDeleteAsset = onDelete;
            if (!IsStreamSceneAsset(serializedObject))
            {
                var allObjects = assetBundle.LoadAllAssets<Object>();
                assetBundleObjects = new List<Object>(allObjects);
            }
            else
            {
                assetBundleObjects = new List<Object>();
            }

            InitElement();
        }

        public void Dispose()
        {
            onDeleteAsset?.Invoke(this);
            if (instantiateObjects != null)
                foreach (var instantiateObj in instantiateObjects)
                    instantiateObj.Destroy();
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
                assetBundle = null;
            }
        }

        private bool IsStreamSceneAsset(SerializedObject obj)
        {
            var prop = obj.FindProperty("m_IsStreamedSceneAssetBundle");
            if (prop == null)
            {
                Debug.LogError("m_IsStreamedSceneAssetBundle is null");
                return false;
            }
            return prop.boolValue;
        }

        public bool Validate()
        {
            return assetBundle != null;
        }

        private void InitElement()
        {
            if (!string.IsNullOrEmpty(assetBundle.name)) element.Q<Foldout>("AssetBundleItem").text = assetBundle.name;
            element.Q<Foldout>("AssetBundleItem").value = false;

            var loadObjectBody = element.Q<VisualElement>("LoadObjectBody");
            foreach (var abObject in assetBundleObjects)
            {
                var field = new ObjectField(abObject.name)
                {
                    allowSceneObjects = true
                };
                loadObjectBody.Add(field);
                field.objectType = abObject.GetType();
                field.value = abObject;
            }

            // instantiate...
            var instantiateBody = element.Q<VisualElement>("MaterialChangeBody");
            instantiateObjects = new List<InstantiateGameObjectFromAb>();
            foreach (var abObject in assetBundleObjects)
            {
                var prefab = abObject as GameObject;
                if (prefab == null) continue;
                var instantiateObject = new InstantiateGameObjectFromAb(prefab);
                instantiateObjects.Add(instantiateObject);

                var instanceUI = new InstantiateGameObjectUI(instantiateObject);
                instanceUI.AddToParent(instantiateBody);
            }

            // advanced
            advancedFold = element.Q<Foldout>("Advanced");
            var advancedBody = new IMGUIContainer(OnAdvancedGUI);
            advancedFold.Add(advancedBody);

            // Close Btn

            element.Q<Button>("CloseBtn").clickable.clicked += OnClickClose;
        }

        private void OnClickClose()
        {
            RemoveFormParent();
            Dispose();
        }

        private void OnAdvancedGUI()
        {
            if (!advancedFold.value) return;
            DoDrawDefaultInspector(serializedObject);
        }

        private static bool DoDrawDefaultInspector(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.Update();

            // Loop through properties and create one field (including children) for each top level property.
            var property = obj.GetIterator();
            var expanded = true;
            while (property.NextVisible(expanded))
            {
                using (new EditorGUI.DisabledScope("m_Script" == property.propertyPath))
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                expanded = false;
            }

            obj.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }

        public void AddToElement(VisualElement parent)
        {
            if (element != null) parent.Add(element);
        }

        public void CollectAbObjectToList<T>(List<T> items) where T : class
        {
            var preloadTable = serializedObject.FindProperty("m_PreloadTable");
            //var preloadInstancies = preloadTable.serializedObject.context;

            for (var i = 0; i < preloadTable.arraySize; ++i)
            {
                var elementProp = preloadTable.GetArrayElementAtIndex(i);
                if (elementProp.objectReferenceValue is T item && !items.Contains(item)) items.Add(item);
            }

            foreach (var abItem in assetBundleObjects)
            {
                if (abItem is T item && !items.Contains(item)) items.Add(item);
            }
        }

        public void RemoveFormParent()
        {
            element.parent?.Remove(element);
        }

        public void DisposeFromOnDisable()
        {
            onDeleteAsset = null;
            Dispose();
        }
    }
}