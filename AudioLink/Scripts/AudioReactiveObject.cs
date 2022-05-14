using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
using System;
using System.Collections;

namespace VRCAudioLink
{
    #if UDON
        using UdonSharp;
        using VRC.Udon;

        public class AudioReactiveObject : UdonSharpBehaviour
        {
            public UdonBehaviour audioLink;
            public int band;
            [Range(0, 127)]
            public int delay;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale = new Vector3(1f, 1f, 1f);


            private int _dataIndex;
            private Vector3 _initialPosition;
            private Vector3 _initialRotation;
            private Vector3 _initialScale;

            void Start()
            {
                UpdateDataIndex();
                _initialPosition = transform.localPosition;
                _initialRotation = transform.localEulerAngles;
                _initialScale = transform.localScale;

            }

            void Update()
            {
                Color[] audioData = (Color[])audioLink.GetProgramVariable("audioData");
                if (audioData.Length != 0)      // check for audioLink initialization
                {
                    float amplitude = audioData[_dataIndex].grayscale;

                    transform.localPosition = _initialPosition + (position * amplitude);
                    transform.localEulerAngles = _initialRotation + (rotation * amplitude);

                    transform.localScale = new Vector3(_initialScale.x * Mathf.Lerp(1f, scale.x, amplitude), _initialScale.y * Mathf.Lerp(1f, scale.y, amplitude), _initialScale.z * Mathf.Lerp(1f, scale.z, amplitude));
                }
            }

            public void UpdateDataIndex()
            {
                _dataIndex = (band * 128) + delay;
            }
        }
    #else
        public class AudioReactiveObject : MonoBehaviour
        {
        }
    #endif
}