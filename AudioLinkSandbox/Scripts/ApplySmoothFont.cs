
using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if UDON
using VRC.Udon;
using UdonSharp;
#endif


#if !COMPILER_UDONSHARP && UNITY_EDITOR // These using statements must be wrapped in this check to prevent issues on builds
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase.Editor.BuildPipeline;
#endif
using UnityEditor;
#if UDON
using UdonSharpEditor;
#endif
#endif

namespace AudioLink
{
	//TODO: Implement this: 
	
		#if !COMPILER_UDONSHARP && UNITY_EDITOR 

/*
	//From: https://github.com/MerlinVR/UdonSharp/blob/master/Assets/UdonSharp/Editor/BuildUtilities/UdonSharpBuildCompile.cs
	//TODO: Return false if there was an issue.
    internal class UdonSharpBuildCompile : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 100;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Avatar)
                return true;

            //if (UdonSharpSettings.GetSettings()?.disableUploadCompile ?? false)
            //    return true;

			Debug.Log( "Build Requested." );

            return true;
        }
    }
	*/

	[ExecuteInEditMode]
	public class ApplySmoothFont : MonoBehaviour
	{
		[TextArea]
		public string TextSet;
		public int Columns;
		public int Rows;
		public Material mat_ApplySmoothTex;
		public string uuidStr;

		private int lasthash;
		private Material UseMaterial;
		private Texture2D UseTexture;
		private bool GenerateMaterial;

		public void Start()
		{
			lasthash = 0;
			UpdateProps();
		}
		
		public void OnValidate()
		{
			UpdateProps();
		}
		
		public void Update()
		{
			if (GenerateMaterial)
			{
				UseMaterial.SetTexture( "_TextData", UseTexture );
				AssetDatabase.CreateAsset(UseMaterial, $"Assets/Compiled/cmat_{uuidStr}.mat");
				GenerateMaterial = false;
			}
		}

		public void UpdateProps()
		{
			if( uuidStr.Length == 0 )
			{
				uuidStr = System.Guid.NewGuid().ToString();
			}
			int thishash = (TextSet+Rows.ToString()+Columns.ToString()).GetHashCode();
			if( thishash == lasthash )
			{
				return;
			}
			lasthash = thishash;

			if( Rows == 0 ) Rows = 10;
			if( Columns == 0 ) Columns = 20;

			UseTexture = AssetDatabase.LoadAssetAtPath($"Assets/Compiled/ctex_{uuidStr}.asset", typeof( Texture2D ) ) as Texture2D;
			if( UseTexture == null )
			{
				Texture2D UseTexture = new Texture2D( Columns, Rows, TextureFormat.RGBAHalf, false );
				AssetDatabase.CreateAsset( UseTexture, $"Assets/Compiled/ctex_{uuidStr}.asset");
			}
			else
			{
				UseTexture.Resize( Columns, Rows );
			}

			int lx = 0, ly = 0;
			int i = 0;
			
			Color currentColor;
			currentColor.r = 1;
			currentColor.g = 1;
			currentColor.b = 1;
			currentColor.a = 0;
			
			int mode = 0;
			string thistag = "";
			int weight = 0;
			int submode = 0;
			
			for( i = 0; i < TextSet.Length && lx+ly*Columns < Rows*Columns; i++ )
			{
				char tc = TextSet[i];
				byte cs = (byte)tc;
				char emit = (char)0;
				
				// This code parses through HTML, sort of.
				// If it sees a < it start looking for RGB or font-weight.
				// if it sees a & it looks for nbsp, gt or lt.

				//Parsing based off of https://pinetools.com/syntax-highlighter
				// for syntax highlighter

				if( mode == 0 )
				{
					if( cs == 9 )
					{
						lx += 4;
					}
					if( cs == 10 )
					{
						// Handle newlines
						lx = 0;
						ly++;
					}
					else if( cs == '<' )
					{
						thistag = "";
						mode = 1;
					}
					else if( cs == '&' )
					{
						thistag = "";
						mode = 2;
						submode = 0;
					}
					else
					{
						emit = tc;
					}
				}
				else if( mode == 2 )
				{
					if( submode == 0 && tc == 'g' )
						emit = '>';
					if( submode == 0 && tc == 'l' )
						emit = '<';
					if( submode == 0 && tc == 'n' )
						emit = ' ';
					if( cs == ';' ) mode = 0;
					submode++;
				}
				else if( mode == 1 )
				{					
					if( cs == '>' )
					{
						int weightindex = thistag.IndexOf( "font-weight" );
						int colorindex = thistag.IndexOf( "rgb(" );
						if( weightindex >= 0 )
						{
							weight = 1;
						}
						else
						{
							weight = 0;
						}
						if( colorindex >= 0 )
						{
							string[] rgbv = thistag.Substring( colorindex+4 ).Split( ',', ')', '\"' );
							int tmp;
							
							currentColor.r = int.TryParse( rgbv[0], out tmp )?tmp/255.0f:1.0f;
							currentColor.g = int.TryParse( rgbv[1], out tmp )?tmp/255.0f:1.0f;
							currentColor.b = int.TryParse( rgbv[2], out tmp )?tmp/255.0f:1.0f;
						}
						else
						{
							currentColor.r = 1;
							currentColor.g = 1;
							currentColor.b = 1;
						}
						mode = 0;
					}
					else
					{
						thistag += TextSet[i];
					}
				}
				if( emit != (char)0 )
				{
					// Actually emit a character.
					Color c = currentColor;
					c.a = (emit/256.0f) + weight*2+2;
					
					UseTexture.SetPixel( lx, ly, c );
					
					lx++;
				}
				if(lx >= Columns)
				{
					lx = 0;
					ly++;
				}
			}
			UseTexture.Apply();

			UseMaterial = AssetDatabase.LoadAssetAtPath($"Assets/Compiled/cmat_{uuidStr}.asset", typeof( Material ) ) as Material;
			if( UseMaterial == null )
			{
				UseMaterial = 
					new Material(Shader.Find("AudioLinkSandbox/ApplySmoothText"));
				UseMaterial.name = "cmat_"+uuidStr;
				GenerateMaterial = true;
			}
			UseMaterial.SetTexture( "_TextData", UseTexture );
			GetComponent<MeshRenderer>().sharedMaterial = UseMaterial;		

		//	AssetDatabase.CreateAsset(mat_this, $"Assets/Compiled/cmat_{uuidStr}.mat");
		//	AssetDatabase.CreateAsset(GetComponent<MeshRenderer>().sharedMaterial, $"Assets/Compiled/cmat_{uuidStr}.mat");

	/*		Debug.Log("START" );
			Debug.Log( UseMaterial );
			
			bool hasMaterial = false;
			string [] foundassets = AssetDatabase.FindAssets( gid );
			foreach (string guid2 in foundassets)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid2);
				if( path.Contains("cmat") ) hasMaterial = true;
			}
			Debug.Log( hasMaterial );
			if( !hasMaterial )
			{
				Debug.Log( $"Can't Find: Assets/Compiled/cmat_{gid}.asset" );
				//AssetDatabase.CreateAsset(UseMaterial, $"Assets/Compiled/cmat_{gid}.asset");
				//AssetDatabase.ImportAsset($"Assets/Compiled/cmat_{gid}.mat", ImportAssetOptions.ImportRecursive);
				//Debug.Log( AssetDatabase.LoadAssetAtPath($"Assets/Compiled/ctex_{gid}.asset", typeof( Texture2D ) ) );
				//Debug.Log(  );
			}
//			AssetDatabase.StopAssetEditing();
*/
		}
	}
#endif
}
