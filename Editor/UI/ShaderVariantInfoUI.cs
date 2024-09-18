using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UTJ
{
    public class ShaderVariantInfoUI
    {
        private readonly string dateStr;
        private DumpInfo dumpInfo;
        private VisualElement element;
        private readonly List<Shader> shaders = new();
        private readonly Dictionary<Shader, ShaderVariants> shaderVariants = new();
        private readonly ShaderVariantCollection variantCollection;

        public ShaderVariantInfoUI(ShaderVariantCollection collection, string date)
        {
            variantCollection = collection;
            dateStr = date;

            ConstructShaderList();
            InitUI();
        }

        private void InitUI()
        {
            var mainFold = new Foldout();
            mainFold.text = variantCollection.name;

            ObjectField objectField = null;

            objectField = new ObjectField("ShaderVariantCollection")
            {
                objectType = typeof(ShaderVariantCollection),
                value = variantCollection
            };
            mainFold.Add(objectField);

            foreach (var shader in shaders)
            {
                var shaderFold = new Foldout
                {
                    text = shader.name,
                    style =
                    {
                        paddingLeft = 10
                    }
                };


                var shaderObject = new ObjectField("Shader")
                {
                    objectType = typeof(Shader),
                    value = shader
                };
                shaderFold.Add(shaderObject);
                shaderFold.value = false;


                ShaderVariants variants = null;
                if (shaderVariants.TryGetValue(shader, out variants))
                {
                    var keywordsFold = new Foldout
                    {
                        text = "keywords(" + variants.keywordNames.Count + ")",
                        style =
                        {
                            left = 20
                        },
                        value = false
                    };
                    foreach (var keyword in variants.keywordNames)
                    {
                        var str = keyword;
                        if (str == "") str = "<none>";
                        var keywordLabel = new Label(str);
                        keywordsFold.Add(keywordLabel);
                    }

                    shaderFold.Add(keywordsFold);
                }

                mainFold.Add(shaderFold);
            }

            var dumpButton = new Button
            {
                text = "Dump To Json"
            };
            dumpButton.clickable.clicked += () =>
            {
                DumpToJson();
                dumpButton.parent.Remove(dumpButton);
            };
            mainFold.Add(dumpButton);

            element = new VisualElement();
            element.Add(mainFold);
        }

        private void ConstructShaderList()
        {
            var serializedObject = new SerializedObject(variantCollection);
            var shaderProperties = serializedObject.FindProperty("m_Shaders");
            if (shaderProperties == null) return;
            for (var i = 0; i < shaderProperties.arraySize; ++i)
            {
                var shaderProp = shaderProperties.GetArrayElementAtIndex(i);
                var shader = shaderProp.FindPropertyRelative("first").objectReferenceValue as Shader;
                var variants = new ShaderVariants();
                var variantsProp = shaderProp.FindPropertyRelative("second.variants");

                for (var j = 0; j < variantsProp.arraySize; ++j)
                {
                    var variantProp = variantsProp.GetArrayElementAtIndex(j).FindPropertyRelative("keywords");
                    variants.keywordNames.Add(variantProp.stringValue);
                }

                shaderVariants.Add(shader, variants);
                shaders.Add(shader);
            }
        }

        public void AddToElement(VisualElement parent)
        {
            parent.Add(element);
        }

        public void Remove()
        {
            element.parent.Remove(element);
        }

        public void DumpToJson()
        {
            if (dumpInfo != null) return;
            dumpInfo = new DumpInfo
            {
                collectionName = variantCollection.name,
                shaderInfos = new List<DumpShaderInfo>()
            };

            foreach (var shader in shaders)
            {
                var shaderInfo = new DumpShaderInfo
                {
                    shaderName = shader.name
                };
                if (shaderVariants.TryGetValue(shader, out var variants))
                {
                    shaderInfo.keywords = new List<string>(variants.keywordNames);
                    shaderInfo.keywords.Sort();
                }

                dumpInfo.shaderInfos.Add(shaderInfo);
            }

            var str = JsonUtility.ToJson(dumpInfo);
            var dir = ShaderItemUI.SaveDir + '/' + dateStr + "/variants";

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var jsonFile = Path.Combine(dir, variantCollection.name + ".json");
            File.WriteAllText(jsonFile, str);
        }

        private class ShaderVariants
        {
            public List<string> keywordNames = new();
        }


        [Serializable]
        private class DumpShaderInfo
        {
            [SerializeField] public string shaderName;

            [SerializeField] public List<string> keywords;
        }

        [Serializable]
        private class DumpInfo
        {
            [SerializeField] public string collectionName;

            [SerializeField] public List<DumpShaderInfo> shaderInfos;
        }
    }
}