using UnityEngine;
using TMPro;

namespace DepthVisor.UI
{
    public class FileItem : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI fileName;

        public void SetFileName(string name)
        {
            fileName.text = name;
        }
    }
}
