using SlotGame.Core;
using SlotGame.Data;
using UnityEditor;
using UnityEngine;

namespace SlotGame.EditorTools
{
    internal static class RtpSimulationMenu
    {
        private const long Spins = 1_000_000;

        [MenuItem(EditorPaths.MenuRoot + "Run RTP Simulation (1M spins)", priority = 40)]
        private static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<SlotConfig>(EditorPaths.SlotConfig);
            if (config == null)
            {
                EditorUtility.DisplayDialog("RTP Simulation", "SlotConfig not found. Build the scene first.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("RTP Simulation", $"Playing {Spins:N0} spins...", 0.5f);
            try
            {
                var report = SlotMathSimulator.Run(config.BuildRules(), Spins, new SeededRandomProvider(Random.Range(0, int.MaxValue)));
                double feature = report.FreeSpinTriggers == 0 ? 0 : (double)report.PaidSpins / report.FreeSpinTriggers;
                string summary = $"{report}\nFree spins trigger roughly 1 in {feature:N0} paid spins.";
                Debug.Log("[Slot Game] " + summary);
                EditorUtility.DisplayDialog("RTP Simulation", summary, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
