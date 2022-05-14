using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif

namespace VRCAudioLink
{
    #if UDON
        using UdonSharp;
        using VRC.Udon;

        public class AudioReactiveSurface : UdonSharpBehaviour
        {
            [Header("To use custom mesh, swap mesh in Mesh Filter component above")]
            [Header("AudioLink Settings")]
            public UdonBehaviour audioLink;
            public int band;
            [Range(0, 127)]
            public int delay;

            [Header("Reactivity Settings")]
            [Tooltip("Emission driven by amplitude")]
            [ColorUsage(true, true)]
            public Color color;
            [Tooltip("Emission multiplier")]
            public float intensity = 1f;
            [Tooltip("Hue shift driven by amplitude")]
            public float hueShift = 0f;
            [Tooltip("Pulse")]
            public float pulse = 0f;
            [Tooltip("Pulse rotation")]
            public float pulseRotation = 0f;

            void Start()
            {
                UpdateMaterial();
            }

            public void UpdateMaterial()
            {
                var block = new MaterialPropertyBlock();
                var mesh = GetComponent<MeshRenderer>();
                block.SetFloat("_Delay", (float)delay/128f);
                block.SetFloat("_Band", (float)band);
                block.SetFloat("_HueShift", hueShift);
                block.SetColor("_EmissionColor", color);
                block.SetFloat("_Emission", intensity);
                block.SetFloat("_Pulse", pulse);
                block.SetFloat("_PulseRotation", pulseRotation);
                mesh.SetPropertyBlock(block);
            }
        }
    #else
        public class AudioReactiveSurface : MonoBehaviour
        {
        }
    #endif
}