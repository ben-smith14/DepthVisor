using System;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DepthVisor.UI
{
    public class DialogManager : MonoBehaviour
    {
        [Header("Dialog Components")]
        [SerializeField] TextMeshProUGUI DialogText = null;
        [SerializeField] Button ConfirmButton = null;
        [SerializeField] Button CancelButton = null;

        public event EventHandler<DialogClosedEventArgs> DialogClosed;
        private CanvasGroup mainCanvasGroup;

        public void InitialiseDialog(string message, string confirmText = "Confirm", string cancelText = "Cancel")
        {
            // Hide the dialog when first created and get a reference to the main canvas group of
            // its parent that contains all of the other UI elements
            gameObject.SetActive(false);
            mainCanvasGroup = gameObject.transform.parent.GetComponentInChildren<CanvasGroup>();

            // Set the dialog message and set the text of the confirm and cancel buttons
            DialogText.text = message;
            ConfirmButton.GetComponentInChildren<TextMeshProUGUI>().text = confirmText;
            CancelButton.GetComponentInChildren<TextMeshProUGUI>().text = cancelText;
        }

        public void ShowDialog()
        {
            // Disable the main canvas group and show the dialog
            mainCanvasGroup.interactable = false;
            gameObject.SetActive(true);
        }

        public void ConfirmDialog()
        {
            // Trigger the dialog close event, passing a success argument
            // to any subscribers that indicates it was confirmed
            DialogClosedEventArgs dialogArgs = new DialogClosedEventArgs
            {
                Success = true
            };
            DialogClosed.Invoke(this, dialogArgs);

            mainCanvasGroup.interactable = true;
            Destroy(gameObject);
        }

        public void CancelDialog()
        {
            // Trigger the dialog close event, passing a success argument
            // to any subscribers that indicates it was canceled
            DialogClosedEventArgs dialogArgs = new DialogClosedEventArgs
            {
                Success = false
            };
            DialogClosed.Invoke(this, dialogArgs);

            mainCanvasGroup.interactable = true;
            Destroy(gameObject);
        }
    }

    public class DialogClosedEventArgs : EventArgs
    {
        public bool Success { get; set; }
    }
}
