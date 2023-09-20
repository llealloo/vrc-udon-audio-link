using UnityEngine;
using UnityEngine.UI;

namespace AudioLink
{
#if UDONSHARP
    using UdonSharp;

    using VRC.SDKBase;

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ThemeColorController : UdonSharpBehaviour
#else
    public class ThemeColorController : MonoBehaviour
#endif
    {
        [UdonSynced] private int _themeColorMode;

        [UdonSynced]
        public Color[] customThemeColors = {
                Color.yellow,
                Color.blue,
                Color.red,
                Color.green
            };

        public AudioLink audioLink; // Initialized by AudioLinkController.

        public Dropdown themeColorDropdown; // Initialized from prefab.

        private Color[] _initCustomThemeColors;
        private int _initThemeColorMode; // Initialized from themeColorDropdown.

        private bool _processGUIEvents = true;

#if UDONSHARP
        private VRCPlayerApi localPlayer;
#endif

        // A view-controller for customThemeColors
        public Transform extensionCanvas;
        public Slider sliderHue;
        public Slider sliderSaturation;
        public Slider sliderValue;
        public Transform[] customColorLassos;
        public int customColorIndex = 0;

        private void Start()
        {
#if UDONSHARP
            localPlayer = Networking.LocalPlayer;
#endif

            _initCustomThemeColors = new[] {
                    customThemeColors[0],
                    customThemeColors[1],
                    customThemeColors[2],
                    customThemeColors[3],
                };

            _initThemeColorMode = themeColorDropdown.value;
            _themeColorMode = _initThemeColorMode;

            _UpdateGUI();
            _UpdateAudioLinkThemeColors();
#if UDONSHARP
            if (Networking.IsOwner(gameObject))
                RequestSerialization();
#endif
        }

#if UDONSHARP
        public override void OnDeserialization()
        {
            _UpdateGUI();
            _UpdateAudioLinkThemeColors();
        }
#endif

        public void _SelectCustomColor0() { _SelectCustomColorN(0); }
        public void _SelectCustomColor1() { _SelectCustomColorN(1); }
        public void _SelectCustomColor2() { _SelectCustomColorN(2); }
        public void _SelectCustomColor3() { _SelectCustomColorN(3); }
        public void _SelectCustomColorN(int n)
        {
            customColorIndex = n;
            _UpdateGUI();
        }

        public void _OnGUIChange()
        {
            if (!_processGUIEvents)
            {
                return;
            }
#if UDONSHARP
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(localPlayer, gameObject);
#endif
            bool modeChanged = _themeColorMode != themeColorDropdown.value;
            _themeColorMode = themeColorDropdown.value;
            customThemeColors[customColorIndex] = Color.HSVToRGB(
                sliderHue.value,
                sliderSaturation.value,
                sliderValue.value
            );

            if (modeChanged) _UpdateGUI();
            _UpdateAudioLinkThemeColors();
#if UDONSHARP
            RequestSerialization();
#endif
        }

        public void _ResetThemeColors()
        {
            _themeColorMode = _initThemeColorMode;
            for (int i = 0; i < 4; ++i)
            {
                customThemeColors[i] = _initCustomThemeColors[i];
            }
            _UpdateGUI();
            _UpdateAudioLinkThemeColors();
#if UDONSHARP
            RequestSerialization();
#endif
        }

        public void _UpdateGUI()
        {
            _processGUIEvents = false;
            themeColorDropdown.value = _themeColorMode;

            bool isCustom = _themeColorMode == 1;
            extensionCanvas.gameObject.SetActive(isCustom);
            for (int i = 0; i < 4; ++i)
            {
                customColorLassos[i].gameObject.SetActive(
                    i == customColorIndex
                );
            }

            // update HSV sliders
            float h, s, v;
            Color.RGBToHSV(customThemeColors[customColorIndex], out h, out s, out v);
            sliderHue.value = h;
            sliderSaturation.value = s;
            sliderValue.value = v;

            _processGUIEvents = true;
        }

        public void _UpdateAudioLinkThemeColors()
        {
            if (audioLink == null) return;
            audioLink.themeColorMode = _themeColorMode;
            audioLink.customThemeColor0 = customThemeColors[0];
            audioLink.customThemeColor1 = customThemeColors[1];
            audioLink.customThemeColor2 = customThemeColors[2];
            audioLink.customThemeColor3 = customThemeColors[3];
            audioLink._UpdateThemeColors();
        }
    }
}
