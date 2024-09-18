using System;
using UnityEngine.UIElements;

namespace UTJ
{
    internal class InstantiateGameObjectUI : IDisposable
    {
        private Button flipShaderButton;
        private readonly InstantiateGameObjectFromAb instantiateGameObject;

        internal InstantiateGameObjectUI(InstantiateGameObjectFromAb inst)
        {
            instantiateGameObject = inst;
        }

        public void Dispose()
        {
        }

        internal void AddToParent(VisualElement parent)
        {
            var element = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            var label = new Label(instantiateGameObject.gameObject.name);
            flipShaderButton = new Button
            {
                text = "AssetBundle Shader"
            };
            flipShaderButton.clickable.clicked += FlipAbProjectShader;

            element.Add(label);
            element.Add(flipShaderButton);
            parent.Add(element);
        }

        private void FlipAbProjectShader()
        {
            if (instantiateGameObject.IsProjectShader)
            {
                instantiateGameObject.SetAbOrigin();
                flipShaderButton.text = "AssetBundle Shader";
            }
            else
            {
                instantiateGameObject.SetProjectOrigin();
                flipShaderButton.text = "Project Shader";
            }
        }
    }
}