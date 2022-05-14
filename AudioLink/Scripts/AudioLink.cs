using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase;
#else
using UnityEngine.Rendering;
#endif
using UnityEngine.UI;
using System;

// HAAAX
using System.ComponentModel;

namespace VRCAudioLink
{
#if UDON
using UdonSharp;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using System.Collections.Immutable;

#endif

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AudioLink : UdonSharpBehaviour
#else
    public class AudioLink : MonoBehaviour
#endif
    {
        const float AUDIOLINK_VERSION_NUMBER = 2.07f;

        [Header("Main Settings")] [Tooltip("Should be used with AudioLinkInput unless source is 2D. WARNING: if used with a custom 3D audio source (not through AudioLinkInput), audio reactivity will be attenuated by player position away from the Audio Source")]
        public AudioSource audioSource;

        [Tooltip("AVPro video player component")]
        public VRCAVProVideoPlayer videoSourceAvPro;

        [Header("Basic EQ")] [Range(0.0f, 2.0f)] [Tooltip("Warning: this setting might be taken over by AudioLinkController")]
        public float gain = 1f;

        [Range(0.0f, 2.0f)] [Tooltip("Warning: this setting might be taken over by AudioLinkController")]
        public float bass = 1f;

        [Range(0.0f, 2.0f)] [Tooltip("Warning: this setting might be taken over by AudioLinkController")]
        public float treble = 1f;

        [Header("4 Band Crossover")] [Range(0.0f, 0.168f)] [Tooltip("Bass / low mid crossover")]
        public float x0 = 0.0f;

        [Range(0.242f, 0.387f)] [Tooltip("Bass / low mid crossover")]
        public float x1 = 0.25f;

        [Range(0.461f, 0.628f)] [Tooltip("Low mid / high mid crossover")]
        public float x2 = 0.5f;

        [Range(0.704f, 0.953f)] [Tooltip("High mid / treble crossover")]
        public float x3 = 0.75f;

        [Header("4 Band Threshold Points (Sensitivity)")] [Range(0.0f, 1.0f)] [Tooltip("Bass threshold level (lower is more sensitive)")]
        public float threshold0 = 0.45f;

        [Range(0.0f, 1.0f)] [Tooltip("Low mid threshold level (lower is more sensitive)")]
        public float threshold1 = 0.45f;

        [Range(0.0f, 1.0f)] [Tooltip("High mid threshold level (lower is more sensitive)")]
        public float threshold2 = 0.45f;

        [Range(0.0f, 1.0f)] [Tooltip("Treble threshold level (lower is more sensitive)")]
        public float threshold3 = 0.45f;

        [Header("Fade Controls")] [Range(0.0f, 1.0f)] [Tooltip("Amplitude fade amount. This creates a linear fade-off / trails effect. Warning: this setting might be taken over by AudioLinkController")]
        public float fadeLength = 0.8f;

        [Range(0.0f, 1.0f)] [Tooltip("Amplitude fade exponential falloff. This attenuates the above (linear) fade-off exponentially, creating more of a pulsed effect. Warning: this setting might be taken over by AudioLinkController")]
        public float fadeExpFalloff = 0.3f;

        [Header("Theme Colors")] [Tooltip("Enable for custom theme colors for Avatars to use.")]
        [StringInList("ColorChord Colors", "Custom")]
        public int themeColorMode;
        public Color themeColor0 = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
        public Color themeColor1 = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        public Color themeColor2 = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public Color themeColor3 = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

        [Header("Internal (Do not modify)")] public Material audioMaterial;
        public GameObject audioTextureExport;
        public GameObject videoTextureExport;
        private Shader _shaderAudioLinkExport;
        #if !VRC_SDK_VRCSDK2 && !VRC_SDK_VRCSDK3
        public RenderTexture audioRenderTexture;
        #endif

        [Header("Experimental (Limits performance)")] [Tooltip("Enable Udon audioData array. Required by AudioReactiveLight and AudioReactiveObject. Uses ReadPixels which carries a performance hit. For experimental use when performance is less of a concern")]
        public bool audioDataToggle = false;

        public Color[] audioData;
        public Texture2D audioData2D; // Texture2D reference for hacked Blit, may eventually be depreciated

        private float[] _spectrumValues = new float[1024];
        private float[] _spectrumValuesTrim = new float[1023];
        private float[] _audioFramesL = new float[1023 * 4];
        private float[] _audioFramesR = new float[1023 * 4];
        private float[] _samples = new float[1023];
        private float _audioLinkInputVolume = 0.01f; // smallify input source volume level

        // Mechanism to provide sync'd instance time to all avatars.
#if UDON
    [UdonSynced]
#endif
        private double _masterInstanceJoinTime;
        private double _elapsedTime = 0;
        private double _elapsedTimeMSW = 0;
        private int    _networkTimeMS;
        private double _networkTimeMSAccumulatedError;
#pragma warning disable 414 ///The private field is assigned but its value is never used
        private bool   _hasInitializedTime = false;
#pragma warning restore 414
        private double _FPSTime = 0;
        private int    _FPSCount = 0;
        private float  _ReadbackTime = 0;
        private System.Diagnostics.Stopwatch stopwatch;

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
        private double GetElapsedSecondsSince2019() { return (Networking.GetNetworkDateTime() - new DateTime(2020, 1, 1) ).TotalSeconds; }
        //private double GetElapsedSecondsSinceMidnightUTC() { return (Networking.GetNetworkDateTime() - DateTime.UtcNow.Date ).TotalSeconds; }
#else
        private double GetElapsedSecondsSince2019() { return 0; }
#endif

        // Fix for AVPro mono game output bug (if running the game with a mono output source like a headset)
        private int _rightChannelTestDelay = 300;
        private int _rightChannelTestCounter;
        private bool _ignoreRightChannel = false;

        void Start()
        {
            #if UDON
            {
                // Handle sync'd time stuff.
                // OLD NOTES
                //Originally used GetServerTimeInMilliseconds
                //Networking.GetServerTimeInMilliseconds will alias to every 49.7 days (2^32ms). GetServerTimeInSeconds also aliases.
                //We still alias, but TCL suggested using Networking.GetNetworkDateTime.
                //DateTime currentDate = Networking.GetNetworkDateTime();
                //UInt64 currentTimeTicks = (UInt64)(currentDate.Ticks/TimeSpan.TicksPerMillisecond);
                // NEW NOTES
                //We now just compute delta times per frame.

                double startTime = GetElapsedSecondsSince2019();
                _networkTimeMS = Networking.GetServerTimeInMilliseconds();
                if (Networking.IsMaster)
                {
                    _masterInstanceJoinTime = startTime;
                    RequestSerialization();
                }

                //_networkTimeOfDayUTC = GetElapsedSecondsSinceMidnightUTC();
                //Debug.Log($"AudioLink _networkTimeOfDayUTC = {_networkTimeOfDayUTC}" );
                Debug.Log($"AudioLink _networkTimeMS = {_networkTimeMS}" );
                Debug.Log($"AudioLink Time Sync Debug: IsMaster: {Networking.IsMaster} startTime: {startTime}");

                _rightChannelTestCounter = _rightChannelTestDelay;
            }
            #endif

            stopwatch = new System.Diagnostics.Stopwatch();
            
            UpdateSettings();
            UpdateThemeColors();
            if (audioSource.name.Equals("AudioLinkInput"))
            {
                audioSource.volume = _audioLinkInputVolume;
            }

            gameObject.SetActive(true); // client disables extra cameras, so set it true
            transform.position = new Vector3(0f, 10000000f, 0f); // keep this in a far away place
            _shaderAudioLinkExport = audioTextureExport.GetComponent<Renderer>().material.shader;
            #if !VRC_SDK_VRCSDK2 && !VRC_SDK_VRCSDK3
            Shader.SetGlobalTexture("_AudioTexture", audioRenderTexture, RenderTextureSubElement.Default);
            #endif
            //GetComponent<Camera>().SetReplacementShader( _shaderAudioLinkExport, "AudioLinkExport" ); 
        }

        public void SetupVideo()
        {
            videoTextureExport.SetActive(videoSourceAvPro == null);
            if (videoSourceAvPro != null)
            {
                VRCAVProVideoScreen screen = (VRCAVProVideoScreen) videoTextureExport.gameObject.GetComponent(typeof(VRCAVProVideoScreen));
                screen.VideoPlayer = videoSourceAvPro;
                // object screen = (object) videoTextureExport.gameObject.GetComponent(typeof(VRCAVProVideoScreen));
                // foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(screen))
                // {
                //     string name = descriptor.Name;
                //     object value = descriptor.GetValue(screen);
                //     Console.WriteLine("{0}={1}", name, value);
                // }

            }
            else
            {
                Debug.Log("skipping AudioLink Video setup.");
            }
        }

        // Only happens once per second.
        private void FPSUpdate()
        {
            #if UDON
            if( !_hasInitializedTime )
            {
                if( _masterInstanceJoinTime > 0.00001 )
                {
                    //We can now do our time setup.
                    double Now = GetElapsedSecondsSince2019();
                    _elapsedTime = Now - _masterInstanceJoinTime;
                    Debug.Log( $"AudioLink Time Sync Debug: Received instance time of {_masterInstanceJoinTime} and current time of {Now} delta of {_elapsedTime}" );
                    _hasInitializedTime = true;
                    _FPSTime = _elapsedTime;
                }
                else if( _elapsedTime > 10 && Networking.IsMaster )
                {
                    //Have we gone more than 10 seconds and we're master?
                    Debug.Log( "AudioLink Time Sync Debug: You were master.  But no _masterInstanceJoinTime was provided for 10 seconds.  Resetting instance time." );
                    _masterInstanceJoinTime = GetElapsedSecondsSince2019();
                    RequestSerialization();
                    _hasInitializedTime = true;
                    _elapsedTime = 0;
                    _FPSTime = _elapsedTime;
                }
            }
            #endif

            audioMaterial.SetVector("_VersionNumberAndFPSProperty", new Vector4(AUDIOLINK_VERSION_NUMBER, 0, _FPSCount, 1));
            #if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
            audioMaterial.SetVector("_PlayerCountAndData", new Vector4(
                VRCPlayerApi.GetPlayerCount(),
                Networking.IsMaster?1.0f:0.0f,
                Networking.LocalPlayer.isInstanceOwner?1.0f:0.0f,
                0 ) );

            #else
                audioMaterial.SetVector("_PlayerCountAndData", new Vector4(
                0,
                0,
                0,
                0 ) );
            #endif
            _FPSCount = 0;
            _FPSTime++;

            // Other things to handle every second.

            // This handles wrapping of the ElapsedTime so we don't lose precision
            // onthe floating point.
            const double ElapsedTimeMSWBoundary = 1024;
            if( _elapsedTime >= ElapsedTimeMSWBoundary )
            {
                //For particularly long running instances, i.e. several days, the first
                //few frames will be spent federating _elapsedTime into _elapsedTimeMSW.
                //This is fine.  It just means over time, the
                _FPSTime = 0;
                _elapsedTime -= ElapsedTimeMSWBoundary;
                _elapsedTimeMSW++;
            }

            // Finely adjust our network time estimate if needed.
            #if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
            int networkTimeMSNow = Networking.GetServerTimeInMilliseconds();
            #else
            int networkTimeMSNow = (int)(Time.time*1000.0f);
            #endif
            int networkTimeDelta = networkTimeMSNow - _networkTimeMS;
            if( networkTimeDelta > 3000 )
            {
                //Major upset, reset.
                _networkTimeMS = networkTimeMSNow;
            }
            else if( networkTimeDelta < -3000 )
            {
                //Major upset, reset.
                _networkTimeMS = networkTimeMSNow;
            }
            else
            {
                //Slowly correct the timebase.
                _networkTimeMS += networkTimeDelta/20;
            }
            //Debug.Log( $"Refinement: ${networkTimeDelta}" );
        }

        private void Update()
        {
            // Tested: There does not appear to be any drift updating it this way.
            _elapsedTime += Time.deltaTime;

            // Advance the current network time by a little.
            // this algorithm also takes into account sub-millisecond jitter.
            {
                double deltaTimeMS = Time.deltaTime*1000.0;
                int advanceTimeMS = (int)(deltaTimeMS);
                _networkTimeMSAccumulatedError += deltaTimeMS - advanceTimeMS;
                if( _networkTimeMSAccumulatedError > 1 )
                {
                    _networkTimeMSAccumulatedError--;
                    advanceTimeMS++;
                }
                _networkTimeMS += advanceTimeMS;
            }

            _FPSCount++;

            if (_elapsedTime >= _FPSTime)
            {
                FPSUpdate();
            }

            audioMaterial.SetVector("_AdvancedTimeProps", new Vector4(
                (float)_elapsedTime,
                (float)_elapsedTimeMSW,
                (float)DateTime.Now.TimeOfDay.TotalSeconds,
                _ReadbackTime ) );

            audioMaterial.SetVector("_AdvancedTimeProps2", new Vector4(
                (float)((_networkTimeMS)&65535),
                (float)((_networkTimeMS)>>16),
                0, 0 ) );

            // General Profiling Notes:
            //    Profiling done on 2021-05-26 on an Intel Intel Core i7-8750H CPU @ 2.20GHz
            //    Running loop 255 times (So divide all times by 255)
            //    Base load of system w/o for loop: ~420us in merlin profile land.
            //    With loop, with just summer: 1.2ms / 255
            //    Calling material.SetVeactor( ... new Vector4 ) in the loop:  2.7ms / 255
            //    Setting a float in the loop (to see if there's a difference): 1.9ms / 255
            //                             but setting 4 floats individually... is 3.0ms / 255
            //    The whole shebang with Networking.GetServerTimeInMilliseconds(); 2.3ms / 255
            //    Material.SetFloat with Networking.GetServerTimeInMilliseconds(); 2.3ms / 255
            //    Material.SetFloat with Networking.GetServerTimeInMilliseconds(), twice; 2.9ms / 255
            //    Casting and encoding as UInt32 as 2 floats, to prevent aliasing, twice: 5.1ms / 255
            //    Casting and encoding as UInt32 as 2 floats, to prevent aliasing, once: 3.2ms / 255

            if (audioSource != null)
            {
                SendAudioOutputData();

                // Used to correct for the volume of the audio source component
                audioMaterial.SetFloat("_SourceVolume", audioSource.volume);
                audioMaterial.SetFloat("_SourceSpatialBlend", audioSource.spatialBlend);
            }

            #if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
                if (Networking.LocalPlayer != null)
                {
                    float distanceToSource = Vector3.Distance(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, audioSource.transform.position);
                    audioMaterial.SetFloat("_SourceDistance", distanceToSource);
                }
            #endif            

        // As an optimization: when in-game, require others to call these after
        // setting values on this object.
        // Since we expect changes to values on this object in editor through the GUI,
        // we do not have explicit events to when things change.
        #if UNITY_EDITOR
            UpdateSettings();
            UpdateThemeColors();
        #endif
        }

        void OnPostRender()
        {
            if (audioDataToggle)
            {
                // This profiling should be removed in a few weeks. (If it's still here on 2021-07-30, please remove refrences to stopwatch and _ReadbackTime)
                stopwatch.Restart();
                audioData2D.ReadPixels(new Rect(0, 0, audioData2D.width, audioData2D.height), 0, 0, false);
                audioData = audioData2D.GetPixels();
                stopwatch.Stop();
                _ReadbackTime = ((float)(stopwatch.Elapsed.TotalMilliseconds));
            }
        }

        public void UpdateSettings()
        {
            audioMaterial.SetFloat("_X0", x0);
            audioMaterial.SetFloat("_X1", x1);
            audioMaterial.SetFloat("_X2", x2);
            audioMaterial.SetFloat("_X3", x3);
            audioMaterial.SetFloat("_Threshold0", threshold0);
            audioMaterial.SetFloat("_Threshold1", threshold1);
            audioMaterial.SetFloat("_Threshold2", threshold2);
            audioMaterial.SetFloat("_Threshold3", threshold3);
            audioMaterial.SetFloat("_Gain", gain);
            audioMaterial.SetFloat("_FadeLength", fadeLength);
            audioMaterial.SetFloat("_FadeExpFalloff", fadeExpFalloff);
            audioMaterial.SetFloat("_Bass", bass);
            audioMaterial.SetFloat("_Treble", treble);
        }

        // Note: These might be changed frequently so as an optimization, they're in a different function
        // rather than bundled in with the other things in UpdateSettings().
        public void UpdateThemeColors()
        {
            audioMaterial.SetInt("_ThemeColorMode", themeColorMode);
            audioMaterial.SetColor("_ThemeColor0", themeColor0);
            audioMaterial.SetColor("_ThemeColor1", themeColor1);
            audioMaterial.SetColor("_ThemeColor2", themeColor2);
            audioMaterial.SetColor("_ThemeColor3", themeColor3);
        }

        public void SendAudioOutputData()
        {
            audioSource.GetOutputData(_audioFramesL, 0);                // left channel

            if (_rightChannelTestCounter > 0)
            {
                if (_ignoreRightChannel) {
                    System.Array.Copy(_audioFramesL, 0, _audioFramesR, 0, 4092);
                } else {
                    audioSource.GetOutputData(_audioFramesR, 1);
                }
                _rightChannelTestCounter--;
            } else {
                _rightChannelTestCounter = _rightChannelTestDelay;      // reset test countdown
                _audioFramesR[0] = 0f;                                  // reset tested array element to zero just in case
                audioSource.GetOutputData(_audioFramesR, 1);            // right channel test
                _ignoreRightChannel = (_audioFramesR[0] == 0f) ? true : false;
            }

            System.Array.Copy(_audioFramesL, 0, _samples, 0, 1023); // 4092 - 1023 * 4
            audioMaterial.SetFloatArray("_Samples0L", _samples);
            System.Array.Copy(_audioFramesL, 1023, _samples, 0, 1023); // 4092 - 1023 * 3
            audioMaterial.SetFloatArray("_Samples1L", _samples);
            System.Array.Copy(_audioFramesL, 2046, _samples, 0, 1023); // 4092 - 1023 * 2
            audioMaterial.SetFloatArray("_Samples2L", _samples);
            System.Array.Copy(_audioFramesL, 3069, _samples, 0, 1023); // 4092 - 1023 * 1
            audioMaterial.SetFloatArray("_Samples3L", _samples);

            System.Array.Copy(_audioFramesR, 0, _samples, 0, 1023); // 4092 - 1023 * 4
            audioMaterial.SetFloatArray("_Samples0R", _samples);
            System.Array.Copy(_audioFramesR, 1023, _samples, 0, 1023); // 4092 - 1023 * 3
            audioMaterial.SetFloatArray("_Samples1R", _samples);
            System.Array.Copy(_audioFramesR, 2046, _samples, 0, 1023); // 4092 - 1023 * 2
            audioMaterial.SetFloatArray("_Samples2R", _samples);
            System.Array.Copy(_audioFramesR, 3069, _samples, 0, 1023); // 4092 - 1023 * 1
            audioMaterial.SetFloatArray("_Samples3R", _samples);
        }

        private float Remap(float t, float a, float b, float u, float v)
        {
            return ((t - a) / (b - a)) * (v - u) + u;
        }
    }

    #if !COMPILER_UDONSHARP && UNITY_EDITOR && UDON
    [CustomEditor(typeof(AudioLink))]
    public class AudioLinkEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Link all sound reactive objects to this AudioLink", "Links all UdonBehaviours with 'audioLink' parameter to this object."))) { LinkAll(); }
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        void LinkAll()
        {
            UdonBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsOfType<UdonBehaviour>();
            foreach (UdonBehaviour behaviour in allBehaviours)
            {
                if (!behaviour.programSource) continue;
                var program = behaviour.programSource.SerializedProgramAsset.RetrieveProgram();
                ImmutableArray<string> exportedSymbolNames = program.SymbolTable.GetExportedSymbols();
                foreach (string exportedSymbolName in exportedSymbolNames)
                {
                    if (exportedSymbolName.Equals("audioLink"))
                    {
                        var variableValue = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour)target);
                        System.Type symbolType = program.SymbolTable.GetSymbolType(exportedSymbolName);
                        if (!behaviour.publicVariables.TrySetVariableValue("audioLink", variableValue))
                        {
                            if (!behaviour.publicVariables.TryAddVariable(CreateUdonVariable(exportedSymbolName, variableValue, symbolType)))
                            {
                                Debug.LogError($"Failed to set public variable '{exportedSymbolName}' value.");
                            }

                            if(PrefabUtility.IsPartOfPrefabInstance(behaviour))
                            {
                                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                            }
                        }
                    }
                }
            }
        }

        IUdonVariable CreateUdonVariable(string symbolName, object value, System.Type type)
        {
            System.Type udonVariableType = typeof(UdonVariable<>).MakeGenericType(type);
            return (IUdonVariable)Activator.CreateInstance(udonVariableType, symbolName, value);
        }

    }
    #endif
}
