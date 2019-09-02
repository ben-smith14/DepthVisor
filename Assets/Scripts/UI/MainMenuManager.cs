using System;
using System.IO;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// File Browser plugin from Unity asset store and code available
// at https://github.com/yasirkula/UnitySimpleFileBrowser
using SimpleFileBrowser;

namespace DepthVisor.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Main Menu Buttons")]
        [SerializeField] Button RecordingButton = null;
        [SerializeField] Button PlaybackButton = null;
        [SerializeField] Button SavePathButton = null;

        private const string emptySavePath = "empty";
        private const string setSavePathText = "Set Save Path";
        private const string changeSavePathText = "Change Save Path";

        void Start()
        {
            // Retrieve the current save path from the app player preferences and
            // ensure that the save path button is clickable
            string savePath = PlayerPrefs.GetString("savePath", emptySavePath);
            SavePathButton.interactable = true;

            // If the save path equals the default value, it is not set, so disable the
            // scene navigation buttons and set the text of the save path button to
            // indicate that it is yet to be set
            if (savePath.Equals(emptySavePath))
            {
                RecordingButton.interactable = false;
                PlaybackButton.interactable = false;
                SavePathButton.GetComponentInChildren<TextMeshProUGUI>().text = setSavePathText;
            }
            else
            {
                // Otherwise, a save path is set, so make all buttons interactable and set the
                // save path button text to indicate that it can be used to change the existing
                // save path
                RecordingButton.interactable = true;
                PlaybackButton.interactable = true;
                SavePathButton.GetComponentInChildren<TextMeshProUGUI>().text = changeSavePathText;
            }

            // Add a quick link to the file browser before it is opened that will allow a user to
            // easily navigate to the main Users folder on the machine
            FileBrowser.AddQuickLink("All Users",
                                     Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName,
                                     null);
        }

        public void GoToRecordingScene()
        {
            SceneManager.LoadScene("Recording");
        }

        public void GoToPlaybackScene()
        {
            SceneManager.LoadScene("Playback");
        }

        public void ExitApplication()
        {
            Application.Quit();
        }

        public void SetSavePath()
        {
            // Disable all main menu buttons whilst the dialog box is open
            RecordingButton.interactable = false;
            PlaybackButton.interactable = false;
            SavePathButton.interactable = false;

            // Start the coroutine that will open the dialog box and wait for a response from
            // the user
            StartCoroutine(ShowDialogCoroutine());
        }

        private IEnumerator ShowDialogCoroutine()
        {
            // Show the dialog and wait for the user to either close it or select a file path. Set the box
            // to file mode, set the starting folder as the current user's main folder and set the box heading
            yield return FileBrowser.WaitForLoadDialog(true,
                                                       Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                                       "Select Save Path");

            // Once the box has been closed, check whether or not a file was selected or if it was cancelled
            if (FileBrowser.Success)
            {
                // If a file path was selected, save it to the player prefs, reenable all buttons and change
                // the save path button text
                PlayerPrefs.SetString("savePath", FileBrowser.Result);

                RecordingButton.interactable = true;
                PlaybackButton.interactable = true;
                SavePathButton.interactable = true;
                SavePathButton.GetComponentInChildren<TextMeshProUGUI>().text = changeSavePathText;
            }
            else
            {
                // Otherwise, the box was cancelled, so check if a save path has been provided before
                if (PlayerPrefs.GetString("savePath", emptySavePath).Equals(emptySavePath))
                {
                    // If it hasn't, only reenable the save path button and set its text to indicate
                    // that it needs to be set
                    SavePathButton.interactable = true;
                    SavePathButton.GetComponentInChildren<TextMeshProUGUI>().text = setSavePathText;
                }
                else
                {
                    // If it has, reenable all buttons and change the save path button text once again
                    RecordingButton.interactable = true;
                    PlaybackButton.interactable = true;
                    SavePathButton.interactable = true;
                    SavePathButton.GetComponentInChildren<TextMeshProUGUI>().text = changeSavePathText;
                }
            }
        }
    }
}
