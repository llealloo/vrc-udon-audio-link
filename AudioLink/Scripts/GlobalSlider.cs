using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
using UnityEngine.UI;

namespace VRCAudioLink
{
    #if UDON
        using UdonSharp;
        using VRC.Udon;

        [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
        public class GlobalSlider : UdonSharpBehaviour
        {
            [UdonSynced]
            private float syncedValue;
            private bool deserializing;
            private Slider slider;
            private VRCPlayerApi localPlayer;

            private void Start()
            {
                slider = transform.GetComponent<Slider>();
                localPlayer = Networking.LocalPlayer;
                syncedValue = slider.value;
                deserializing = false;

                if (Networking.IsOwner(gameObject))
                    RequestSerialization();
            }

            public override void OnDeserialization()
            {
                deserializing = true;
                slider.value = syncedValue;
                deserializing = false;
            }

            public void SlideUpdate()
            {
                if (deserializing)
                    return;
                if (!Networking.IsOwner(gameObject))
                    Networking.SetOwner(localPlayer, gameObject);

                syncedValue = slider.value;
                RequestSerialization();
            }
        }
    #else
        public class GlobalSlider : MonoBehaviour
        {
        }
    #endif
}