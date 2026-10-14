using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SilentExtraction
{
    public enum MissionPhase { Briefing, Playing, Paused, Won, Lost }

    /// <summary>Owns objective interaction and session state; UI only presents these values.</summary>
    public sealed class MissionController : MonoBehaviour
    {
        public static MissionController Instance { get; private set; }
        public PlayerMotor player;
        public GuardBrain guard;
        public Transform dataCore;
        public Transform extraction;
        public MissionPhase Phase { get; private set; } = MissionPhase.Briefing;
        public bool HasData { get; private set; }
        public int Decoys { get; private set; } = 3;
        public int Detections { get; private set; }
        public float Elapsed { get; private set; }
        public bool DebugOverlay { get; private set; }
        public string Message { get; private set; } = "";
        public string Prompt { get; private set; } = "";
        float messageUntil;
        float bestTime;
        public float BestTime => bestTime;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1;
            bestTime = PlayerPrefs.GetFloat("SilentExtraction.BestTime", 0);
            Application.targetFrameRate = 60;
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.tabKey.wasPressedThisFrame) DebugOverlay = !DebugOverlay;
                if (keyboard.enterKey.wasPressedThisFrame && Phase == MissionPhase.Briefing) Begin();
                if (keyboard.escapeKey.wasPressedThisFrame) TogglePause();
                if (keyboard.rKey.wasPressedThisFrame && Phase != MissionPhase.Playing) Restart();
            }
            if (Phase != MissionPhase.Playing) return;
            Elapsed += Time.deltaTime;
            if (Time.time > messageUntil) Message = "";
            Prompt = "";
            if (!HasData && Vector3.Distance(player.transform.position, dataCore.position) < 2.2f)
                Prompt = "E  /  SECURE THE DATA CORE";
            else if (Vector3.Distance(player.transform.position, extraction.position) < 2.4f)
                Prompt = HasData ? "E  /  EXTRACT" : "Find the data core before extracting";
            if (!HasData) dataCore.Rotate(0, 40 * Time.deltaTime, 0, Space.World);
        }
        public void Begin() => Phase = MissionPhase.Playing;
        public void TogglePause()
        {
            if (Phase == MissionPhase.Playing) { Phase = MissionPhase.Paused; Time.timeScale = 0; }
            else if (Phase == MissionPhase.Paused) { Phase = MissionPhase.Playing; Time.timeScale = 1; }
        }
        public void Restart()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public void Interact()
        {
            if (Phase != MissionPhase.Playing) return;
            Vector3 p = player.transform.position;
            if (!HasData && Vector3.Distance(p, dataCore.position) < 2.2f)
            {
                HasData = true;
                dataCore.gameObject.SetActive(false);
                Notify("DATA SECURED  /  Return to the green extraction pad");
            }
            else if (HasData && Vector3.Distance(p, extraction.position) < 2.4f)
            {
                Phase = MissionPhase.Won;
                if (bestTime <= 0 || Elapsed < bestTime)
                {
                    bestTime = Elapsed;
                    PlayerPrefs.SetFloat("SilentExtraction.BestTime", bestTime);
                    PlayerPrefs.Save();
                }
            }
        }
        public void Caught()
        {
            if (Phase == MissionPhase.Playing) Phase = MissionPhase.Lost;
        }
        public void RegisterDetection() => Detections++;
        public void EmitNoise(Vector3 position, float radius) => guard.HearNoise(position, radius);
        public void ThrowDecoy()
        {
            if (Phase != MissionPhase.Playing || Decoys <= 0) return;
            Vector3 position = player.transform.position + player.transform.forward * 6;
            if (Mouse.current != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    Vector3 target = ray.GetPoint(distance);
                    position = player.transform.position + Vector3.ClampMagnitude(target - player.transform.position, 9);
                }
            }
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 2, NavMesh.AllAreas))
            {
                Notify("Aim at reachable floor to throw a decoy");
                return;
            }
            Decoys--;
            guard.HearNoise(hit.position, 14);
            PulseEffect.Create(hit.position, new Color(.4f, .7f, 1));
            Notify("DECOY DEPLOYED  /  Guard investigates nearby sound");
        }
        public void Notify(string text) { Message = text; messageUntil = Time.time + 3; }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && Phase == MissionPhase.Playing) TogglePause();
        }
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1;
        }
    }
}
