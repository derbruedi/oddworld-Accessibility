using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OddworldAccess
{
    [HarmonyPatch(typeof(App), "Update")]
    public static class AbeNavSystem
    {
        // ==========================================================
        // KONFIGURATION
        // ==========================================================
        
        public enum ScanCategory { All, Interact, Door, Danger, Enemy, Friend, Platform, Pickup, Portal, Secret }
        static ScanCategory currentCategory = ScanCategory.All;

        static List<GameObject> foundObjects = new List<GameObject>();
        static int currentObjectIndex = 0;
        
        public static GameObject CurrentTarget = null;
        public static string LastReadText = "No text read yet.";
        public static Vector3 PlayerPosition = Vector3.zero;

        private static string chantBuffer = "";
        private static float lastChantTime = 0f;
        
        private static bool reflectionInit = false;
        private static int floorMask = -1;
        private static MethodInfo mPostEvent;
        private static float? lockedHeight = null; 

        // Grinder Status
        private static bool grindersDisabled = false;

        // ==========================================================
        // HAUPTSCHLEIFE (UPDATE)
        // ==========================================================

        public static void CollectChantWord(string word)
        {
            if (Time.time > lastChantTime + 10f) chantBuffer = "";
            if (!chantBuffer.EndsWith(word + " "))
            {
                chantBuffer += word + " ";
                lastChantTime = Time.time;
            }
        }

        static void Postfix(App __instance)
        {
            if (!reflectionInit)
            {
                try { 
                    Type pmType = AccessTools.TypeByName("PhysicsMasks");
                    if (pmType != null) floorMask = (int)AccessTools.Field(pmType, "FloorLayerMask").GetValue(null);
                    
                    Type soundType = AccessTools.TypeByName("AkSoundEngine");
                    if (soundType != null) mPostEvent = AccessTools.Method(soundType, "PostEvent", new Type[] { typeof(string), typeof(GameObject) });
                } catch {}
                reflectionInit = true;
            }

            var abeField = AccessTools.Field(typeof(App), "m_Abe");
            if (abeField == null) return;
            var abeObj = abeField.GetValue(__instance) as MonoBehaviour;
            if (abeObj == null) return;
            Abe realAbe = abeObj as Abe;

            Vector3 finalPos = GetPosition(realAbe);
            PlayerPosition = finalPos;
            NavAudioSystem.PlayerPosition = finalPos;

            HandleInput(realAbe);
            UpdateAssistSystems(realAbe);
        }

        // ==========================================================
        // HELFER: ABE / ELUM POSITION
        // ==========================================================
        
        static MonoBehaviour GetActiveMover(Abe abe)
        {
            try 
            {
                var field = AccessTools.Field(typeof(Abe), "m_cAbeElum");
                if (field != null)
                {
                    var abeElum = field.GetValue(abe) as MonoBehaviour;
                    if (abeElum != null && abeElum.gameObject.activeInHierarchy)
                    {
                        return abeElum; 
                    }
                }
            } 
            catch {}
            return abe;
        }

        static Vector3 GetPosition(Abe abe)
        {
            MonoBehaviour mover = GetActiveMover(abe);
            return mover.transform.position;
        }

        // ==========================================================
        // ASSISTENTEN (M, G, P, K)
        // ==========================================================

        static void UpdateAssistSystems(Abe abe)
        {
            // SHRYKULL (Taste G)
            if (Input.GetKeyDown(KeyCode.G)) ActivateShrykullWithSound(abe);

            // GRINDER KILLER (Taste K)
            if (Input.GetKeyDown(KeyCode.K)) ToggleGrinderSafety();

            // SCHWEBEN / LEVITATION (Taste M halten)
            MonoBehaviour mover = GetActiveMover(abe);

            if (Input.GetKeyDown(KeyCode.M))
            {
                lockedHeight = mover.transform.position.y;
                TolkHelper.Speak("Floating");
            }

            if (Input.GetKey(KeyCode.M) && lockedHeight.HasValue)
            {
                SetGravity(mover, false);
                Vector3 currentPos = mover.transform.position;
                mover.transform.position = new Vector3(currentPos.x, lockedHeight.Value, currentPos.z);
                Rigidbody rb = mover.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 v = rb.velocity;
                    rb.velocity = new Vector3(v.x, 0f, v.z);
                }
            }

            if (Input.GetKeyUp(KeyCode.M))
            {
                lockedHeight = null;
                SetGravity(mover, true);
                TolkHelper.Speak("Gravity On");
            }
        }

        static void ToggleGrinderSafety()
        {
            grindersDisabled = !grindersDisabled;
            int count = 0;
            try
            {
                if (Grinder.m_lstGrinders != null)
                {
                    foreach (var grinder in Grinder.m_lstGrinders)
                    {
                        if (grinder == null) continue;
                        count++;
                        grinder.enabled = !grindersDisabled;
                        var colliders = grinder.GetComponentsInChildren<Collider>();
                        foreach (var c in colliders) c.enabled = !grindersDisabled;
                        var animators = grinder.GetComponentsInChildren<Animator>();
                        foreach (var a in animators) a.enabled = !grindersDisabled;
                    }
                }
            }
            catch(Exception ex) { MelonLogger.Error("Grinder Toggle Error: " + ex.Message); }
            string state = grindersDisabled ? "Disabled (Safe)" : "Active (Danger)";
            TolkHelper.Speak($"Grinders {state}. Affected: {count}");
        }

        static void SetGravity(MonoBehaviour character, bool enabled)
        {
            try
            {
                PropertyInfo prop = character.GetType().GetProperty("GravityEnabled");
                if (prop != null) prop.SetValue(character, enabled, null);
            }
            catch {}
        }

        static void ActivateShrykullWithSound(Abe abe)
        {
            try
            {
                MethodInfo addCharges = AccessTools.Method(typeof(Abe), "AddShrykullCharges", new Type[] { typeof(int) });
                if (addCharges != null) addCharges.Invoke(abe, new object[] { 1 });

                if (mPostEvent != null)
                {
                    mPostEvent.Invoke(null, new object[] { "Play_shrykrull_ready", abe.gameObject });
                    mPostEvent.Invoke(null, new object[] { "Play_vox_abe_laugh", abe.gameObject });
                }
                TolkHelper.Speak("Shrykull Ready!");
            }
            catch { TolkHelper.Speak("Error activating power."); }
        }

        static void TeleportToTarget(Abe abe)
        {
            if (CurrentTarget == null) { TolkHelper.Speak("No target."); return; }

            MonoBehaviour mover = GetActiveMover(abe);
            string moverName = (mover.GetType().Name == "AbeElum") ? "Elum" : "Abe";

            Vector3 tPos = CurrentTarget.transform.position;
            Vector3 mPos = mover.transform.position;
            
            float offset = (tPos.x > mPos.x) ? -1.5f : 1.5f;
            string tName = CurrentTarget.name.ToLower();
            if (tName.Contains("door") || tName.Contains("well") || tName.Contains("portal")) offset = 0f;

            float targetY = tPos.y;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(tPos.x + offset, tPos.y + 2f, mPos.z), Vector3.down, out hit, 5f, (floorMask != -1) ? floorMask : 1))
            {
                targetY = hit.point.y + 0.1f;
            }

            Vector3 finalPos = new Vector3(tPos.x + offset, targetY, mPos.z);

            try
            {
                var cc = mover.GetComponent<CharacterController>();
                var rb = mover.GetComponent<Rigidbody>();
                if (cc) cc.enabled = false;
                if (rb) { rb.isKinematic = true; rb.velocity = Vector3.zero; }

                mover.transform.position = finalPos;
                
                var splineField = AccessTools.Field(mover.GetType(), "m_cSmartSplineController");
                if (splineField != null) {
                    object spline = splineField.GetValue(mover);
                    if (spline != null) {
                        AccessTools.Method(spline.GetType(), "Search", new Type[]{typeof(bool)}).Invoke(spline, new object[]{true});
                    }
                }

                if (rb) rb.isKinematic = false;
                if (cc) cc.enabled = true;

                TolkHelper.Speak($"Teleported {moverName}");
            }
            catch { TolkHelper.Speak("Teleport error"); }
        }

        // ==========================================================
        // INPUT HANDLER
        // ==========================================================

        static void HandleInput(Abe realAbe)
        {
            Vector3 centerPos = GetPosition(realAbe);

            // --- NEU: NAVI SCAN AUF TASTE N ---
            if (Input.GetKeyDown(KeyCode.N))
            {
                ScanForPaths(centerPos);
            }

            // --- SCANNER KATEGORIEN ---
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.BackQuote)) ChangeCategory(centerPos, 1);
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Backspace)) ChangeCategory(centerPos, -1);
            
            // --- OBJEKTE DURCHSCHALTEN ---
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus)) CycleObject(centerPos, 1);
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.Comma) || Input.GetKeyDown(KeyCode.KeypadMinus)) CycleObject(centerPos, -1);
            
            // --- INFO ---
            if (Input.GetKeyDown(KeyCode.Y)) SpeakTargetInfo(centerPos);
            if (Input.GetKeyDown(KeyCode.P)) TeleportToTarget(realAbe);
            
            if (Input.GetKeyDown(KeyCode.F1)) TolkHelper.Speak($"X: {Math.Round(centerPos.x)}, Y: {Math.Round(centerPos.y)}");
            
            if (Input.GetKeyDown(KeyCode.L)) {
                if (!string.IsNullOrEmpty(chantBuffer) && (Time.time < lastChantTime + 20f)) TolkHelper.Speak("Msg: " + chantBuffer);
                else TolkHelper.Speak("Info: " + LastReadText);
            }
            if (Input.GetKeyDown(KeyCode.H) && realAbe != null) CheckAbeHealth(realAbe);
        }

        // ==========================================================
        // NEU: PFAD-NAVI LOGIK (Findet Pfade im Spline-System)
        // ==========================================================
        static void ScanForPaths(Vector3 abePos)
        {
            // Wir suchen nach 'BezierSplineGO', das ist die Klasse für Wege im Spiel
            Type splineType = AccessTools.TypeByName("BezierSplineGO");
            if (splineType == null) { TolkHelper.Speak("Error: Spline system not found."); return; }

            var allSplines = UnityEngine.Object.FindObjectsOfType(splineType);
            GameObject closestPath = null;
            float minDistance = 9999f;
            int pathCount = 0;

            foreach (var splineObj in allSplines)
            {
                MonoBehaviour splineScript = splineObj as MonoBehaviour;
                if (splineScript == null) continue;

                float dist = Vector3.Distance(splineScript.transform.position, abePos);
                
                // Wir suchen nur Pfade in 30 Meter Umkreis
                if (dist < 30.0f) 
                {
                    pathCount++;
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPath = splineScript.gameObject;
                    }
                }
            }

            if (closestPath != null)
            {
                // Namen säubern für bessere Sprachausgabe
                string pathName = closestPath.name.Replace("BezierSpline", "Path").Replace("_", " ");
                TolkHelper.Speak($"Nearest Path: {pathName}. Distance: {Math.Round(minDistance)} meters.");
            }
            else
            {
                TolkHelper.Speak("No navigation paths nearby.");
            }
        }

        // ==========================================================
        // SCANNER LOGIK (OBJEKTE)
        // ==========================================================

        static void SpeakTargetInfo(Vector3 centerPos, bool resort = true) {
            if (resort) SortObjectsByDistance(centerPos);
            
            if (foundObjects.Count == 0) { 
                RefreshObjectList(centerPos); 
                if (foundObjects.Count == 0) { 
                    TolkHelper.Speak($"No {currentCategory}."); 
                    CurrentTarget = null; 
                    return; 
                } 
            }
            
            if (currentObjectIndex >= foundObjects.Count) currentObjectIndex = 0;
            if (currentObjectIndex < 0) currentObjectIndex = 0;
            
            GameObject target = foundObjects[currentObjectIndex];
            if (target == null || !target.activeInHierarchy) { 
                RefreshObjectList(centerPos); 
                TolkHelper.Speak("Gone."); 
                CurrentTarget = null; 
                return; 
            }
            
            CurrentTarget = target;
            Vector3 diff = target.transform.position - centerPos;
            
            string dirH = (diff.x > 0.5f) ? "Right" : (diff.x < -0.5f) ? "Left" : "Center";
            string dirV = "";
            string navHint = "";

            if (diff.y > 2.5f) {
                dirV = ", High Up";
                navHint = FindVerticalHelper(centerPos);
                if (navHint == "") navHint = ". Look for a ledge.";
            } else if (diff.y < -2.5f) {
                dirV = ", Deep Down";
                navHint = FindVerticalHelper(centerPos);
                if (navHint == "") navHint = ". Look for a drop.";
            }
            
            string extraInfo = "";
            string cleanName = CleanObjectName(target);

            if (cleanName.Contains("Sign") || cleanName.Contains("Stone") || cleanName.Contains("Floating")) {
                string s = GetTextFromObject(target);
                if (s.Length > 2) extraInfo = $". Reads: {s}";
            }

            TolkHelper.Speak($"{cleanName} {currentObjectIndex+1} of {foundObjects.Count}. {(int)diff.magnitude} steps {dirH}{dirV}{extraInfo}{navHint}");
        }

        static string FindVerticalHelper(Vector3 playerPos)
        {
            float searchRadius = 15.0f;
            GameObject bestHelper = null;
            float closestDist = float.MaxValue;
            List<GameObject> helpers = new List<GameObject>();
            
            // Suche nach Hilfsmitteln (Aufzüge, Portale)
            helpers.AddRange(FindAllActive<Elevator>());
            helpers.AddRange(FindAllActive<CargoElevator>());
            helpers.AddRange(FindAllActive<Well>());
            helpers.AddRange(FindAllActive<JumpPad>());

            foreach (var h in helpers)
            {
                float d = Vector3.Distance(h.transform.position, playerPos);
                if (d < searchRadius && d < closestDist)
                {
                    closestDist = d;
                    bestHelper = h;
                }
            }

            if (bestHelper != null)
            {
                Vector3 hDiff = bestHelper.transform.position - playerPos;
                string hDir = (hDiff.x > 1.0f) ? "Right" : (hDiff.x < -1.0f) ? "Left" : "here";
                return $". Try {CleanObjectName(bestHelper)} {hDir}.";
            }
            return "";
        }

        static List<GameObject> FindAllActive<T>() where T : Component
        {
            var list = new List<GameObject>();
            var found = UnityEngine.Object.FindObjectsOfType(typeof(T)) as T[];
            if (found != null)
            {
                foreach(var f in found) if(f.gameObject.activeInHierarchy) list.Add(f.gameObject);
            }
            return list;
        }

        static void RefreshObjectList(Vector3 centerPos) {
            foundObjects.Clear(); 
            currentObjectIndex = 0; 
            CurrentTarget = null;
            try {
                if (currentCategory == ScanCategory.All) AddAllTypes();
                else switch (currentCategory) {
                    case ScanCategory.Interact: AddInteractables(); break;
                    case ScanCategory.Door:     AddDoors(); break;
                    case ScanCategory.Danger:   AddDangers(); break;
                    case ScanCategory.Enemy:    AddEnemies(); break;
                    case ScanCategory.Friend:   AddFriends(); break;
                    case ScanCategory.Platform: AddPlatforms(); break;
                    case ScanCategory.Portal:   AddPortals(); break;
                    case ScanCategory.Pickup:   AddPickups(); break;
                    case ScanCategory.Secret:   AddSecrets(); break;
                }
                foundObjects = foundObjects.Distinct().ToList();
                foundObjects.RemoveAll(child => IsChildOfAnyInList(child));
                SortObjectsByDistance(centerPos);
            } catch {}
        }

        static bool IsChildOfAnyInList(GameObject child)
        {
            if (child == null) return true;
            Transform parent = child.transform.parent;
            while (parent != null)
            {
                if (foundObjects.Contains(parent.gameObject)) return true;
                parent = parent.parent;
            }
            return false;
        }

        static void SortObjectsByDistance(Vector3 centerPos) {
            if(foundObjects.Count > 0) {
                foundObjects.Sort((a,b) => {
                    if (a == null || b == null) return 0;
                    float distA = Vector3.SqrMagnitude(a.transform.position - centerPos);
                    float distB = Vector3.SqrMagnitude(b.transform.position - centerPos);
                    return distA.CompareTo(distB);
                });
            }
        }

        static void ChangeCategory(Vector3 p, int d) {
            int c = (int)currentCategory + d;
            int max = Enum.GetNames(typeof(ScanCategory)).Length;
            if (c >= max) c = 0; 
            if (c < 0) c = max - 1;
            currentCategory = (ScanCategory)c; 
            TolkHelper.Speak($"Scanner: {currentCategory}"); 
            RefreshObjectList(p);
        }

        static void CycleObject(Vector3 p, int d) {
            SortObjectsByDistance(p);
            if(foundObjects.Count > 0) { 
                currentObjectIndex += d; 
                if(currentObjectIndex >= foundObjects.Count) currentObjectIndex = 0; 
                if(currentObjectIndex < 0) currentObjectIndex = foundObjects.Count - 1; 
                SpeakTargetInfo(p, false); 
            }
            else { 
                RefreshObjectList(p); 
                if(foundObjects.Count == 0) TolkHelper.Speak("None."); 
                else SpeakTargetInfo(p, false); 
            }
        }

        static void AddAllTypes() { 
            AddInteractables(); AddDoors(); AddDangers(); AddEnemies(); 
            AddFriends(); AddPlatforms(); AddPortals(); AddPickups(); AddSecrets(); 
        }

        static void AddInteractables() {
            AddObjects<Lever>(); AddObjects<RingPull>(); AddObjects<ChimeBells>(); 
            AddObjectsByName("Wheel"); AddObjectsByName("Valve"); AddObjectsByName("Switch");
            AddObjectsByName("Button"); AddObjectsByName("Chain"); AddObjectsByName("Bell"); AddObjectsByName("Chime");
        }

        static void AddPickups() {
            AddObjects<PickUp>(); 
            AddObjectsByName("Sack"); AddObjectsByName("Bag"); AddObjectsByName("BonePile");
            AddObjectsByName("Debris"); AddObjectsByName("Barrel"); AddObjectsByName("Coin");       
        }

        static void AddSigns() { 
            AddObjects<TextPrint>(); AddObjects<DirectoryStoryStone>(); 
            AddObjectsByName("Hint"); AddObjectsByName("Fly"); AddObjectsByName("Message"); AddObjectsByName("Sign"); 
        }

        static void AddDoors() { AddObjects<Door>(); AddObjects<Hatch>(); AddObjects<SidewaysDoor>(); }

        static void AddDangers() { 
            AddObjects<Explosive>(); AddObjects<TimedMine>(); AddObjects<ToggleMine>(); AddObjects<ProximityMine>(); AddObjects<ChantSuppressor>(); 
            AddObjectsByName("Grinder"); AddObjectsByName("Zap"); AddObjectsByName("Electric"); AddObjectsByName("UXB"); 
            AddObjectsByName("Motion"); AddObjectsByName("Laser"); AddObjectsByName("Beam"); AddObjectsByName("Slicer"); 
        }

        static void AddEnemies() { 
            AddObjects<Slig>(); AddObjects<Scrab>(); AddObjects<Paramite>(); AddObjects<Slog>(); AddObjects<Bat>(); AddObjects<Bees>(); AddObjectsByName("Greeter"); 
        }

        static void AddFriends() { AddObjects<MudokonSlave>(); AddObjectsByName("Elum"); }
        static void AddPlatforms() { AddObjects<Elevator>(); AddObjects<CargoElevator>(); AddObjects<JumpPad>(); }
        static void AddPortals() { AddObjects<Well>(); AddObjectsByName("Door"); }
        static void AddSecrets() { AddObjectsByName("Secret"); AddObjectsByName("Hidden"); }

        static void AddObjects<T>() where T : Component {
            var l = UnityEngine.Object.FindObjectsOfType(typeof(T)) as T[];
            if(l != null) {
                foreach(var i in l) {
                    if(!i.gameObject.activeInHierarchy) continue;
                    if (i is PickUp) { if (!IsVisible(i.gameObject)) continue; }
                    if(!foundObjects.Contains(i.gameObject)) foundObjects.Add(i.gameObject);
                }
            }
        }

        static void AddObjectsByName(string n) {
            foreach(GameObject g in UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                if(g.activeInHierarchy && !foundObjects.Contains(g)) {
                    if (g.name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0) {
                        foundObjects.Add(g);
                    }
                }
            }
        }

        static bool IsVisible(GameObject go) {
            var r = go.GetComponent<Renderer>();
            if (r != null && r.enabled) return true;
            var childRenderers = go.GetComponentsInChildren<Renderer>();
            foreach(var cr in childRenderers) { if (cr.enabled) return true; }
            return false;
        }

        static string CleanObjectName(GameObject t) {
            string n = t.name.Replace("(Clone)","").Trim();
            if(n.Contains("RockSack") || n.Contains("Debris")) return "Stone Sack";
            if(n.Contains("MeatSack") || n.Contains("Bone")) return "Meat Sack";
            if(n.Contains("Char_")) n = n.Replace("Char_", "");
            
            if(t.GetComponent("ChimeBells")) return "Chime Bell" + GetChimeStatus(t);
            if(t.GetComponent("TextPrint")) return "Floating Text";
            else if(n.Contains("Directory")) return "Story Stone";
            else if(n.Contains("Motion")) return "Motion Detector";
            else if(n.Contains("Electric")) return "Electric Gate";
            
            return n;
        }

        static string GetChimeStatus(GameObject go) {
            try {
                var bell = go.GetComponent("ChimeBells");
                if (bell == null) return "";
                var propPlayed = AccessTools.Property(bell.GetType(), "tunePlayed");
                if (propPlayed != null) {
                    bool done = (bool)propPlayed.GetValue(bell, null);
                    if (done) return " (Done)";
                }
                var propUnlocked = AccessTools.Property(bell.GetType(), "unlocked");
                if (propUnlocked != null) {
                    bool active = (bool)propUnlocked.GetValue(bell, null);
                    return active ? " (Active)" : " (Locked)";
                }
            } catch {}
            return "";
        }

        static void CheckAbeHealth(Abe abe) {
            try {
                var hp = AccessTools.Field(typeof(Abe).BaseType, "m_fHealth").GetValue(abe);
                TolkHelper.Speak($"Health: {hp}");
            } catch {}
        }

        static string GetTextFromObject(GameObject go) {
            try {
                var tp = go.GetComponent("TextPrint");
                if(tp!=null) {
                    var f = AccessTools.Field(tp.GetType(), "curtext");
                    if(f!=null) return CleanText((string)f.GetValue(tp));
                }
            } catch {}
            return "";
        }
        
        static string CleanText(string i) { return string.IsNullOrEmpty(i) ? "" : Regex.Replace(i.Replace("\n"," "), @"\[.*?\]", "").Trim(); }
    }
}