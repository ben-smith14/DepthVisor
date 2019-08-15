using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

using DepthVisor.Recording;
using DepthVisor.Kinect;

namespace DepthVisor.UI
{
    public class CanvasManager : MonoBehaviour
    {
        [Header("Main Camera")]
        [SerializeField] CameraOrbitControls OrbitalCamera;

        [Header("Home Button")]
        [SerializeField] Button HomeButton;

        [Header("Panel Components")]
        [SerializeField] GameObject OptionsPanel;
        [SerializeField] TMP_InputField FileNameInput;
        [SerializeField] TextMeshProUGUI ErrorText;
        [SerializeField] ScrollViewLoader ScrollViewLoader;

        [Header("Recording UI Components")]
        [SerializeField] Button NewRecordingButton;
        [SerializeField] TextMeshProUGUI FileNameText;
        [SerializeField] GameObject RecordingButtonContainer;
        [SerializeField] Color32 RecordingOn = Color.red;
        [SerializeField] Color32 RecordingOnHighlight = new Color32(245, 0, 0, 255);
        [SerializeField] Color32 RecordingOnPressed = new Color32(200, 0, 0, 255);
        [SerializeField] string RecordingOnText = "STOP";
        [SerializeField] Color32 RecordingOnTextColour = Color.white;
        [SerializeField] TextMeshProUGUI TimerText;

        [Header("Mesh Object Container")]
        [SerializeField] GameObject KinectViewContainer;

        [Header("Dialog Box Prefab")]
        [SerializeField] GameObject DialogBox;

        [Header("Recording and File Access")]
        [SerializeField] GameObject RecordingAndFileManager;

        private static readonly Regex fileNamePattern = new Regex(@"^\w[\w\-]{2}\s?([\w\-]+\s?)*$", RegexOptions.Compiled);
        private const string overwriteFileMessage = "That file already exists. Do you want to overwrite it?";
        private const float borderFade = 127.5f;

        private enum RecordingState
        {
            StartingUp,
            NoData,
            NoFile,
            InputNewFile,
            FileReady,
            RecordingData,
            StopRecording
        }

        private RecordingState currentRecordingState;
        private KinectManager kinectManager;
        private RecordingManager recordingManager;
        private FileSystemManager fileManager;

        private Button recordingButton;
        private Image recordingButtonBorder;
        private TextMeshProUGUI recordingButtonText;
        private ColorBlock defaultButtonColours;
        private string defaultButtonText;
        private Color32 defaultButtonTextColur;

        private GameObject dialogBox;
        private string currentOpenFileName;

        void Start()
        {
            // Cache a reference to the kinect manager, the recording manager and the file
            // manager
            kinectManager = KinectViewContainer.GetComponentInChildren<KinectManager>();
            recordingManager = RecordingAndFileManager.GetComponent<RecordingManager>();
            fileManager = RecordingAndFileManager.GetComponent<FileSystemManager>();

            // Also cache references to the different components of the recording controls
            // container
            recordingButton = RecordingButtonContainer.GetComponentInChildren<Button>();
            recordingButtonBorder = RecordingButtonContainer.GetComponentInChildren<Image>();
            recordingButtonText = recordingButton.GetComponentInChildren<TextMeshProUGUI>();

            // Store the default values of these components for use later on
            defaultButtonColours = recordingButton.colors;
            defaultButtonText = recordingButtonText.text;
            defaultButtonTextColur = recordingButtonText.color;

            // Set the recording state to indicate that no data is coming in yet
            SetRecordingState(RecordingState.NoData);
        }

        void Update()
        {
            // If the system is still waiting for data from the kinect manager, set the recording
            // state to no data if not already done so and return
            if (!kinectManager.IsDataAvailable())
            {
                SetRecordingState(RecordingState.NoData);
                return;
            }

            // Otherwise, data is available, so if the current state indicates that the system is
            // waiting for data, the state can now be changed to no file
            if (currentRecordingState == RecordingState.NoData)
            {
                SetRecordingState(RecordingState.NoFile);
            }
            else if (currentRecordingState == RecordingState.StopRecording)
            {
                // Instantiate whatever kind of loading ui component I use in the state changer method
                // when stop recording is first set and if isprocessingfile is true; then, update it
                // here until I can change scene
                if (recordingManager.IsProcessingFile)
                {
                    // TODO : Update the loading screen of some kind until able to load playback
                    // scene
                }
                else
                {
                    // TODO : For testing, load main menu scene, but change to playback scene later; need
                    // to also pass the file name to the playback screen to open the file there
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }

        public void GoToMainMenuScene()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OpenNewRecordingPanel()
        {
            SetRecordingState(RecordingState.InputNewFile);
        }

        public void CloseNewRecordingPanel()
        {
            // If the panel is closed and a file name was not provided, go back to the
            // the no file state
            if (currentOpenFileName == null)
            {
                SetRecordingState(RecordingState.NoFile);
            }
            else
            {
                // Otherwise, a file name was given previously, so go back to the file
                // ready state
                SetRecordingState(RecordingState.FileReady);
            }

            // Reenable mouse drag, as the pointer exit event will not have been triggered
            OrbitalCamera.MouseMovementEnabled();
        }

        public void DoneNewRecordingPanel()
        {
            // A file name must start with an alphanumeric character and must then be
            // at least 3 characters long without any spaces. After this, it can include
            // spaces, but only one at a time. If the file name is valid, disable the error
            // message, disable the panel and reenable the new recording button
            string fileName = FileNameInput.text;
            if (fileNamePattern.IsMatch(fileName))
            {
                // If the file name already exists in the save directory, create an instance
                // of the dialog prefab and wait for its result
                if (fileManager.DoesFileExist(fileName))
                {
                    // Instantiate the dialog prefab, get its manager component, initialise the
                    // dialog, add a handler to the closing event and then show the dialog box,
                    // which will disable the main canvas group
                    dialogBox = Instantiate(DialogBox, gameObject.transform) as GameObject;
                    DialogManager dialogManager = dialogBox.GetComponent<DialogManager>();
                    dialogManager.InitialiseDialog(overwriteFileMessage);
                    dialogManager.DialogClosed += DialogClosedHandler;
                    dialogManager.ShowDialog();
                }
                else
                {
                    // Otherwise, simply create the directory if it doesn't exist and add the file,
                    // also storing the input file name 
                    fileManager.CreateSaveDirectoryIfNotExists();
                    fileManager.CreateOrOverwriteFile(fileName);
                    currentOpenFileName = fileName;

                    // Then also set the recording state to file ready and reenable mouse drag
                    SetRecordingState(RecordingState.FileReady);
                    OrbitalCamera.MouseMovementEnabled();
                }
            }
            else
            {
                // Otherwise, show the error message in the panel
                ErrorText.enabled = true;
            }
        }

        public void ToggleRecording()
        {
            // If the system is currently recording, the recording is being stopped
            if (currentRecordingState == RecordingState.RecordingData)
            {
                // Therefore, first stop the timer and change the recording state to
                // reflect this
                TimerText.GetComponent<TimerManager>().StopTimer();
                SetRecordingState(RecordingState.StopRecording);

                // TODO : PUT IN FOR TESTING (REMOVE LATER)
                HomeButton.interactable = true;
            }
            else
            {
                // Otherwise, the system is not yet recording, so start the timer and
                // set the recording state to recording
                TimerText.GetComponent<TimerManager>().StartTimer();
                SetRecordingState(RecordingState.RecordingData);
            }
        }

        private void DialogClosedHandler(object sender, DialogClosedEventArgs dialogArgs)
        {
            // If the dialog was successfully confirmed, the user wants to overwrite the
            // file, so do this
            if (dialogArgs.Success)
            {
                // Overwrite the file and store the file name
                string fileName = FileNameInput.text;
                fileManager.CreateOrOverwriteFile(fileName);
                currentOpenFileName = fileName;

                // Then, set the recording state to file ready and reenable mouse drag
                SetRecordingState(RecordingState.FileReady);
                OrbitalCamera.MouseMovementEnabled();
            }

            // Finally, dereference the soon to be destroyed dialog box
            dialogBox = null;
        }

        private void SetRecordingState(RecordingState newState)
        {
            // If the recording state has not changed, return
            if (currentRecordingState == newState)
            {
                return;
            }

            // Otherwise, make changes to the UI based on the new state
            switch (newState)
            {
                // For the no data state, disable all recording controls apart from the home button
                case RecordingState.NoData:
                    SetUiStates(true, false, false, false, false);
                    break;
                // If data is coming in but no file has been created, only enable the home button and
                // new recording button
                case RecordingState.NoFile:
                    SetUiStates(true, true, false, false, false);
                    break;
                // If the user has indicated that they wish to create a new file, disable all controls apart
                // from the home button and options panel
                case RecordingState.InputNewFile:
                    SetUiStates(true, false, true, false, false);
                    break;
                // If the user has created a file and is ready to record, disable the panel but reenable
                // everything else
                case RecordingState.FileReady:
                    SetUiStates(true, true, false, true, false);
                    break;
                // If the user has begun recording, disable everything apart from the recording controls and
                // change their appearence to indicate recording
                case RecordingState.RecordingData:
                    SetUiStates(false, false, false, true, true);

                    // Also indicate to the recording manager that it should start recording data
                    recordingManager.StartRecording(currentOpenFileName);
                    break;
                // Once the user has stopped recording, keep everything disabled but also change the recording
                // button state and then disable it as well
                case RecordingState.StopRecording:
                    SetUiStates(false, false, false, false, false);

                    // Also indicate to the recording manager that it should stop recording data
                    recordingManager.StopRecording();
                    break;
            }

            // Store the new state
            currentRecordingState = newState;
        }

        private void SetUiStates(bool homeButton, bool newRecordingButton, bool optionsPanel, bool recordingControls, bool isRecording)
        {
            // Set the visibility and state of all the main UI components based on the input values
            if (HomeButton.interactable != homeButton) { HomeButton.interactable = homeButton; }
            if (NewRecordingButton.interactable != newRecordingButton) { NewRecordingButton.interactable = newRecordingButton; }

            SetPanelVisibility(optionsPanel);

            SetRecordingControlsVisibility(recordingControls);
            SetRecordingControlsState(isRecording);
        }

        private void SetPanelVisibility(bool visible)
        {
            if (visible)
            {
                // If the panel is currently not visible already, disable the error text,
                // then open the panel and load in the file items to the scroll view
                if (!OptionsPanel.activeSelf)
                {
                    OptionsPanel.SetActive(true);
                    ErrorText.enabled = false;
                    ScrollViewLoader.LoadFileNames();
                }
            }
            else
            {
                // Otherwise, if the panel is visible, disable the error text once again
                // and clear the scroll view children, also disabling the panel itself
                if (OptionsPanel.activeSelf)
                {
                    ErrorText.enabled = false;
                    ScrollViewLoader.ClearCurrentContent();
                    OptionsPanel.SetActive(false);
                }
            }
        }

        private void SetRecordingControlsVisibility(bool visible)
        {
            if (visible)
            {
                // If the recording button is not interactable already, enable the file name text
                // and set it based on whether the current open file name is null or not. Then,
                // enable the recording button and unfade its border
                if (!recordingButton.interactable)
                {
                    FileNameText.enabled = true;
                    FileNameText.text = "File: " + (currentOpenFileName ?? "Unknown File");

                    recordingButton.interactable = true;
                    recordingButtonBorder.color += new Color(0, 0, 0, borderFade);
                }
            }
            else
            {
                // Otherwise, if the recording button is interactable, disable the file
                // name text as well as the button, then fade its border by setting the
                // alpha value
                if (recordingButton.interactable)
                {
                    FileNameText.enabled = false;
                    recordingButton.interactable = false;
                    recordingButtonBorder.color -= new Color(0, 0, 0, borderFade);
                }
            }
        }

        private void SetRecordingControlsState(bool recordingOn)
        {
            // Deselect the recording controls button in the event system
            EventSystem.current.SetSelectedGameObject(null);

            // If the recording is on, set the button's colour block and text
            // to the recording state parameters
            if (recordingOn)
            {
                ColorBlock buttonColors = recordingButton.colors;
                buttonColors.normalColor = RecordingOn;
                buttonColors.highlightedColor = RecordingOnHighlight;
                buttonColors.pressedColor = RecordingOnPressed;
                recordingButton.colors = buttonColors;

                recordingButtonText.text = RecordingOnText;
                recordingButtonText.color = RecordingOnTextColour;
            }
            else
            {
                // Otherwise, set the button back to its defaults to indicate that
                // it is not recording
                recordingButton.colors = defaultButtonColours;
                recordingButtonText.text = defaultButtonText;
                recordingButtonText.color = defaultButtonTextColur;
            }
        }
    }
}
