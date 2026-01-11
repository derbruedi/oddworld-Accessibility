using MelonLoader;
using UnityEngine;
using System;

namespace OddworldAccess
{
    public class NavAudioBeacon : MonoBehaviour
    {
        private AudioSource source;
        private float beepTimer = 0f;
        private bool ready = false;

        // Einstellungen für das Sonar (Geigerzähler-Logik)
        private const float MAX_DISTANCE = 25.0f; // Ab hier ist das Piepen am langsamsten
        private const float MIN_INTERVAL = 0.10f; // Schnellstes Piepen (ganz nah)
        private const float MAX_INTERVAL = 1.20f; // Langsamstes Piepen (weit weg)
        private const float PAN_WIDTH = 6.0f;     // Innerhalb von 6 Metern wandert der Ton zur Mitte

        void Start()
        {
            Invoke("InitAudio", 0.5f);
        }

        void InitAudio()
        {
            source = gameObject.AddComponent<AudioSource>();
            
            // Wir machen das Panning manuell
            source.panLevel = 0.0f; 
            source.volume = 0.6f;
            source.playOnAwake = false;
            source.loop = false;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.clip = CreateSoftPing();
            
            if (source.clip != null) ready = true;
        }

        private AudioClip CreateSoftPing()
        {
            int sampleRate = 44100;
            float frequency = 1100; 
            float lengthSec = 0.12f; 

            int sampleCount = (int)(sampleRate * lengthSec);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float wave = Mathf.Sin(2 * Mathf.PI * frequency * t);
                float envelope = Mathf.Exp(-20.0f * t); 
                samples[i] = wave * envelope;
            }

            AudioClip clip = AudioClip.Create("SoftPing", sampleCount, 1, sampleRate, false, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void Update()
        {
            if (!ready) return;
            // Hier greifen wir auf die statische Klasse zu, die unten definiert ist
            if (!NavAudioSystem.IsAudioEnabled) return;

            GameObject target = AbeNavSystem.CurrentTarget;
            
            if (target != null && target.activeInHierarchy)
            {
                Vector3 playerPos = NavAudioSystem.PlayerPosition;
                Vector3 targetPos = target.transform.position;
                
                // 1. Distanz berechnen
                float dist = Vector3.Distance(targetPos, playerPos);
                float deltaX = targetPos.x - playerPos.x;
                float deltaY = targetPos.y - playerPos.y;

                // 2. Intervall berechnen (Geigerzähler)
                float t = Mathf.Clamp01(dist / MAX_DISTANCE);
                float currentInterval = Mathf.Lerp(MIN_INTERVAL, MAX_INTERVAL, t);

                // 3. Panning (Links / Rechts)
                source.pan = Mathf.Clamp(deltaX / PAN_WIDTH, -1.0f, 1.0f); 

                // 4. Pitch für Oben/Unten
                float pitch = 1.0f;
                if (deltaY > 1.5f) pitch = 1.2f;      
                else if (deltaY < -1.5f) pitch = 0.8f; 
                source.pitch = pitch;

                // 5. Lautstärke basierend auf Distanz
                float vol = Mathf.Lerp(0.8f, 0.2f, t);
                source.volume = vol;

                // 6. Abspielen
                beepTimer -= Time.deltaTime;
                if (beepTimer <= 0f)
                {
                    source.PlayOneShot(source.clip);
                    beepTimer = currentInterval; 
                }
            }
        }
    }

    // --- DIESE KLASSE HAT GEFEHLT ---
    public static class NavAudioSystem
    {
        private static GameObject beaconObj;
        public static Vector3 PlayerPosition = Vector3.zero;
        public static bool IsAudioEnabled = true;

        public static void Initialize()
        {
            if (beaconObj == null)
            {
                beaconObj = new GameObject("Accessibility_AudioBeacon");
                beaconObj.AddComponent<NavAudioBeacon>();
                GameObject.DontDestroyOnLoad(beaconObj); 
            }
        }
    }
}