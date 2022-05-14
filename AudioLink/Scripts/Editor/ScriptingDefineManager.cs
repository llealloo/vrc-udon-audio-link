#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCAudioLink.Editor
{
    [InitializeOnLoad]
    public class ScriptingDefineManager
    {
        static ScriptingDefineManager()
        {
            AddDefineIfMissing(EditorUserBuildSettings.selectedBuildTargetGroup, "AUDIOLINK");
        }

        private static void AddDefineIfMissing(BuildTargetGroup buildTarget, string newDefine)
        {
            bool definesChanged = false;
            string existingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
            HashSet<string> defineSet = new HashSet<string>();

            if (existingDefines.Length > 0)
                defineSet = new HashSet<string>(existingDefines.Split(';'));

            if (defineSet.Add(newDefine))
                definesChanged = true;

            if (definesChanged)
            {
                string finalDefineString = string.Join(";", defineSet.ToArray());
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, finalDefineString);
                Debug.LogFormat("Set Scripting Define Symbols for selected build target ({0}) to: {1}",
                    buildTarget.ToString(), finalDefineString);
            }
        }
    }
}
#endif