using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib; 
using OddworldAccess;

[assembly: MelonInfo(typeof(OddworldMod), "Oddworld Access", "3.2.0", "DeinName")]
[assembly: MelonGame("Oddworld Inhabitants", "Oddworld: New 'n' Tasty")]

namespace OddworldAccess
{
    public class OddworldMod : MelonMod
    {
        private GameObject lastSelectedObj = null;
        private string lastReadText = "";
        private string lastSubtitle = "";
        private float subCheckTimer = 0f;

        [Obsolete] 
        public override void OnApplicationStart()
        {
            TolkHelper.Load();
            NavAudioSystem.Initialize();
            TolkHelper.Speak("Access Mod Loaded. Press B for Beacon, N for Navi.");
        }

        public override void OnApplicationQuit()
        {
            TolkHelper.Unload();
        }

        public override void OnUpdate()
        {
            // --- TASTE B: AUDIO BEACON (Vorher N) ---
            if (Input.GetKeyDown(KeyCode.B))
            {
                NavAudioSystem.IsAudioEnabled = !NavAudioSystem.IsAudioEnabled;
                TolkHelper.Speak(NavAudioSystem.IsAudioEnabled ? "Beacon On" : "Beacon Off");
            }

            // --- TASTE T: STATUS REPORT ---
            if (Input.GetKeyDown(KeyCode.T))
            {
                StatusReader.ReportStatus();
            }

            // Untertitel Timer (alle 0.2 Sekunden)
            if (Time.time > subCheckTimer)
            {
                subCheckTimer = Time.time + 0.2f; 
                CheckSubtitles();
            }

            // --- MENÜ LOGIK (BLEIBT GLEICH) ---
            try
            {
                if (IsMoviePlaying()) return;

                GameObject currentObj = UICamera.selectedObject;
                if (currentObj == null) currentObj = UICamera.hoveredObject;

                if (currentObj != null && currentObj != lastSelectedObj)
                {
                    string textToRead = "";

                    // Spezialbehandlung für Spielstände
                    Component btnComponent = currentObj.GetComponent("SaveSlotSelectionButton");
                    
                    if (btnComponent != null)
                    {
                        int slotID = (int)AccessTools.Field(btnComponent.GetType(), "m_nSlot").GetValue(btnComponent);
                        textToRead = GetSaveSlotDataString(slotID);
                    }
                    else
                    {
                        textToRead = AnalyzeObject(currentObj);
                    }

                    if (!string.IsNullOrEmpty(textToRead) && textToRead != lastReadText)
                    {
                        if (!textToRead.Contains("UIContent"))
                        {
                            MelonLogger.Msg($"[UI] {textToRead}");
                            TolkHelper.Speak(textToRead);
                        }
                        lastReadText = textToRead;
                    }
                    lastSelectedObj = currentObj;
                }
            }
            catch { }
        }

        private string GetSaveSlotDataString(int slotNum)
        {
            try
            {
                Type appType = AccessTools.TypeByName("App");
                object appInstance = AccessTools.Method(appType, "getInstance").Invoke(null, null);
                if (appInstance == null) return "Game not ready";

                var saveLoadField = AccessTools.Field(appType, "m_saveload");
                object saveLoad = saveLoadField.GetValue(appInstance);
                if (saveLoad == null) return "Save System Error";

                var slotDataField = AccessTools.Field(saveLoad.GetType(), "m_cSaveSlots");
                object saveSlotData = slotDataField.GetValue(saveLoad);
                if (saveSlotData == null) return "No Save Data";

                string fieldName = $"m_cSaveSlot{slotNum}";
                var individualSlotField = AccessTools.Field(saveSlotData.GetType(), fieldName);
                object individualSlot = individualSlotField.GetValue(saveSlotData);

                if (individualSlot == null) return $"Slot {slotNum}: Empty";

                Type t = individualSlot.GetType();
                string chapter = AccessTools.Field(t, "m_eChapter").GetValue(individualSlot).ToString();
                string percent = (string)AccessTools.Field(t, "m_strMudsRescuedPercentage").GetValue(individualSlot);
                string difficulty = AccessTools.Field(t, "m_eDifficulty").GetValue(individualSlot).ToString();
                float timeSeconds = (float)AccessTools.Field(t, "m_fTimePlayed").GetValue(individualSlot);
                
                TimeSpan time = TimeSpan.FromSeconds(timeSeconds);
                string timeString = $"{time.Hours}h {time.Minutes}m";
                string niceChapter = Regex.Replace(chapter, "(\\B[A-Z])", " $1"); 

                return $"Slot {slotNum}: {niceChapter}, {percent}, {difficulty}, {timeString}";
            }
            catch (Exception ex)
            {
                return $"Slot {slotNum} Error";
            }
        }

        private string AnalyzeObject(GameObject obj)
        {
            if (obj == null) return "";

            UIToggle toggle = obj.GetComponent<UIToggle>();
            if (toggle != null)
            {
                string state = toggle.value ? "On" : "Off";
                string label = FindNameForControl(obj); 
                return $"{label}: {state}";
            }

            UISlider slider = obj.GetComponent<UISlider>();
            if (slider != null)
            {
                int percent = (int)(slider.value * 100);
                string name = FindNameForControl(obj);
                return $"{name}: {percent} percent";
            }

            UIPopupList popup = obj.GetComponent<UIPopupList>();
            if (popup != null) return $"{popup.value}";

            string resultText = FindTextInChildren(obj);
            if (string.IsNullOrEmpty(resultText) && obj.transform.childCount > 0)
            {
                foreach (Transform child in obj.transform)
                {
                    string childText = FindTextInChildren(child.gameObject);
                    if (!string.IsNullOrEmpty(childText)) resultText += childText + " ";
                }
            }
            return CleanText(resultText);
        }

        private string FindNameForControl(GameObject go)
        {
            string text = FindTextInChildren(go);
            if (!string.IsNullOrEmpty(text)) return CleanText(text);
            if (go.transform.parent != null) return go.transform.parent.name;
            return "Setting";
        }

        private string FindTextInChildren(GameObject go)
        {
            UILabel label = go.GetComponent<UILabel>();
            if (label != null && !string.IsNullOrEmpty(label.text)) return label.text;
            
            foreach (Transform child in go.transform)
            {
                UILabel cLabel = child.GetComponent<UILabel>();
                if (cLabel != null && !string.IsNullOrEmpty(cLabel.text)) return cLabel.text;
            }
            return "";
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, @"\[.*?\]", "").Trim();
        }

        private void CheckSubtitles()
        {
            if (!IsMoviePlaying()) { lastSubtitle = ""; return; }

            UILabel[] labels = GameObject.FindObjectsOfType<UILabel>();
            foreach (var label in labels)
            {
                if (label != null && label.gameObject.activeInHierarchy && label.enabled)
                {
                    string txt = label.text;
                    if (!string.IsNullOrEmpty(txt) && txt.Length > 2 && txt != lastSubtitle)
                    {
                        string clean = CleanText(txt);
                        if (!string.IsNullOrEmpty(clean))
                        {
                            TolkHelper.Speak(clean);
                            lastSubtitle = txt; 
                            return; 
                        }
                    }
                }
            }
        }

        private bool IsMoviePlaying()
        {
            try {
                Type appType = AccessTools.TypeByName("App");
                object appInstance = AccessTools.Method(appType, "getInstance").Invoke(null, null);
                return (bool)AccessTools.Method(appType, "IsMoviePlaying").Invoke(appInstance, null);
            } catch { return false; }
        }
    }
}