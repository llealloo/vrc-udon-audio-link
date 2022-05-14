
using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif

#if UDON
using UdonSharp;
using VRC.Udon;

namespace VRCAudioLink
{
    public class MirrorToggle : UdonSharpBehaviour
    {

        public GameObject mirror;

        void Start()
        {
            
        }

        public override void Interact()
        {
            mirror.SetActive(!mirror.activeSelf);
        }

    }
}

#else
namespace VRCAudioLink
{
    public class MirrorToggle : MonoBehaviour { }
}
#endif