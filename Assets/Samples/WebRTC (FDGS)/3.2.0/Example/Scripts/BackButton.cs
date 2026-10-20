using UnityEngine;
#if ENABLE_INPUT_SYSTEM && WEBRTC_SAMPLES_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unity.WebRTC.Samples
{
    internal class BackButton : MonoBehaviour
    {
        [SerializeField]
        GameObject m_BackButton;
        public GameObject backButton
        {
            get => m_BackButton;
            set => m_BackButton = value;
        }

        void Start()
        {
            if (SampleScenes.CanLoad(SampleScenes.MenuScene))
            {
                m_BackButton.SetActive(true);
            }
        }

        void Update()
        {
            if (EscapePressed())
            {
                BackButtonPressed();
            }
        }

        public void BackButtonPressed()
        {
            if (SampleScenes.CanLoad(SampleScenes.MenuScene))
            {
                SampleScenes.Load(SampleScenes.MenuScene);
            }
        }

        static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM && WEBRTC_SAMPLES_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
