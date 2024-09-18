﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ
{
    internal class InstanciateGameObjectUI:System.IDisposable
    {
        private InstantiateGameObjectFromAb instantiateGameObject;
        private Button flipShaderButton;

        internal InstanciateGameObjectUI(InstantiateGameObjectFromAb inst)
        {
            this.instantiateGameObject = inst;
        }

        internal void AddToParent(VisualElement parent)
        {
            VisualElement element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;

            Label label = new Label(instantiateGameObject.gameObject.name);
            flipShaderButton = new Button();
            flipShaderButton.text = "AssetBundle Shader";
            flipShaderButton.clickable.clicked += FlipAbProjectShader;

            element.Add(label);
            element.Add(flipShaderButton);
            parent.Add(element);
        }

        void FlipAbProjectShader()
        {
            if( this.instantiateGameObject.IsProjectShader)
            {
                this.instantiateGameObject.SetAbOrigin();
                flipShaderButton.text = "AssetBundle Shader";
            }
            else
            {
                this.instantiateGameObject.SetProjectOrigin();
                flipShaderButton.text = "Project Shader";
            }
        }

        public void Dispose()
        {
        }
    }
}
