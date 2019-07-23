using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
