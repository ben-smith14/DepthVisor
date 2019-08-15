using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using DepthVisor.Recording;

namespace DepthVisor.UI
{
    public class ScrollViewLoader : MonoBehaviour
    {
        [SerializeField] FileSystemManager fileManager;
        [SerializeField] GameObject NoFilesPrefab;
        [SerializeField] GameObject FileItemPrefab;

        public void LoadFileNames()
        {
            // Clear all existing children if any
            ClearCurrentContent();

            // Ensure that the save directory exists and get the list of files within it
            fileManager.CreateSaveDirectoryIfNotExists();
            IEnumerable<string> fileList = fileManager.GetFileList();

            // If there are no files, add an instance of the no files prefab to the scroll
            // view content and return
            if (!fileList.Any())
            {
                GameObject noFiles = Instantiate(NoFilesPrefab, gameObject.transform) as GameObject;
                noFiles.SetActive(true);
                return;
            }

            // Otherwise, add an instance of the file item prefab to the scroll view content
            // for every DepthVisor file in the saves folder
            foreach (string fileName in fileList)
            {
                GameObject fileItem = Instantiate(FileItemPrefab, gameObject.transform) as GameObject;
                fileItem.SetActive(true);
                fileItem.GetComponent<FileItem>().SetFileName(fileName);
            }
        }

        public void ClearCurrentContent()
        {
            // Clear all child objects from the scroll view content parent
            foreach (Transform child in gameObject.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
