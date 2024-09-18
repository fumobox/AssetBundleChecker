using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UTJ
{
    public class ShaderItemUI
    {
        internal static readonly string SaveDir = "ShaderVariants/AssetBundles";
        private readonly string dateTimeStr;
        private DumpProgressUI dumpProgress;
        private readonly VisualElement element;
        private readonly Shader shader;

        private ShaderDumpInfo shaderDumpInfo;

        public ShaderItemUI(Shader sh, VisualTreeAsset treeAsset, string date)
        {
            shader = sh;
            dateTimeStr = date;
            var shaderData = ShaderUtil.GetShaderData(sh);
            element = treeAsset.CloneTree();

            element.Q<Foldout>("ShaderFold").text = sh.name;
            element.Q<Foldout>("ShaderFold").value = false;
            // shader value
            element.Q<ObjectField>("ShaderVal").objectType = typeof(Shader);
            element.Q<ObjectField>("ShaderVal").value = sh;

            var shaderSubShadersFold = element.Q<Foldout>("SubShaders");
            shaderSubShadersFold.text = "SubShaders(" + shaderData.SubshaderCount + ")";
            shaderSubShadersFold.value = false;

            for (var i = 0; i < shaderData.SubshaderCount; ++i)
            {
                var subShaderFold = new Foldout();
                var subShader = shaderData.GetSubshader(i);

                CreateSubShaderMenu(subShaderFold, i, subShader);
                shaderSubShadersFold.Add(subShaderFold);
            }

            // DumpBtn
            element.Q<Button>("DumpButton").clickable.clicked += DumpStart;
        }

        public void AddToElement(VisualElement parent)
        {
            parent.Add(element);
        }

        private void CreateSubShaderMenu(Foldout subShaderFold, int idx, ShaderData.Subshader subShader)
        {
            subShaderFold.text = "SubShader " + idx + " PassNum:" + subShader.PassCount;

            for (var i = 0; i < subShader.PassCount; ++i)
            {
                var pass = subShader.GetPass(i);
                var label = new Label("PassName:" + pass.Name);
                subShaderFold.Add(label);
            }
        }

        public void Remove()
        {
            shaderDumpInfo = null;
            element.parent.Remove(element);
        }

        public bool IsDumpComplete()
        {
            if (shaderDumpInfo == null) return false;
            return shaderDumpInfo.IsComplete;
        }

        public void DumpStart()
        {
            if (shaderDumpInfo != null) return;
            shaderDumpInfo = new ShaderDumpInfo(shader);

            var dumpBtn = element.Q<Button>("DumpButton");
            // add progress
            dumpProgress = new DumpProgressUI
            {
                style =
                {
                    width = 200
                }
            };
            dumpBtn.parent.Add(dumpProgress);
            //
            dumpBtn.parent.Remove(dumpBtn);
            EditorApplication.update += Update;
        }

        private void Update()
        {
            if (shaderDumpInfo == null)
            {
                EditorApplication.update -= Update;
                return;
            }

            shaderDumpInfo.SetYieldCheckTime();
            if (!shaderDumpInfo.MoveNext())
            {
                OnDumpComplete();
            }
            else
            {
                dumpProgress.value = shaderDumpInfo.Progress * 100.0f;
                dumpProgress.text = shaderDumpInfo.ProgressStr;
            }
        }

        private void OnDumpComplete()
        {
            var dir = SaveDir + '/' + dateTimeStr;

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var parent = dumpProgress.parent;
            parent.Remove(dumpProgress);
            CreateKeywordList(parent);

            var jsonString = JsonUtility.ToJson(shaderDumpInfo);
            var file = Path.Combine(dir, shader.name.Replace("/", "_") + ".json");
            File.WriteAllText(file, jsonString);
            EditorApplication.update -= Update;
        }

        private void CreateKeywordList(VisualElement parent)
        {
            var keywords = shaderDumpInfo.CollectKeywords();
            var keywordFold = new Foldout();
            keywordFold.text = "Keywords(" + keywords.Count + ")";

            keywordFold.style.left = 20;
            keywordFold.value = false;
            foreach (var keyword in keywords)
            {
                var keywordLabel = new Label(keyword);
                keywordFold.Add(keywordLabel);
            }

            parent.Add(keywordFold);
        }
    }
}