using UnityEngine;
using TMPro;

namespace DepthVisor.UI
{
    public class RecordingTimerManager : MonoBehaviour
    {
        private bool timerOn;
        public float TimerCount { get; private set; }

        void Start()
        {
            timerOn = false;
            TimerCount = 0.0f;
        }

        void Update()
        {
            if (timerOn)
            {
                TimerCount += Time.deltaTime;

                // Format the text input so that each component always displays two digits
                gameObject.GetComponent<TextMeshProUGUI>().text = TimeFloatToString(TimerCount);
            }
        }

        public void StartTimer()
        {
            timerOn = true;
        }

        public void StopTimer()
        {
            timerOn = false;
        }

        public static string TimeFloatToString(float timeFloat)
        {
            int seconds = (int)timeFloat % 60;
            int mins = (int)timeFloat / 60;
            int hours = (int)timeFloat / 3600;

            // Format the text inputs so that each component always displays two digits
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, seconds);
        }
    }
}
