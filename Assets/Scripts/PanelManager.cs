using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DepthVisor.UI
{
    public class PanelManager : MonoBehaviour
    {
        public void Start()
        {
            gameObject.SetActive(false);
        }

        public void TogglePanel()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
