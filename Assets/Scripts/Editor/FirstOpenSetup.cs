using System.IO;
using UnityEditor;

namespace SlotGame.EditorTools
{
    [InitializeOnLoad]
    internal static class FirstOpenSetup
    {
        private const string PromptedKey = "SlotGame.FirstOpenPrompted";

        static FirstOpenSetup()
        {
            if (UnityEngine.Application.isBatchMode || File.Exists(EditorPaths.Scene) || SessionState.GetBool(PromptedKey, false))
                return;

            SessionState.SetBool(PromptedKey, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorUtility.DisplayDialog("Slot Game",
                        "The game scene hasn't been generated yet. Build the scene, prefabs and animations now?",
                        "Build now", "Later"))
                {
                    SlotSceneBuilder.BuildFromMenu();
                }
            };
        }
    }
}
