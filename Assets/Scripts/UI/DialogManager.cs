using System;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DepthVisor.UI
{
    public class DialogManager : MonoBehaviour
    {
        [Header("Dialog Components")]
        [SerializeField] TextMeshProUGUI DialogText;
        [SerializeField] Button CloseButton;
        [SerializeField] Button ConfirmButton;
        [SerializeField] Button CancelButton;

        public event EventHandler<DialogClosedEventArgs> DialogClosed;
        private CanvasGroup mainCanvasGroup;

        public void InitialiseDialog(string message)
        {
            // Hide the dialog when first created, get a reference to the main canvas group of its
            // parent canvas that contains all of the other UI elements and set the message
            gameObject.SetActive(false);
            mainCanvasGroup = gameObject.transform.parent.GetComponentInChildren<CanvasGroup>();
            DialogText.text = message;
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
