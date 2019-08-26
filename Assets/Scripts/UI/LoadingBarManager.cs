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

            // If the remaining data items are 0, simply update the UI components to
            // 100%. Otherwise, as the remaining data items will be decreasing, take
            // the percentage away from 100 to get the remaining percentage
            if (RemainingDataItems == 0)
            {
                UpdateLoading(100.0f);
            }
            else
            {
                UpdateLoading(100.0f - ((float)RemainingDataItems / totalDataItems * 100.0f));
            }
        }

        public void DestroyLoading()
        {
            // Destroy the loading bar element
            Destroy(gameObject);
        }

        private void UpdateLoading(float percentageComplete)
        {
            // If the percentage complete is 0 or 100 exactly, set the UI
            // components manually to avoid any division issues with floats
            if (percentageComplete == 0.0f)
            {
                LoadingSlider.value = LoadingSlider.minValue;
                LoadingPercentage.text = "0%";
            }
            else if (percentageComplete == 100.0f)
            {
                LoadingSlider.value = LoadingSlider.maxValue;
                LoadingPercentage.text = "100%";
            }
            else if (0.0f < percentageComplete && percentageComplete < 100.0f)
            {
                // Otherwise, if in the valid range, set the loading bar value and
                // percentage text based on the normalised value
                LoadingSlider.value = (int)percentageComplete;
                LoadingPercentage.text = (int)percentageComplete + "%";
            }
            else
            {
                throw new System.ArgumentOutOfRangeException("Invalid percentage complete; must be in the range 0.0 - 100.0");
            }
        }
    }
}
