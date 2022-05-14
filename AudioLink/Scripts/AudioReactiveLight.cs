using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
using System;

namespace VRCAudioLink
{
    #if UDON
        using UdonSharp;
        using VRC.Udon;

        public class AudioReactiveLight : UdonSharpBehaviour
        {
            public UdonBehaviour audioLink;
            public int band;
            [Range(0, 127)]
            public int delay;
            public bool affectIntensity = true;
            public float intensityMultiplier = 1f;
            public float hueShift;

            private Light _light;
            private int _dataIndex;
            private Color _initialColor;

            void Start()
            {
                _light = transform.GetComponent<Light>();
                _initialColor = _light.color;
                _dataIndex = (band * 128) + delay;

            }

            void Update()
            {
                Color[] audioData = (Color[])audioLink.GetProgramVariable("audioData");
                if(audioData.Length != 0)       // check for audioLink initialization
                {
                    float amplitude = audioData[_dataIndex].grayscale;
                    if (affectIntensity) _light.intensity = amplitude * intensityMultiplier;
                    _light.color = HueShift(_initialColor, amplitude * hueShift);
                }
            }

            private Color HueShift(Color color, float hueShiftAmount)
            {
                float h, s, v;
                Color.RGBToHSV(color, out h, out s, out v);
                h += hueShiftAmount;
                return Color.HSVToRGB(h, s, v);
            }
        }
    #else
        public class AudioReactiveLight : MonoBehaviour
        {
        }
    #endif
}