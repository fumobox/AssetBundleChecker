using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UTJ
{
    public class AssetBundleCheckerWindow : EditorWindow
    {
        private VisualTreeAsset assetBundleTreeAsset;
        private ScrollView assetBunleItemBody;

        private VisualElement bodyElement;
        private IEnumerator dumpExecute;
        private Toggle isResolveDepencies;

        private Button loadAbButton;

        private List<AssetBundleItemUI> loadAbItemUIs = new();
        private readonly Dictionary<AssetBundleItemUI, List<ShaderItemUI>> loadShaderItems = new();
        private readonly Dictionary<AssetBundleItemUI, List<ShaderVariantInfoUI>> loadVariantItems = new();
        private string openDateStr;
        private ScrollView shaderItemBody;
        private VisualTreeAsset shaderTreeAsset;
        private ScrollView shaderVariantsItemBody;

        private void Update()
        {
            if (dumpExecute != null)
                if (!dumpExecute.MoveNext())
                    dumpExecute = null;
        }


        private void OnEnable()
        {
            var windowLayoutPath = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleChecker.uxml";
            var now = DateTime.Now;
            openDateStr = now.ToString("yyyyMMdd_HHmmss");

            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(windowLayoutPath);
            var visualElement = CloneTree(tree);
            rootVisualElement.Add(visualElement);

            InitHeader();

            bodyElement = rootVisualElement.Q<VisualElement>("BodyItems");
            InitAssetBundleItems();
            InitShaderItems();
            InitShaderVariants();

            SetAssetFileMode();
        }

        private void OnDisable()
        {
            if (loadAbItemUIs != null)
                foreach (var abItem in loadAbItemUIs)
                    abItem.DisposeFromOnDisable();
            loadAbItemUIs = null;
        }

        [MenuItem("Tools/UTJ/AssetBundleCheck")]
        public static void Create()
        {
            GetWindow<AssetBundleCheckerWindow>();
        }

        private void InitHeader()
        {
            // load btn
            loadAbButton = rootVisualElement.Q<Button>("LoadAssetBundle");
            loadAbButton.clickable.clicked += SelectAssetBundleFile;
            // dump all
            var dumpAllBtn = rootVisualElement.Q<Button>("DumpAllShader");
            dumpAllBtn.clickable.clicked += DumpAllShader;
            //clear all
            var unloadAllBtn = rootVisualElement.Q<Button>("UnloadAll");
            unloadAllBtn.clickable.clicked += UnloadAll;
            //
            isResolveDepencies = rootVisualElement.Q<Toggle>("AutoLoadDependencies");
            isResolveDepencies.value = true;
            //
            var headerToolbar = rootVisualElement.Q<VisualElement>("Header");
            headerToolbar.Q<ToolbarButton>("Assets").clickable.clicked += SetAssetFileMode;
            headerToolbar.Q<ToolbarButton>("Shaders").clickable.clicked += SetShaderMode;
            headerToolbar.Q<ToolbarButton>("ShaderVariants").clickable.clicked += SetShaderVariantMode;
        }

        private void SetAssetFileMode()
        {
            SetVisibility(assetBunleItemBody, true);
            SetVisibility(shaderItemBody, false);
            SetVisibility(shaderVariantsItemBody, false);
        }

        private void SetShaderMode()
        {
            SetVisibility(assetBunleItemBody, false);
            SetVisibility(shaderItemBody, true);
            SetVisibility(shaderVariantsItemBody, false);
        }

        private void SetShaderVariantMode()
        {
            SetVisibility(assetBunleItemBody, false);
            SetVisibility(shaderItemBody, false);
            SetVisibility(shaderVariantsItemBody, true);
        }

        private void SetVisibility(ScrollView itemBody, bool flag)
        {
            if (flag)
            {
                if (!bodyElement.Contains(itemBody)) bodyElement.Add(itemBody);
            }
            else
            {
                if (bodyElement.Contains(itemBody)) bodyElement.Remove(itemBody);
            }
        }

        private void InitAssetBundleItems()
        {
            var assetBuntleItemFile = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleFileItem.uxml";
            assetBundleTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetBuntleItemFile);

            assetBunleItemBody = new ScrollView
            {
                style =
                {
                    overflow = Overflow.Hidden
                }
            };
        }

        private void InitShaderItems()
        {
            var shaderItem = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/ShaderItem.uxml";
            shaderItemBody = new ScrollView
            {
                style =
                {
                    overflow = Overflow.Hidden
                }
            };
            shaderTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(shaderItem);
        }

        private void InitShaderVariants()
        {
            shaderVariantsItemBody = new ScrollView
            {
                style =
                {
                    overflow = Overflow.Hidden
                }
            };
        }

        private void SelectAssetBundleFile()
        {
            var file = EditorUtility.OpenFilePanel("Select AssetBundle", "", "");
            if (string.IsNullOrEmpty(file)) return;
            var isResolveDependencies = isResolveDepencies.value;

            if (isResolveDependencies)
            {
                var fileList = new List<string>();
                AssetBundleManifestResolver.GetLoadFiles(file, fileList);
                foreach (var loadFile in fileList) LoadAssetBundle(loadFile);
            }
            else
            {
                LoadAssetBundle(file);
            }
        }

        private void LoadAssetBundle(string file)
        {
            var assetBundleItem = new AssetBundleItemUI(file, assetBundleTreeAsset, OnDeleteAssetBundleItem);
            if (!assetBundleItem.Validate()) return;
            assetBundleItem.AddToElement(assetBunleItemBody);
            loadAbItemUIs.Add(assetBundleItem);

            // Shaderリスト
            var shaders = new List<Shader>();
            assetBundleItem.CollectAbObjectToList(shaders);
            var shaderItems = new List<ShaderItemUI>();
            foreach (var shader in shaders)
            {
                var shaderItem = new ShaderItemUI(shader, shaderTreeAsset, openDateStr);
                shaderItem.AddToElement(shaderItemBody);
                shaderItems.Add(shaderItem);
            }

            loadShaderItems.Add(assetBundleItem, shaderItems);
            // shaderVariantCollectionリスト
            var variantCollections = new List<ShaderVariantCollection>();
            assetBundleItem.CollectAbObjectToList(variantCollections);
            var variantItems = new List<ShaderVariantInfoUI>();
            foreach (var variantCollection in variantCollections)
            {
                var variantItem = new ShaderVariantInfoUI(variantCollection, openDateStr);
                variantItem.AddToElement(shaderVariantsItemBody);
                variantItems.Add(variantItem);
            }

            loadVariantItems.Add(assetBundleItem, variantItems);
        }


        private void OnDeleteAssetBundleItem(AssetBundleItemUI item)
        {
            if (loadAbItemUIs != null) loadAbItemUIs.Remove(item);
            if (loadShaderItems != null)
            {
                if (loadShaderItems.TryGetValue(item, out var shaderItems))
                {
                    foreach (var shaderItem in shaderItems) shaderItem.Remove();
                    loadShaderItems.Remove(item);
                }
            }

            if (loadVariantItems != null)
            {
                if (loadVariantItems.TryGetValue(item, out var variantItems))
                {
                    foreach (var variantItem in variantItems) variantItem.Remove();
                    loadVariantItems.Remove(item);
                }
            }
        }

        private static VisualElement CloneTree(VisualTreeAsset asset)
        {
            return asset.CloneTree();
        }

        private void DumpAllShader()
        {
            if (dumpExecute == null) dumpExecute = ExecuteDumpAll();
        }

        private IEnumerator ExecuteDumpAll()
        {
            var variantItems = new List<ShaderVariantInfoUI>();
            var allItem = new List<ShaderItemUI>();
            foreach (var items in loadShaderItems.Values)
                if (items != null)
                    allItem.AddRange(items);
            foreach (var items in loadVariantItems.Values)
                if (items != null)
                    variantItems.AddRange(items);
            yield return null;

            foreach (var item in allItem)
            {
                item.DumpStart();
                while (!item.IsDumpComplete()) yield return null;
            }

            foreach (var item in variantItems)
            {
                item.DumpToJson();
                yield return null;
            }
        }


        private void UnloadAll()
        {
            var delList = new List<AssetBundleItemUI>(loadAbItemUIs);
            foreach (var del in delList)
            {
                del.RemoveFormParent();
                del.Dispose();
            }
        }
    }
}