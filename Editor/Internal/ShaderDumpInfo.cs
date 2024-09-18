using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    public class ShaderDumpInfo
    {
        public enum ShaderGpuProgramType
        {
            GpuProgramUnknown = 0,

            GpuProgramGLLegacy_Removed = 1,
            GpuProgramGLES31AEP = 2,
            GpuProgramGLES31 = 3,
            GpuProgramGLES3 = 4,
            GpuProgramGLES = 5,
            GpuProgramGLCore32 = 6,
            GpuProgramGLCore41 = 7,
            GpuProgramGLCore43 = 8,
            GpuProgramDX9VertexSM20_Removed = 9,
            GpuProgramDX9VertexSM30_Removed = 10,
            GpuProgramDX9PixelSM20_Removed = 11,
            GpuProgramDX9PixelSM30_Removed = 12,
            GpuProgramDX10Level9Vertex_Removed = 13,
            GpuProgramDX10Level9Pixel_Removed = 14,
            GpuProgramDX11VertexSM40 = 15,
            GpuProgramDX11VertexSM50 = 16,
            GpuProgramDX11PixelSM40 = 17,
            GpuProgramDX11PixelSM50 = 18,
            GpuProgramDX11GeometrySM40 = 19,
            GpuProgramDX11GeometrySM50 = 20,
            GpuProgramDX11HullSM50 = 21,
            GpuProgramDX11DomainSM50 = 22,
            GpuProgramMetalVS = 23,
            GpuProgramMetalFS = 24,

            GpuProgramSPIRV = 25,

            GpuProgramConsole = 26,
            GpuProgramCount
        }

        private readonly IEnumerator executeProgress;

        [SerializeField] public string fallback;

        [SerializeField] public List<int> keywordFlags;

        [SerializeField] public List<string> keywordNames;

        [SerializeField] public string name;

        [SerializeField] public List<PropInfo> propInfos;

        private readonly SerializedObject serializedObject;

        [SerializeField] public List<SubShaderInfo> subShaderInfos;


        private readonly DumpYieldCheck yieldChk = new();

        public ShaderDumpInfo(Shader sh)
        {
            serializedObject = new SerializedObject(sh);
            executeProgress = Execute();
        }

        public bool IsComplete { get; set; }

        public float Progress
        {
            get
            {
                if (yieldChk == null) return 0.0f;
                return yieldChk.Progress;
            }
        }

        public string ProgressStr
        {
            get
            {
                if (yieldChk == null) return "";
                return yieldChk.CurrentState;
            }
        }


        public List<string> CollectKeywords()
        {
            var hashedKeywords = new HashSet<string>();
            foreach (var subshader in subShaderInfos)
            foreach (var pass in subshader.passes)
            {
                foreach (var gpuProgram in pass.fragmentInfos) hashedKeywords.Add(gpuProgram.CombinedKeyword);
                foreach (var gpuProgram in pass.vertInfos) hashedKeywords.Add(gpuProgram.CombinedKeyword);
            }

            var retList = new List<string>(hashedKeywords);
            retList.Sort();
            return retList;
        }

        public bool MoveNext()
        {
            return executeProgress.MoveNext();
        }

        public void SetYieldCheckTime()
        {
            yieldChk.SetYieldCheckTime();
        }

        private void ExecuteKeywordInfos()
        {
            // names
            var keywordNamesProp = serializedObject.FindProperty("m_ParsedForm.m_KeywordNames");
            var keywordNameNum = keywordNamesProp.arraySize;
            keywordNames = new List<string>(keywordNameNum);
            for (var i = 0; i < keywordNameNum; ++i)
                keywordNames.Add(keywordNamesProp.GetArrayElementAtIndex(i).stringValue);
            // flags
            var flagsProp = serializedObject.FindProperty("m_ParsedForm.m_KeywordFlags");
            var flagsNum = flagsProp.arraySize;
            keywordFlags = new List<int>(flagsNum);
            for (var i = 0; i < flagsNum; ++i) keywordFlags.Add(flagsProp.GetArrayElementAtIndex(i).intValue);
        }

        private IEnumerator Execute()
        {
            //EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            /*
            var prop = serializedObject.GetIterator();
            while (prop.Next(true))
            {
                Debug.Log(prop.name + "::" + prop.stringValue);
            }
            yield return null;
            */
            // name
            var nameProp = serializedObject.FindProperty("m_ParsedForm.m_Name");
            name = nameProp.stringValue;
            //fallback
            var fallbackProp = serializedObject.FindProperty("m_ParsedForm.m_FallbackName");
            fallback = fallbackProp.stringValue;

            // props
            var propsproperty = serializedObject.FindProperty("m_ParsedForm.m_PropInfo.m_Props");
            propInfos = new List<PropInfo>(propsproperty.arraySize);
            for (var i = 0; i < propsproperty.arraySize; ++i)
            {
                var prop = propsproperty.GetArrayElementAtIndex(i);
                var propInfo = new PropInfo(prop);
                propInfos.Add(propInfo);
            }

            yield return null;

            // keyword names
            ExecuteKeywordInfos();
            yield return null;

            // subShaders
            var subShadersProp = serializedObject.FindProperty("m_ParsedForm.m_SubShaders");
            var subShaderNum = subShadersProp.arraySize;
            subShaderInfos = new List<SubShaderInfo>(subShaderNum);
            yieldChk.SetSubshaderCount(subShaderNum);

            for (var i = 0; i < subShadersProp.arraySize; ++i)
            {
                var currentSubShaderProp = subShadersProp.GetArrayElementAtIndex(i);
                var info = new SubShaderInfo(this, currentSubShaderProp, yieldChk);
                while (info.MoveNext()) yield return null;
                subShaderInfos.Add(info);
                // yield chk
                yieldChk.CompleteSubShaderIdx(i);
                if (yieldChk.ShouldYield()) yield return null;
            }

            IsComplete = true;
        }

        [Serializable]
        public class GpuProgramInfo
        {
            [SerializeField] public int tierIndex;

            [SerializeField] public string gpuProgramType;

            [SerializeField] public List<int> keywordIndecies;

            [SerializeField] public List<int> localKeywordIndecies;

            [SerializeField] public List<string> keywords;

            [NonSerialized] public string CombinedKeyword;

            public GpuProgramInfo(SerializedProperty serializedProperty, int tier)
            {
                var gpuProgramTypeProp = serializedProperty.FindPropertyRelative("m_GpuProgramType");
                tierIndex = tier;
                gpuProgramType = ((ShaderGpuProgramType)gpuProgramTypeProp.intValue).ToString();

                var keywords = serializedProperty.FindPropertyRelative("m_KeywordIndices");

                if (keywords == null) keywords = serializedProperty.FindPropertyRelative("m_GlobalKeywordIndices");
                keywordIndecies = new List<int>(keywords.arraySize);
                for (var i = 0; i < keywords.arraySize; ++i)
                {
                    var keywordIndex = keywords.GetArrayElementAtIndex(i).intValue;
                    keywordIndecies.Add(keywordIndex);
                    // Debug.Log("keywordIndex[" + i + "]" + keywordIndex);
                }

                var localKeywords = serializedProperty.FindPropertyRelative("m_LocalKeywordIndices");
                if (localKeywords != null)
                {
                    localKeywordIndecies = new List<int>();
                    for (var i = 0; i < localKeywords.arraySize; ++i)
                    {
                        var keywordIndex = localKeywords.GetArrayElementAtIndex(i).intValue;
                        localKeywordIndecies.Add(keywordIndex);
                    }
                }
            }

            public void ResolveKeywordName(List<string> keywordNames)
            {
                keywords = new List<string>(keywordIndecies.Count);
                foreach (var index in keywordIndecies)
                {
                    if (index < keywordNames.Count)
                        keywords.Add(keywordNames[index]);
                    else
                        Debug.LogError("ResolveKeywordName failed " + index + "::" + keywordIndecies.Count);
                }

                if (localKeywordIndecies != null)
                    foreach (var index in localKeywordIndecies)
                    {
                        if (index < keywordNames.Count)
                            keywords.Add(keywordNames[index]);
                        else
                            Debug.LogError("ResolveKeywordName failed " + index + "::" + keywordIndecies.Count);
                    }

                ResolvedConbinedKeyword();
            }

            private void ResolvedConbinedKeyword()
            {
                var sortedKeywords = new List<string>(keywords);
                sortedKeywords.Sort();
                var length = 0;
                foreach (var word in sortedKeywords) length += word.Length + 1;
                var builder = new StringBuilder(length);
                foreach (var word in sortedKeywords) builder.Append(word).Append(' ');
                CombinedKeyword = builder.Length <= 1 ? "<none>" : builder.ToString();
            }
        }

        [Serializable]
        public class ShaderState
        {
            [SerializeField] public string name;

            [SerializeField] public List<ShaderTagInfo> tags;

            public ShaderState(SerializedProperty serializedProperty)
            {
                name = serializedProperty.FindPropertyRelative("m_Name").stringValue;
                var tagsProp = serializedProperty.FindPropertyRelative("m_Tags.tags");

                tags = new List<ShaderTagInfo>(tagsProp.arraySize);

                for (var i = 0; i < tagsProp.arraySize; ++i)
                {
                    var tagInfo = new ShaderTagInfo(tagsProp.GetArrayElementAtIndex(i));
                    tags.Add(tagInfo);
                }
            }
        }

        [Serializable]
        public class ShaderTagInfo
        {
            [SerializeField] public string key;

            [SerializeField] public string value;

            public ShaderTagInfo(SerializedProperty serializedProperty)
            {
                var firstProp = serializedProperty.FindPropertyRelative("first");
                var secondProp = serializedProperty.FindPropertyRelative("second");

                key = firstProp.stringValue;
                value = secondProp.stringValue;

                //                Debug.Log("tag::" + firstProp.stringValue + ":" + secondProp.stringValue);
            }
        }

        [Serializable]
        public class KeywordDictionaryInfo
        {
            [SerializeField] public int idx;

            [SerializeField] public string keyword;

            public KeywordDictionaryInfo(int index, string key)
            {
                idx = index;
                keyword = key;
            }
        }

        [Serializable]
        public class PassInfo
        {
            [SerializeField] public string useName;

            [SerializeField] public string name;

            [SerializeField] public ShaderState state;

            [SerializeField] public List<ShaderTagInfo> tags;

            [SerializeField] public List<KeywordDictionaryInfo> keywordInfos;

            [SerializeField] public List<GpuProgramInfo> vertInfos;

            [SerializeField] public List<GpuProgramInfo> fragmentInfos;

            private ShaderDumpInfo dumpInfoObject;
            private IEnumerator execute;
            private Dictionary<int, string> keywordDictionary;

            private SerializedProperty serializedProperty;
            private DumpYieldCheck yieldChk;

            public PassInfo(ShaderDumpInfo dumpInfo, SerializedProperty prop, DumpYieldCheck yieldCheck)
            {
                dumpInfoObject = dumpInfo;
                serializedProperty = prop;
                yieldChk = yieldCheck;
                execute = Execute();
            }

            public bool MoveNext()
            {
                return execute.MoveNext();
            }

            public IEnumerator Execute()
            {
                SetupShaderStage(serializedProperty);
                SetupTags(serializedProperty);
                SetupNameInfo(serializedProperty);
                var shaderExecute = ExecuteShader();
                while (shaderExecute.MoveNext()) yield return null;
                yield return null;
            }

            public IEnumerator ExecuteShader()
            {
                var progVertex = serializedProperty.FindPropertyRelative("progVertex.m_PlayerSubPrograms");
                var progFragment = serializedProperty.FindPropertyRelative("progFragment.m_PlayerSubPrograms");
                yield return null;
                var vertexNum = GetSubProgramNum(progVertex);
                var fragmentNum = GetSubProgramNum(progFragment);
                yieldChk.SetVertexNum(vertexNum);
                yieldChk.SetFragmentNum(fragmentNum);
                vertInfos = new List<GpuProgramInfo>(vertexNum);
                fragmentInfos = new List<GpuProgramInfo>(fragmentNum);

                var vertTierNum = progVertex.arraySize;
                for (var tierIndex = 0; tierIndex < vertTierNum; ++tierIndex)
                {
                    var tierPrograms = progVertex.GetArrayElementAtIndex(tierIndex);
                    var vertExec = ExecuteGPUPrograms(vertInfos, tierIndex, tierPrograms, tierPrograms.arraySize,
                        yieldChk.CompleteVertIdx);
                    while (vertExec.MoveNext()) yield return null;
                }

                var fragTierNum = progFragment.arraySize;
                for (var tierIndex = 0; tierIndex < fragTierNum; ++tierIndex)
                {
                    var tierPrograms = progFragment.GetArrayElementAtIndex(tierIndex);
                    var vertExec = ExecuteGPUPrograms(fragmentInfos, tierIndex, tierPrograms, tierPrograms.arraySize,
                        yieldChk.CompleteFragIdx);
                    while (vertExec.MoveNext()) yield return null;
                }
            }

            private int GetSubProgramNum(SerializedProperty prop)
            {
                var num = 0;
                var tierNum = prop.arraySize;
                for (var i = 0; i < tierNum; i++)
                {
                    var tierPrograms = prop.GetArrayElementAtIndex(i);
                    num += tierPrograms.arraySize;
                }

                return num;
            }

            private IEnumerator ExecuteGPUPrograms(List<GpuProgramInfo> programs, int tier, SerializedProperty props,
                int num, Action<int> onCompleteIndex)
            {
                for (var i = 0; i < num; ++i)
                {
                    var gpuProgram = new GpuProgramInfo(props.GetArrayElementAtIndex(i), tier);
                    gpuProgram.ResolveKeywordName(dumpInfoObject.keywordNames);
                    vertInfos.Add(gpuProgram);
                    // yield
                    onCompleteIndex(i);
                    if (yieldChk.ShouldYield()) yield return null;
                }
            }


            private void SetupShaderStage(SerializedProperty serializedProperty)
            {
                var stateProp = serializedProperty.FindPropertyRelative("m_State");
                state = new ShaderState(stateProp);
            }

            private void SetupTags(SerializedProperty serializedProperty)
            {
                var tagsProp = serializedProperty.FindPropertyRelative("m_Tags.tags");

                tags = new List<ShaderTagInfo>(tagsProp.arraySize);
                for (var i = 0; i < tagsProp.arraySize; ++i)
                {
                    var tagInfo = new ShaderTagInfo(serializedProperty.GetArrayElementAtIndex(i));
                    tags.Add(tagInfo);
                }
            }


            private void SetupNameInfo(SerializedProperty serializedProperty)
            {
                useName = serializedProperty.FindPropertyRelative("m_UseName").stringValue;
                name = serializedProperty.FindPropertyRelative("m_Name").stringValue;
            }
        }

        [Serializable]
        public class SubShaderInfo
        {
            [SerializeField] public List<PassInfo> passes;

            private ShaderDumpInfo dumpInfoObject;
            private IEnumerator execute;

            private SerializedProperty serializedProperty;
            private DumpYieldCheck yieldChk;

            public SubShaderInfo(ShaderDumpInfo dumpInfo, SerializedProperty prop, DumpYieldCheck yieldCheck)
            {
                dumpInfoObject = dumpInfo;
                serializedProperty = prop;
                yieldChk = yieldCheck;
                execute = Execute();
            }

            public bool MoveNext()
            {
                return execute.MoveNext();
            }

            private IEnumerator Execute()
            {
                var passesProp = serializedProperty.FindPropertyRelative("m_Passes");
                var passCnt = passesProp.arraySize;
                passes = new List<PassInfo>(passCnt);
                yieldChk.SetPassCount(passCnt);
                for (var i = 0; i < passesProp.arraySize; ++i)
                {
                    var currentPassProp = passesProp.GetArrayElementAtIndex(i);
                    var passInfo = new PassInfo(dumpInfoObject, currentPassProp, yieldChk);
                    while (passInfo.MoveNext()) yield return null;
                    passes.Add(passInfo);
                    // yield
                    yieldChk.CompletePassIdx(i);
                    if (yieldChk.ShouldYield()) yield return null;
                }

                yield return null;
            }
        }

        [Serializable]
        public class PropInfo
        {
            [SerializeField] public string name;

            public PropInfo(SerializedProperty serializedProperty)
            {
                var propNameProperty = serializedProperty.FindPropertyRelative("m_Name");
                if (propNameProperty != null) name = propNameProperty.stringValue;
            }
        }
    }
}