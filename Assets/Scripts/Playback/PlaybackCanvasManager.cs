using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

using DepthVisor.UI;
using DepthVisor.Kinect;

namespace DepthVisor.Playback
{
    public class PlaybackCanvasManager : MonoBehaviour
    {
        [Header("Main Camera")]
        [SerializeField] CameraOrbitControls OrbitalCamera = null;

        [Header("Toolbar Buttons")]
        [SerializeField] Button HomeButton = null;
        [SerializeField] Button OpenRecordingButton = null;
        [SerializeField] Button UploadRecordingButton = null;

        [Header("Panel Components")]
        [SerializeField] GameObject OptionsPanel = null;
        [SerializeField] GameObject OpenRecordingControls = null;
        [SerializeField] ScrollViewLoaderSelectable FileList = null;
        [SerializeField] TextMeshProUGUI OpenErrorText = null;
        [SerializeField] Button OpenFileButton = null;
        [SerializeField] GameObject UploadRecordingControls = null;
        [SerializeField] TMP_InputField PatientIDInput = null;
        [SerializeField] TMP_InputField AuthorIDInput = null;
        [SerializeField] TMP_InputField RecordingTitleInput = null;
        [SerializeField] TMP_InputField DescriptionInput = null;
        [SerializeField] TMP_InputField BodySiteInput = null;
        [SerializeField] TMP_InputField StartDateInput = null;
        [SerializeField] TMP_InputField StartTimeInput = null;
        [SerializeField] TextMeshProUGUI UploadErrorText = null;

        [Header("Playback Controls")]
        [SerializeField] CanvasGroup PlaybackControlsGroup = null;
        [SerializeField] PlaybackTimesManager timesManager;
        [SerializeField] TextMeshProUGUI FileNameText = null;
        [SerializeField] Slider PlaybackSlider;

        [Header("Playback Manager and Mesh")]
        [SerializeField] PlaybackManager playbackManager = null;
        [SerializeField] KinectMeshPlayback kinectMesh = null;

        [Header("UI Component Prefabs")]
        [SerializeField] GameObject DialogBox;
        [SerializeField] GameObject LoadingBar = null;

        public bool IsPlaying { get; private set; }

        private const string noRecordedFile = "empty";
        private const string loadingFileInfoMessage = "Please do not close the application. Extracting file information...";
        private const string loadingChunksMessage = "File found. Loading in initial data. Please do not close the application...";
        private const string loadingMeshMessage = "File loaded. Preparing mesh...";

        private enum PlaybackState
        {
            StartingUp,
            NoOpenFile,
            OpenFileSelected,
            DownloadingFile,
            OpeningFileInfo,
            OpeningFileData,
            PausedFile,
            PlayingFile,
            BufferingFile,
            UploadFileSelected,
            UploadingFile
        }

        private PlaybackState currentPlaybackState;
        private LoadingBarManager loadingManager;

        private IDictionary<string, TMP_InputField> uploadFileInputs;
        private string currentOpenFileName;
        private int totalDataItems;
        private int remainingDataItems;
        private bool fileInfoReady;

        void Start()
        {
            // First create a dictionary of the upload file input fields to make accessing them
            // as a group easier throughout the lifetime of the class
            uploadFileInputs = new Dictionary<string, TMP_InputField>();
            uploadFileInputs.Add("patientId", PatientIDInput);
            uploadFileInputs.Add("authorId", AuthorIDInput);
            uploadFileInputs.Add("recordingTitle", RecordingTitleInput);
            uploadFileInputs.Add("description", DescriptionInput);
            uploadFileInputs.Add("bodySite", BodySiteInput);
            uploadFileInputs.Add("startDate", StartDateInput);
            uploadFileInputs.Add("startTime", StartTimeInput);

            // Then if the player prefs contains a key "fileName", the scene is being passed
            // a new recording from the recording scene
            string fileName = PlayerPrefs.GetString("fileName", noRecordedFile);
            if (!fileName.Equals(noRecordedFile))
            {
                // In this case, store the file name and delete the key from player prefs
                currentOpenFileName = fileName;
                PlayerPrefs.DeleteKey("fileName");
            }

            // Change the state to indicate that no file has been opened
            SetPlaybackState(PlaybackState.NoOpenFile);
        }

        void Update()
        {
            if (currentOpenFileName != null && currentPlaybackState == PlaybackState.NoOpenFile)
            {
                // Change the state to indicate that the file is opening
                SetPlaybackState(PlaybackState.OpeningFileInfo);
            }
            else if (currentPlaybackState == PlaybackState.OpeningFileInfo && fileInfoReady)
            {
                // If the file info is ready but the state has not transitioned, perform
                // the state change
                loadingManager.Progress();
                SetPlaybackState(PlaybackState.OpeningFileData);
            }
            else if (currentPlaybackState == PlaybackState.OpeningFileData)
            { 
                // If the playback manager is still loading the initial chunks, check
                // if the loading bar can be updated
                if (playbackManager.IsLoading)
                {
                    int lastRemainingItems = loadingManager.RemainingDataItems;
                    int actualRemainingItems = playbackManager.GetDataQueueCount();

                    // If the number of items in the processing queue has changed, update the
                    // loading bar progress
                    if (lastRemainingItems != actualRemainingItems)
                    {
                        // Update its progress by the difference between the remaining item values
                        for (int i = 0; i < (lastRemainingItems - actualRemainingItems); i++)
                        {
                            loadingManager.Progress();
                        }
                    }
                }
                else
                {
                    // Change the loading message to indicate that the last step is initialising the mesh
                    loadingManager.ChangeLoadingMessage(loadingMeshMessage);

                    // Call the method to show and initialise the mesh
                    kinectMesh.ShowAndInitialiseMesh();

                    // Destroy and dereference the loading bar if it exists
                    if (loadingManager != null)
                    {
                        loadingManager.DestroyLoading();
                        loadingManager = null;
                    }

                    // Change the playback state to paused
                    SetPlaybackState(PlaybackState.PausedFile);
                }
            }
        }

        public void GoToMainMenuScene()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OpenFileOptionsPanel()
        {
            SetPlaybackState(PlaybackState.OpenFileSelected);
        }

        public void OpenFile()
        {
            currentOpenFileName = FileList.GetSelectedFileName();
            SetPlaybackState(PlaybackState.OpeningFileInfo);
        }

        public void CloseOptionsPanel()
        {
            // If the panel is closed and a file name does not exists within the class, go
            // back to the no file state
            if (currentOpenFileName == null)
            {
                SetPlaybackState(PlaybackState.NoOpenFile);
            }
            else
            {
                // Otherwise, a file name is available and it has been opened, so go into the
                // paused state
                SetPlaybackState(PlaybackState.PausedFile);
            }

            // Reenable mouse drag, as the pointer exit event will not have been triggered
            OrbitalCamera.MouseMovementEnabled();
        }

        public void UploadFileOptionsPanel()
        {
            SetPlaybackState(PlaybackState.UploadFileSelected);
        }

        private void FileInfoLoadedHandler(object sender, EventArgs e)
        {
            // Flip the file info ready flag once the handler is triggered from the playback
            // manager
            fileInfoReady = true;
        }

        private void SetPlaybackState(PlaybackState newState)
        {
            // If the playback state has not changed, return
            if (currentPlaybackState == newState)
            {
                return;
            }

            // Otherwise, make changes to the UI based on the new state
            switch (newState)
            {
                // For the no open file state, disable all playback controls apart from the home button and the
                // open file button
                case PlaybackState.NoOpenFile:
                    SetUiStates(true, true, false, false, false, false, false);
                    break;
                // If the open file button is selected, open the panel with this set of controls visible and disable
                // the toolbar buttons apart from the home button. Also, disable the playback controls whilst the
                // panel is open
                case PlaybackState.OpenFileSelected:
                    SetUiStates(true, false, false, true, true, false, false);
                    if (playbackManager.IsPlaying) { playbackManager.StopPlaying(); }
                    break;
                // TODO : If the user has indicated that they want to download a file from the open file options??
                // (Maybe instantiate holorepo dialog box that disables main canvas and allows user to input patient
                // id, then updates scroll view and allows them to select an option, press download, wait for download
                // and then close dialog and open file)
                case PlaybackState.DownloadingFile:
                    break;
                // If the user has selected a file to open, disable all UI controls whilst it loads the file info
                case PlaybackState.OpeningFileInfo:
                    SetUiStates(false, false, false, false, false, false, false);

                    // Indicate to the playback manager that it should begin opening the specified file
                    // and add a event handler to the file info finished loading event
                    playbackManager.FileInfoFinishedLoading += FileInfoLoadedHandler;
                    playbackManager.OpenFile(currentOpenFileName);

                    // Instantiate a loading bar prefab and get a reference to its manager class
                    GameObject loadingBar = Instantiate(LoadingBar, gameObject.transform) as GameObject;
                    loadingManager = loadingBar.GetComponent<LoadingBarManager>();

                    // Then, initialise it with 1 data item and the loading file info message before showing
                    // it in the canvas
                    loadingManager.InitialiseLoading(loadingFileInfoMessage, 1);
                    loadingManager.ShowLoading();

                    // Finally, set the file info ready flag to false
                    fileInfoReady = false;
                    break;
                // If the file info has been loaded in and chunk data is now being loaded in, disable all UI
                // controls whilst this happens
                case PlaybackState.OpeningFileData:
                    SetUiStates(false, false, false, false, false, false, false);

                    // An instance of the loading bar should be available with a reference to its manager
                    // cached, so reinitialise the loading bar with the new chunk queue data
                    loadingManager.InitialiseLoading(loadingChunksMessage, playbackManager.GetDataQueueCount());
                    loadingManager.ShowLoading();

                    // Remove the file info finished loading event handler now that it has been triggered
                    playbackManager.FileInfoFinishedLoading -= FileInfoLoadedHandler;
                    break;
                // If the user has opened a file and they are playing it or they have paused it, everything will be enabled
                // apart from the options panel
                case PlaybackState.PausedFile:
                    SetUiStates(true, true, true, false, false, false, true);
                    break;
                case PlaybackState.PlayingFile:
                    SetUiStates(true, true, true, false, false, false, true);
                    break;
                // If the file is buffering, everything should be disabled to prevent errors from occurring
                case PlaybackState.BufferingFile:
                    SetUiStates(false, false, false, false, false, false, false);
                    break;
                // If the upload file button is selected, open the panel with this set of controls visible and disable
                // the toolbar buttons apart from the home button. Also, disable the playback controls whilst the
                // panel is open
                case PlaybackState.UploadFileSelected:
                    SetUiStates(true, false, false, true, false, true, false);
                    playbackManager.StopPlaying();
                    break;
                // TODO : If the user is uploading a file??
                // (Maybe instantiate loading bar box that disables main canvas and tracks progress of upload, then when it
                // completes, destroy loading bar and show dialog box that indicates it has completed, so it can just be
                // dismissed or confirmed?)
                case PlaybackState.UploadingFile:
                    break;
            }

            // Store the new state
            currentPlaybackState = newState;
        }

        private void SetUiStates(bool homeButton, bool openRecordingButton, bool uploadRecordingButton,
            bool optionsPanel, bool openRecordingControls, bool uploadRecordingControls, bool playbackControls)
        {
            // Set the visibility and state of all the main UI components based on the input values
            if (HomeButton.interactable != homeButton) { HomeButton.interactable = homeButton; }
            if (OpenRecordingButton.interactable != openRecordingButton) { OpenRecordingButton.interactable = openRecordingButton; }
            if (UploadRecordingButton.interactable != uploadRecordingButton) { UploadRecordingButton.interactable = uploadRecordingButton; }

            // The inputs to the panel visibility function must be all false or one of the recording control sets must be false
            // and the other true if the options panel is visible
            if ((!optionsPanel && !openRecordingControls && !uploadRecordingButton) || (openRecordingControls == !uploadRecordingButton))
            {
                SetPanelVisibilityAndControls(optionsPanel, openRecordingControls, uploadRecordingControls);
            }
            else
            {
                throw new ArgumentException("Open controls and upload controls cannot be enabled at the same time");
            }

            // Set the state of the playback controls group
            SetPlaybackControlsState(playbackControls);
        }

        private void SetPanelVisibilityAndControls(bool panelVisibility, bool openRecordings, bool uploadRecordings)
        {
            if (panelVisibility)
            {
                if (openRecordings)
                {
                    // The panel should be visible and in open file mode
                    if (!OptionsPanel.activeSelf || !OpenRecordingControls.activeSelf)
                    {
                        OptionsPanel.SetActive(true);
                        UploadRecordingControls.SetActive(false);
                        OpenRecordingControls.SetActive(true);
                        OpenErrorText.enabled = false;
                        FileList.LoadFileNames();
                    }
                }
                else if (uploadRecordings)
                {
                    // The panel should be visible and in upload file mode
                    if (!OptionsPanel.activeSelf || !UploadRecordingControls.activeSelf)
                    {
                        OptionsPanel.SetActive(true);
                        OpenRecordingControls.SetActive(false);
                        UploadRecordingControls.SetActive(true);
                        UploadErrorText.enabled = false;
                    }
                }
            }
            else
            {
                if (OptionsPanel.activeSelf)
                {
                    // If the panel is visible and in open file mode, clean up all components and
                    // hide them
                    if (OpenRecordingControls.activeSelf)
                    {
                        OpenErrorText.enabled = false;
                        FileList.ClearCurrentContent();
                        OpenRecordingControls.SetActive(false);
                        OptionsPanel.SetActive(false);
                    }
                    else if (UploadRecordingControls.activeSelf)
                    {
                        // Otherwise, if the panel is visible and in upload file mode, do the same,
                        // but first clear all of the text from the input fields
                        UploadErrorText.enabled = false;
                        foreach (KeyValuePair<string, TMP_InputField> input in uploadFileInputs)
                        {
                            input.Value.text = "";
                        }
                        UploadRecordingControls.SetActive(false);
                        OptionsPanel.SetActive(false);
                    }
                }
            }
        }

        private void SetPlaybackControlsState(bool isInteractable)
        {
            if (isInteractable)
            {
                // Enable the playback controls and file name, setting its value
                PlaybackControlsGroup.interactable = true;
                FileNameText.enabled = true;
                FileNameText.text = "File: " + (currentOpenFileName ?? "Unknown File");
            }
            else
            {
                // Otherwise, disable the file name and playback controls
                FileNameText.enabled = false;
                PlaybackControlsGroup.interactable = false;
            }
        }
    }
}
