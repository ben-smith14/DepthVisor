using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DepthVisor.UI
{
    public class TimerManager : MonoBehaviour
    {
        private bool timerOn;
        private float timerCount;

        void Start()
        {
            timerOn = false;
        }

        void Update()
        {
            if (timerOn)
            {
                timerCount += Time.deltaTime;

                int seconds = (int)timerCount % 60;
                int mins = (int)timerCount / 60;

                // Format the text input so that each component always displays two digits
                gameObject.GetComponent<TextMeshProUGUI>().text = string.Format("{0:00}:{1:00}", mins, seconds);
            }
        }

        public void StartTimer()
        {
            timerOn = true;
        }

        public void StopTimer()
        {
            timerOn = false;
            timerCount -= timerCount;
        }
    }
}
