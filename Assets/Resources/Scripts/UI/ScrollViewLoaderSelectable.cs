using System.Linq;

using UnityEngine;
using UnityEngine.UI;

namespace DepthVisor.UI
{
    public class ScrollViewLoaderSelectable : ScrollViewLoader
    {
        [SerializeField] Button OpenFileButton = null;

        private ToggleGroup toggleGroup;

        void Update()
        {
            // If the scroll view is visible, check to see if an item has been selected in
            // the toggle group
            if (gameObject.activeSelf)
            {
                if (toggleGroup.ActiveToggles().Any())
                {
                    // If it has, enable the open file button
                    if (!OpenFileButton.interactable) { OpenFileButton.interactable = true; }
                }
                else
                {
                    // Otherwise, disable this button
                    if (OpenFileButton.interactable) { OpenFileButton.interactable = false; }
                }
            }
        }

        public override void LoadFileNames()
        { 
            // Call the parent method to setup the scroll view
            base.LoadFileNames();

            // Get a reference to the toggle group on the game object
            toggleGroup = gameObject.GetComponent<ToggleGroup>();

            // For all children of the game object, add them to the toggle group
            // unless they do not have a toggle component, in which case break out
            // of the loop, as the only case in which this will happen is if there
            // are no files
            foreach (Transform child in gameObject.transform)
            {
                Toggle itemToggle = child.gameObject.GetComponent<Toggle>();
                if (itemToggle == null)
                {
                    break;
                }
                else
                {
                    itemToggle.group = toggleGroup;
                }
            }

            // Turn all toggles off
            toggleGroup.SetAllTogglesOff();

            // Initialise the open file button to disabled
            OpenFileButton.enabled = false;
        }

        public override void ClearCurrentContent()
        {
            // Call the parent method to remove all child objects from
            // the toggle group
            base.ClearCurrentContent();

            // Dereference the toggle group and set the open file button
            // back to disabled
            toggleGroup = null;
            OpenFileButton.enabled = false;
        }

        public string GetSelectedFileName()
        {
            // First extract the selected toggle component from the toggle group
            Toggle selectedItem = toggleGroup.ActiveToggles().ToList()[0];

            // Then, return the file name of the text within its parent game object
            return selectedItem.gameObject.GetComponent<FileItem>().GetFileName();
        }
    }
}
