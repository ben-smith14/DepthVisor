using UnityEngine;
using TMPro;

namespace DepthVisor.UI
{
    public class FileItem : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI fileName = null;

        public void SetFileName(string name)
        {
            fileName.text = name;
        }

        public string GetFileName()
        {
            return fileName.text;
        }
    }
}
