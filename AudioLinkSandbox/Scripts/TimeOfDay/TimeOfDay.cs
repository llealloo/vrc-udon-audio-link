using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif

namespace cnlohr
{
	#if UDON
	using VRC.Udon;
	using UdonSharp;
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class TimeOfDay : UdonSharpBehaviour
	#else
	public class TimeOfDay : MonoBehaviour
	#endif
	{
		public Light lightToControl;
		public Material matSquareStarrySky;
		private bool bHeld;
		public float fProgressSpeed = 0.5f;
		Quaternion qch_mirror;
/*
		XXX TODO
		public float DuskBright;
		public Color DuskColor;
		public float DayBright;
		public Color DayColor;
		public float NightBright;
		public Color NightColor;
*/
		void Start()
		{
			qch_mirror = new Quaternion( 0, 1, 0, 0 );  //Flips sun around over the floor.
		}
		
		#if UDON
		override public void OnPickup ()
		{
			bHeld = true;
		}
		
		override public void OnDrop()
		{
			bHeld = false;
		}
		#endif

		void Update()
		{
			if( !bHeld )
			{
				transform.localRotation *= Quaternion.Euler((float)((Time.deltaTime)*fProgressSpeed),0,0);
			}

			Quaternion quat = transform.localRotation;

			matSquareStarrySky.SetInt( "_UseInputBasis", 1 );
			matSquareStarrySky.SetVector( "_InputBasisQuaternion", new Vector4( quat.x, quat.y, quat.z, quat.w ) );

			Vector3 OriginalLightAngle = quat * new Vector3( 0, 0, 1 );
			if( OriginalLightAngle.y > 0 )
			{
				// At night, need to reflect around y.  To do this, it's a little
				// weird, but we rotate around XZ. Note that this only works
				// if transforming the light vector since it would break chirality.
				//Alternatively, output = .x = -.z, .y = .w, .z = .x, .w = -.y, if you don't want to do the work of the multiply
				//But, Udon makes this faster.
				quat = quat * qch_mirror;
				
				lightToControl.intensity = .04f;
			}
			else
			{
				lightToControl.intensity = 1.0f;
			}
			lightToControl.transform.localRotation = quat;
			quat = transform.localRotation;
		}
	}
}
