using UnityEngine;
using UnityEngine.UI;

namespace DepthVisor.UI
{
    public class OpenFileButton : MonoBehaviour
    {
        // Implementation for the standard button behaviour on the open file
        // button, as its connection to the state of the file list group seems
        // to disable this otherwise
        public void OnMouseHover()
        {
            ColorBlock colors = gameObject.GetComponent<Button>().colors;
            gameObject.GetComponent<Image>().color = colors.highlightedColor;
        }

        public void OnMouseExitHover()
        {
            ColorBlock colors = gameObject.GetComponent<Button>().colors;
            gameObject.GetComponent<Image>().color = colors.normalColor;
        }

        public void OnPressed()
        {
            ColorBlock colors = gameObject.GetComponent<Button>().colors;
            gameObject.GetComponent<Image>().color = colors.pressedColor;
        }
    }
}
