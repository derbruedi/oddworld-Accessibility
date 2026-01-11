using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;

[HarmonyPatch(typeof(Subtitles), "Display")]
public static class SubtitlePatch
{
    static string lastToken = "";
    static float lastSpeakTime = 0f;

    static void Postfix(Subtitles __instance, string token)
    {
        try
        {
            if (token == lastToken && Time.time < lastSpeakTime + 5f) return;

            Type langManType = AccessTools.TypeByName("LanguageManager");
            if (langManType != null)
            {
                var getTextMethod = AccessTools.Method(langManType, "GetText", new Type[] { typeof(string) });
                if (getTextMethod != null)
                {
                    string realText = (string)getTextMethod.Invoke(null, new object[] { token });

                    if (!string.IsNullOrEmpty(realText) && realText != "BAD KEY")
                    {
                        string cleanText = System.Text.RegularExpressions.Regex.Replace(realText, @"\[.*?\]", "").Trim();
                        TolkHelper.Speak(cleanText);

                        lastToken = token;
                        lastSpeakTime = Time.time;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Time.time > lastSpeakTime + 10f)
                MelonLogger.Error($"Error in SubtitlePatch: {ex.Message}");
        }
    }
}