using Cinemachine;
using UnityEngine;
namespace UnityDemo
{
    public class CharactorSelecter : MonoBehaviour
    {
        [SerializeField]
        GameObject[] _characters;
        [SerializeField]
        PlayerCharactorDefines charactorDefines;
        public int _selectedIndex = 0;
        [SerializeField] CameraController[] cameraControllers;

        private void Start()
        {
            cameraControllers = new CameraController[_characters.Length];
            for (int i = 0; i < _characters.Length; i++)
            {
                CameraController cameraController = _characters[i].GetComponentInChildren<CameraController>();
                cameraController.SetCameraMode(CameraController.CameraMode.Disable);
                cameraControllers[i] = cameraController;
            }
            cameraControllers[_selectedIndex].SetCameraMode(CameraController.CameraMode.CharactorSelect);
        }

        public void OnNextClicked()
        {
            var currentCamera = cameraControllers[_selectedIndex];
            int nextIndex = (_selectedIndex + 1 + _characters.Length) % _characters.Length;
            var nextCamera = cameraControllers[nextIndex];
            currentCamera.SetCameraMode( CameraController.CameraMode.Disable);
            nextCamera.SetCameraMode(CameraController.CameraMode.CharactorSelect);
            _selectedIndex = nextIndex;
        }

        public void OnPrevClicked()
        {
            int nextIndex = (_selectedIndex - 1 + _characters.Length) % _characters.Length;
            var currentCamera = cameraControllers[_selectedIndex];
            var nextCamera = cameraControllers[nextIndex];
            currentCamera.SetCameraMode(CameraController.CameraMode.Disable);
            nextCamera.SetCameraMode(CameraController.CameraMode.CharactorSelect);
            _selectedIndex = nextIndex;

        }
    }
}