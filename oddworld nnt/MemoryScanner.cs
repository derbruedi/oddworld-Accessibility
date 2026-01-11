using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;

namespace OddworldAccess
{
    public class MemoryScanner : MelonMod
    {
        public override void OnUpdate()
        {
            // TASTE N: Startet jetzt den Pfad-Scanner (Navi)
            if (Input.GetKeyDown(KeyCode.N))
            {
                ScanForPaths();
            }
            
            // TASTE L: Scannt weiterhin Texte/Objekte (optional, falls du das behalten willst)
            if (Input.GetKeyDown(KeyCode.L))
            {
                // ScanEverythingNearby(); // Kannst du auskommentieren, wenn du L für was anderes brauchst
            }
        }

        void ScanForPaths()
        {
            MelonLogger.Msg("!!! SUCHE NACH PFADEN (SPLINES) !!!");
            TolkHelper.Speak("Scanning paths...");

            // Wir suchen nach der Klasse 'BezierSplineGO', die die Wege definiert
            // Das haben wir in deinen Dateien Meat.cs und SplineMarkerSet.cs herausgefunden.
            Type splineType = AccessTools.TypeByName("BezierSplineGO");
            
            if (splineType == null) 
            {
                TolkHelper.Speak("Error: Spline class not found.");
                return;
            }

            var allSplines = UnityEngine.Object.FindObjectsOfType(splineType);
            Vector3 abePos = GetAbePosition();
            
            GameObject closestPath = null;
            float minDistance = 9999f;
            int pathCount = 0;

            foreach (var splineObj in allSplines)
            {
                MonoBehaviour splineScript = splineObj as MonoBehaviour;
                if (splineScript == null) continue;

                // Distanz berechnen
                float dist = Vector3.Distance(splineScript.transform.position, abePos);
                
                // Wir interessieren uns nur für Pfade in der Nähe (30 Meter)
                if (dist < 30.0f)
                {
                    pathCount++;
                    
                    // Ist es der nächste Pfad?
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPath = splineScript.gameObject;
                    }
                    
                    // Für Debugging ins Log schreiben
                    MelonLogger.Msg($"[PFAD] '{splineScript.name}' Dist: {Math.Round(dist,1)}m");
                }
            }

            if (closestPath != null)
            {
                // Sagt dir den Namen des nächsten Pfads an. 
                // Wenn wir die Namen kennen (z.B. "MainPath"), wissen wir, wo wir sind.
                TolkHelper.Speak($"Found {pathCount} paths. Closest is {closestPath.name}, {Math.Round(minDistance)} meters away.");
            }
            else
            {
                TolkHelper.Speak("No navigation paths nearby.");
            }
        }

        Vector3 GetAbePosition()
        {
            var abe = GameObject.FindObjectOfType<Abe>();
            if (abe != null) return abe.transform.position;
            return Vector3.zero;
        }
        
        // Alte Scan-Funktion (optional drin lassen oder löschen)
        void ScanEverythingNearby()
        {
            // ... (Dein alter Code hier, falls du ihn noch brauchst)
        }
    }
}