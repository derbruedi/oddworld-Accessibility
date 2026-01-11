using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System;

namespace OddworldAccess
{
    [HarmonyPatch(typeof(ChantFlies), "FormWord")] 
    public static class ChantFlyPatch
    {
        static void Prefix(string word)
        {
            try 
            {
                // Wir unterbrechen das Spiel NICHT.
                // Wir nehmen das Wort nur und stecken es in den Speicher von AbeNavSystem.
                if (!string.IsNullOrEmpty(word) && word.Length > 1)
                {
                    AbeNavSystem.CollectChantWord(word);
                }
            }
            catch {}
        }
    }
}