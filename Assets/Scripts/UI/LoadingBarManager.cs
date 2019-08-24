using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DepthVisor.UI
{
    public class LoadingBarManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI LoadingMessage = null;
        [SerializeField] Slider LoadingSlider = null;
        [SerializeField] TextMeshProUGUI LoadingPercentage = null;

        public int RemainingDataItems { get; private set; }
        private int totalDataItems;

        public void InitialiseLoading(string message, int totalDataItems)
        {
            // Hide the loading bar when first created and set all of the text fields
            gameObject.SetActive(false);
            LoadingMessage.text = message;
            LoadingPercentage.text = "0%";

            // Then set the slider to its initial position and initialise the data item values
            LoadingSlider.value = 0.0f;
            RemainingDataItems = this.totalDataItems = totalDataItems;
        }

        public void ShowLoading()
        {
            gameObject.SetActive(true);
        }

        public void ChangeLoadingMessage(string newMessage)
        {
            LoadingMessage.text = newMessage;
        }

        public void Progress()
        {
            // Progress the loading bar by 1 value
            RemainingDataItems--;

            // Update the UI components. As the remaining data items will be decreasing,
            // take the percentage away from 100 to get the remaining percentage
            UpdateLoading(100.0f - ((float)RemainingDataItems / totalDataItems * 100.0f));
        }

        public void DestroyLoading()
        {
            // Destroy the loading bar element
            Destroy(gameObject);
        }

        private void UpdateLoading(float percentageComplete)
        {
            // Validate the input and throw an exception if invalid
            if (0.0f <= percentageComplete && percentageComplete <= 100.0f)
            {
                // If valid, set the loading bar value and the percentage text
                LoadingSlider.value = percentageComplete / 100;
                LoadingPercentage.text = (int)percentageComplete + "%";
            }
            else
            {
                throw new System.ArgumentOutOfRangeException("Invalid percentage complete; must be in the range 0.0 - 100.0");
            }
        }
    }
}
