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
        [SerializeField] Button RecordingButton;
        [SerializeField] TextMeshProUGUI TimerText;
        [SerializeField] GameObject KinectMeshContainer;

        // TODO: Serialize the colours to expose in editor
        private Color32 redHighlight = new Color32(245, 0, 0, 255);
        private Color32 redPressed = new Color32(200, 0, 0, 255);
        private Color32 whiteHighlight = new Color32(245, 245, 245, 255);
        private Color32 whitePressed = new Color32(200, 200, 200, 255);

        private bool isRecording;
        private KinectRecordingStore recordedData;
        private KinectMeshGenerator kinectMesh;

        void Start()
        {
            kinectMesh = KinectMeshContainer.GetComponentInChildren<KinectMeshGenerator>();
            // recordedData = new KinectRecordingStore();
            isRecording = false;
        }

        void Update()
        {
            if (isRecording)
            {
                // recordedData.AddFrame();
            }
        }

        public void ToggleRecording()
        {
            if (isRecording)
            {
                TimerText.GetComponent<TimerManager>().StopTimer();

                // Deselect the button and switch its colour block back to the normal values
                EventSystem.current.SetSelectedGameObject(null);
                ColorBlock buttonColors = RecordingButton.colors;
                buttonColors.normalColor = Color.white;
                buttonColors.highlightedColor = whiteHighlight;
                buttonColors.pressedColor = whitePressed;
                RecordingButton.colors = buttonColors;

                // TODO : Change button to text mesh pro
                Text buttonText = RecordingButton.GetComponentInChildren<Text>();
                buttonText.text = "Start Recording";
                buttonText.color = new Color32(50, 50, 50, 255);

                // Kinect Recording Serialize and Save, then reset reference
            }
            else
            {
                TimerText.GetComponent<TimerManager>().StartTimer();

                // Deselect the button and switch its colour block to the red versions of the normal values
                EventSystem.current.SetSelectedGameObject(null);
                ColorBlock buttonColors = RecordingButton.colors;
                buttonColors.normalColor = Color.red;
                buttonColors.highlightedColor = redHighlight;
                buttonColors.pressedColor = redPressed;
                RecordingButton.colors = buttonColors;

                Text buttonText = RecordingButton.GetComponentInChildren<Text>();
                buttonText.text = "STOP";
                buttonText.color = Color.white;
            }

            isRecording = !isRecording;
        }
    }
}
