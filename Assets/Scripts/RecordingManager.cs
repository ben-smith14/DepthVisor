using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

using DepthVisor.UI;
using DepthVisor.Kinect;

namespace DepthVisor.Recording
{
    public class RecordingManager : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] Canvas UiCanvas;

        [Header("Recording Button")]
        [SerializeField] GameObject RecordingButtonContainer;
        [SerializeField] Color32 RecordingOn = Color.red;
        [SerializeField] Color32 RecordingOnHighlight = new Color32(245, 0, 0, 255);
        [SerializeField] Color32 RecordingOnPressed = new Color32(200, 0, 0, 255);
        [SerializeField] string RecordingOnText = "STOP";
        [SerializeField] Color32 RecordingOnTextColour = Color.white;

        [Header("Recording Timer Text")]
        [SerializeField] TextMeshProUGUI TimerText;

        [Header("Mesh Objects")]
        [SerializeField] GameObject KinectViewContainer;

        private KinectManager kinectManager;
        private KinectMeshGenerator kinectMesh;
        private KinectRecordingStore recordedData;
        private FileSystemManager fileManager;

        private Button recordingButton;
        private Image recordingButtonBorder;
        private TextMeshProUGUI recordingButtonText;
        private ColorBlock defaultButtonColours;
        private string defaultButtonText;
        private Color32 defaultButtonTextColur;

        private bool isRecording;
        private const float borderFade = 127.5f;

        void Start()
        {
            // Cache references to the kinect manager and the mesh filter, which contains
            // the vertex and uv mapping data
            kinectManager = KinectViewContainer.GetComponentInChildren<KinectManager>();
            kinectMesh = KinectViewContainer.GetComponentInChildren<KinectMeshGenerator>();

            // Then cache references to the button, its border and its text
            recordingButton = RecordingButtonContainer.GetComponentInChildren<Button>();
            recordingButtonBorder = RecordingButtonContainer.GetComponentInChildren<Image>();
            recordingButtonText = recordingButton.GetComponentInChildren<TextMeshProUGUI>();

            // Store the default colours and text of the button in the not recording state
            defaultButtonColours = recordingButton.colors;
            defaultButtonText = recordingButtonText.text;
            defaultButtonTextColur = recordingButtonText.color;

            // Disable the recording button on start so that recording can only begin once
            // the system is receiving valid data from the Kinect. Also, decrease the alpha
            // value on the border to match this new state
            recordingButton.interactable = false;
            recordingButtonBorder.color -= new Color(0, 0, 0, borderFade);

            // Initialise the recording store object
            // TODO : Only do this if one doesn't already exist
            recordedData = new KinectRecordingStore();
            fileManager = new FileSystemManager();
            fileManager.SerializationFinished += ToggleCanvasInteract;

            // Initialise recording as off
            isRecording = false;
        }

        void Update()
        {
            // If the system is still waiting for data from the kinect manager, disable the
            // recording button if not already done so and return
            if (!kinectManager.IsDataAvailable())
            {
                if (recordingButton.interactable)
                {
                    recordingButton.interactable = false;
                    recordingButtonBorder.color -= new Color(0, 0, 0, borderFade);
                }

                return;
            }

            // Otherwise, enable the recording button if disabled
            if (!recordingButton.interactable)
            {
                recordingButton.interactable = true;
                recordingButtonBorder.color += new Color(0, 0, 0, borderFade);
            }

            // If currently recording, add the mesh data to a frame in the
            // recorded data container
            if (isRecording)
            {
                recordedData.AddFrame(
                    kinectMesh.ReadVertices(),
                    kinectManager.ColourTexture,
                    kinectMesh.ReadUvs()
                );
            }
        }

        private void ToggleCanvasInteract(object sender, EventArgs e)
        {
            CanvasGroup canvasGroup = UiCanvas.GetComponent<CanvasGroup>();
            if (!canvasGroup.interactable)
            {
                canvasGroup.interactable = true;
            }
        }

        public void ToggleRecording()
        {
            // If the system is currently recording, change all of the UI elements
            // to their not recording state
            if (isRecording)
            {
                // Disable the entire canvas whilst serialization of the file occurs,
                // stop the timer and set the recording button to the not recording
                // state
                UiCanvas.GetComponent<CanvasGroup>().interactable = false;
                TimerText.GetComponent<TimerManager>().StopTimer();
                SetButtonState(false);

                // TODO : Switch to playback viewer, retaining the kinect recording object;
                // Kinect Recording Serialize and Save, then reset reference
                fileManager.SerializeAndSave(recordedData, "testFile");

                // Re-enable the canvas
                UiCanvas.GetComponent<CanvasGroup>().interactable = true;
            }
            else
            {
                // Otherwise, the system is not recording, so start the timer and
                // set the recording button to the recording state
                TimerText.GetComponent<TimerManager>().StartTimer();
                SetButtonState(true);
            }

            // Flip the recording flag
            isRecording = !isRecording;
        }

        private void SetButtonState(bool recordingOn)
        {
            // Deselect the button in the event system
            EventSystem.current.SetSelectedGameObject(null);

            // Set its colour block and text to the recording state if the
            // recording is on
            if (recordingOn)
            {
                ColorBlock buttonColors = recordingButton.colors;
                buttonColors.normalColor = RecordingOn;
                buttonColors.highlightedColor = RecordingOnHighlight;
                buttonColors.pressedColor = RecordingOnPressed;
                recordingButton.colors = buttonColors;

                recordingButtonText.text = RecordingOnText;
                recordingButtonText.color = RecordingOnTextColour;
            } else
            {
                // Reset button back to defaults when not recording
                recordingButton.colors = defaultButtonColours;
                recordingButtonText.text = defaultButtonText;
                recordingButtonText.color = defaultButtonTextColur;
            }
        }
    }
}
