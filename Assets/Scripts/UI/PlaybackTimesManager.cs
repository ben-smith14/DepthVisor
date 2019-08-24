using System;

using UnityEngine;
using TMPro;

namespace DepthVisor.UI
{
    public class PlaybackTimesManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI CountUpTimer = null;
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

                // If the timer count has exceeded or reached the recording length, stop playing and
                // set the timer text components to their max values
                if (timerCountUp >= recordingLength)
                {
                    StopPlaying();
                    CountUpTimer.text = RecordingTimerManager.TimeFloatToString(recordingLength);
                    CountDownTimer.text = RecordingTimerManager.TimeFloatToString(0.0f);
                }

                // Otherwise, simply format the text components so that each item always displays two
                // digits for each unit of time
                CountUpTimer.text = RecordingTimerManager.TimeFloatToString(timerCountUp);
                CountDownTimer.text = RecordingTimerManager.TimeFloatToString(recordingLength - timerCountUp);
            }
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

        public void SetTimerPositions(float time)
        {
            if (recordingLength == 0.0f)
            {
                throw new UnassignedReferenceException("Recording length has not been set");
            }
            else if (time > recordingLength)
            {
                throw new ArgumentOutOfRangeException("Input time exceeds the total recording length");
            }

            timerCountUp = time;

            // Format the text input so that each component always displays two digits
            CountUpTimer.text = RecordingTimerManager.TimeFloatToString(timerCountUp);
            CountDownTimer.text = RecordingTimerManager.TimeFloatToString(recordingLength - timerCountUp);
        }

        public void InitialiseTimers(float recordingLength)
        {
            isPlaying = false;
            timerCountUp = 0.0f;
            this.recordingLength = recordingLength;
        }

        public void ResetTimers()
        {
            isPlaying = false;
            timerCountUp = 0.0f;
            recordingLength = 0.0f;
        }

        public void SetRecordingLength(float recordingLength)
        {
            this.recordingLength = recordingLength;
        }
    }
}
