using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections;

namespace OddworldAccess
{
    public static class StatusReader
    {
        private static MethodInfo methodMudsStates = null;
        private static Type typeChapters = null;

        public static void ReportStatus()
        {
            int localActive = 0;
            
            try
            {
                var allObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach(var obj in allObjects)
                {
                    if (obj.GetType().Name == "MudokonSlave" && obj.gameObject.activeInHierarchy)
                    {
                        localActive++;
                    }
                }
            }
            catch {}

            string globalStats = GetPreciseGlobalStats();
            string message = $"{localActive} Mudokons nearby. {globalStats}";
            TolkHelper.Speak(message);
        }

        private static string GetPreciseGlobalStats()
        {
            try
            {
                if (methodMudsStates == null)
                {
                    Type typeMudList = AccessTools.TypeByName("MudokonList");
                    if (typeMudList == null) return "MudokonList class not found.";

                    typeChapters = AccessTools.TypeByName("LevelList+Chapters");
                    if (typeChapters == null) typeChapters = AccessTools.TypeByName("LevelList.Chapters"); 
                    if (typeChapters == null) return "Chapters enum not found.";

                    methodMudsStates = AccessTools.Method(typeMudList, "MudsStatesForChapter", new Type[] { typeChapters, typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(bool) });
                }

                if (methodMudsStates == null) return "Stats method not found.";

                int totalRescuedGame = 0;
                int totalMudsGame = 0;

                Array chapters = Enum.GetValues(typeChapters);

                foreach (var chapter in chapters)
                {
                    object[] parameters = new object[] { chapter, 0, 0, true }; 
                    methodMudsStates.Invoke(null, parameters);
                    int chapterTotal = (int)parameters[1];
                    int chapterRescued = (int)parameters[2];

                    totalMudsGame += chapterTotal;
                    totalRescuedGame += chapterRescued;
                }

                float percent = 0f;
                if (totalMudsGame > 0) 
                    percent = ((float)totalRescuedGame / (float)totalMudsGame) * 100f;

                return $"Total rescued: {totalRescuedGame} of {totalMudsGame} ({Math.Round(percent)}%).";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Stats Error: {ex.Message}");
                return "Global stats unavailable.";
            }
        }
    }
}