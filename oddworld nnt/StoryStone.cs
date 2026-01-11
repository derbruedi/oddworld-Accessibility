using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Text.RegularExpressions;
using System.Reflection;

namespace OddworldAccess
{
    [HarmonyPatch(typeof(App), "Update")]
    public static class StoryStoneFinalTarget
    {
        static float nextCheck = 0f;
        static string lastSpokenText = "";

        static void Postfix()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.25f; // 4x pro Sekunde prüfen

            try
            {
                // 1. Suche den aktiven Stein
                DirectoryStoryStone activeStone = null;
                var allStones = GameObject.FindObjectsOfType<DirectoryStoryStone>();
                
                foreach (var stone in allStones)
                {
                    if (stone.InUse)
                    {
                        activeStone = stone;
                        break;
                    }
                }

                // Kein Stein an? Reset und raus.
                if (activeStone == null)
                {
                    if (lastSpokenText != "") lastSpokenText = "";
                    return;
                }

                // 2. DAS IST NEU: Wir holen das InfoPanel!
                // Im Quellcode steht: public Transform InfoPanel { get; set; }
                // Das ist der Ort, wo der Text wirklich ist.
                Transform panel = activeStone.InfoPanel;

                if (panel == null)
                {
                    // Fallback: Wenn kein InfoPanel da ist, suchen wir am Stein selbst (wie früher)
                    panel = activeStone.transform;
                }

                // 3. Suche TextPrint AUF DEM PANEL
                var textPrints = panel.GetComponentsInChildren<TextPrint>(true);
                
                // Sortieren nach Name (_a, _b, _c)
                var sortedPrints = textPrints.OrderBy(x => x.gameObject.name).ToList();

                string fullStory = "";

                foreach (var print in sortedPrints)
                {
                    // Ist es sichtbar?
                    if (!print.gameObject.activeInHierarchy) continue;
                    if (print.renderer == null || !print.renderer.enabled) continue;

                    // Text holen
                    string textPart = GetTextContent(print);

                    // Filtern
                    if (string.IsNullOrEmpty(textPart)) continue;
                    
                    textPart = textPart.Replace("\n", " ").Replace("\r", " ").Trim();
                    textPart = Regex.Replace(textPart, @"\[.*?\]", ""); 

                    if (textPart.Length < 2) continue;
                    if (textPart.Contains("UIContent")) continue;
                    if (textPart.Contains("#CRO#")) continue; 
                    if (textPart.Contains("#USE#")) continue;
                    if (Regex.IsMatch(textPart, @"^\d+\s*/\s*\d+$")) continue; 

                    fullStory += textPart + " ";
                }

                fullStory = fullStory.Trim();
                
                // 4. Sprechen
                if (fullStory.Length > 5 && fullStory != lastSpokenText)
                {
                    MelonLogger.Msg("--------------------------------------------------");
                    MelonLogger.Msg($"[STORY] {fullStory}");
                    MelonLogger.Msg("--------------------------------------------------");
                    
                    TolkHelper.Speak(fullStory);
                    AbeNavSystem.LastReadText = fullStory;
                    
                    lastSpokenText = fullStory;
                }
            }
            catch (Exception ex)
            {
                // Fehler ignorieren
            }
        }

        static string GetTextContent(TextPrint instance)
        {
            try
            {
                var traverse = Traverse.Create(instance);
                object val = traverse.Field("curtext").GetValue(); 
                if (val != null) return val.ToString();
                return instance.String_ID;
            }
            catch { return ""; }
        }
    }
}