
using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if UDON
using VRC.Udon;
using UdonSharp;

public class RevealSurfaceScript : UdonSharpBehaviour
#else
public class RevealSurfaceScript : MonoBehaviour
#endif
{
	public Material AdjustGiveQuaternion;
    void Start()
    {
        
    }
	
	void Update()
	{
		Quaternion quat = transform.localRotation;
		Vector3 pos = transform.position;

		
		float AllowRot = Mathf.Max(Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"),Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger") );
		AdjustGiveQuaternion.SetFloat( "_AllowRotation", AllowRot );
		AdjustGiveQuaternion.SetVector( "_InputBasisQuaternion", new Vector4( quat.x, quat.y, quat.z, quat.w ) );
		AdjustGiveQuaternion.SetVector( "_InputBasisVertex", new Vector4( pos.x, pos.y, pos.z, 0 ) );
	}
}
