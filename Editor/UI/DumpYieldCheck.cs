﻿using System.Text;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    public class DumpYieldCheck
    {
        private int fragCount;
        private int fragIdx;
        private int passCount;
        private int passIdx;
        private double startTime;

        private readonly StringBuilder stringBuilder = new(32);

        private int subShaderCount;
        private int subShaderIdx;
        private int vertCount;
        private int vertIdx;

        public float Progress
        {
            get
            {
                float subShaderProgress = 0.0f, passProgress = 0.0f, vertProgress = 0.0f, fragProgress = 0.0f;


                if (subShaderCount <= 0) return 0.0f;
                subShaderProgress = (subShaderIdx + 1) / (float)subShaderCount;

                if (passCount <= 0) return subShaderProgress;
                passProgress = (passIdx + 1) / (float)passCount;
                passProgress *= 1 / (float)subShaderCount;

                if (vertCount > 0)
                {
                    vertProgress = (vertIdx + 1) / (float)vertCount;
                    vertProgress *= 1 / (float)subShaderCount * (1 / (float)passCount) * 0.5f;
                }

                if (fragCount > 0)
                {
                    fragProgress = (fragIdx + 1) / (float)fragCount;
                    fragProgress *= 1 / (float)subShaderCount * (1 / (float)passCount) * 0.5f;
                }

                return Mathf.Min(1.0f, subShaderProgress + passProgress + vertProgress + fragProgress);
            }
        }

        public string CurrentState
        {
            get
            {
                stringBuilder.Length = 0;
                stringBuilder.Append("Sub:").Append(subShaderIdx + 1).Append('/').Append(subShaderCount);
                stringBuilder.Append(" Pass:").Append(passIdx + 1).Append('/').Append(passCount);
                stringBuilder.Append(" Vert:").Append(vertIdx + 1).Append('/').Append(vertCount);
                stringBuilder.Append(" Frag:").Append(fragIdx + 1).Append('/').Append(fragCount);
                return stringBuilder.ToString();
            }
        }

        public bool ShouldYield()
        {
            var time = EditorApplication.timeSinceStartup;
            if (time - startTime > 0.03f) return true;
            return false;
        }


        public void CompleteSubShaderIdx(int idx)
        {
            subShaderIdx = idx;
        }

        public void SetSubshaderCount(int subCnt)
        {
            subShaderCount = subCnt;
            subShaderIdx = -1;
            SetPassCount(0);
        }

        public void CompletePassIdx(int idx)
        {
            passIdx = idx;
        }

        public void SetPassCount(int passCnt)
        {
            passCount = passCnt;
            passIdx = -1;
            SetVertexNum(0);
            SetFragmentNum(0);
        }

        public void CompleteVertIdx(int idx)
        {
            vertIdx = idx;
        }

        public void SetVertexNum(int cnt)
        {
            vertCount = cnt;
            vertIdx = -1;
        }

        public void CompleteFragIdx(int idx)
        {
            fragIdx = idx;
        }

        public void SetFragmentNum(int cnt)
        {
            fragCount = cnt;
            fragIdx = -1;
        }

        public void SetYieldCheckTime()
        {
            startTime = EditorApplication.timeSinceStartup;
        }
    }
}