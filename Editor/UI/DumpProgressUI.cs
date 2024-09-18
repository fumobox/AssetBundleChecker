using UnityEngine.UIElements;

namespace UTJ
{
    public class DumpProgressUI : VisualElement
    {
        private readonly Label label;
        private readonly ProgressBar progressBar;

        public DumpProgressUI()
        {
            progressBar = new ProgressBar();
            Add(progressBar);
            label = new Label();
            Add(label);
        }

        public float value
        {
            set => progressBar.value = value;
        }

        public string text
        {
            set => label.text = value;
        }
    }
}