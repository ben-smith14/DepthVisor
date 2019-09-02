using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DepthVisor.UI
{
    public class SliderManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI CountUpTimer = null;
        [SerializeField] Slider PlaybackSlider = null;
        [SerializeField] TextMeshProUGUI CountDownTimer = null;

        private bool isPlaying;
        private float timerCountUp;
        private float recordingLength;

        void Start()
        {
            isPlaying = false;
            timerCountUp = 0.0f;
            recordingLength = 0.0f;
        }

        void Update()
        {
            if (isPlaying)
            {
                timerCountUp += Time.deltaTime;

                // If the timer count has reached or exceeded the recording length, stop playing and
                // set all of the components to their max values
                if (timerCountUp >= recordingLength)
                {
                    StopPlaying();

                    CountUpTimer.text = RecordingTimerManager.TimeFloatToString(recordingLength);
                    PlaybackSlider.value = PlaybackSlider.maxValue;
                    CountDownTimer.text = RecordingTimerManager.TimeFloatToString(0.0f);
                }

                // Otherwise, simply set the text components and slider value
                CountUpTimer.text = RecordingTimerManager.TimeFloatToString(timerCountUp);
                PlaybackSlider.value = timerCountUp / recordingLength;
                CountDownTimer.text = RecordingTimerManager.TimeFloatToString(recordingLength - timerCountUp);
            }
        }

        public void InitialiseSlider(float recordingLength)
        {
            // Reset all internal variables
            isPlaying = false;
            timerCountUp = 0.0f;
            this.recordingLength = recordingLength;

            // Initialise the text and slider components to their minimum values
            CountUpTimer.text = RecordingTimerManager.TimeFloatToString(0.0f);
            PlaybackSlider.value = PlaybackSlider.minValue;
            CountUpTimer.text = RecordingTimerManager.TimeFloatToString(recordingLength);
        }

        public void StartPlaying()
        {
            if (recordingLength == 0.0f)
            {
                throw new UnassignedReferenceException("Recording length has not been set");
            }

            isPlaying = true;
        }

        public void StopPlaying()
        {
            isPlaying = false;
        }
    }
}
