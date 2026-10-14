using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilentExtraction.Editor
{
    /// <summary>Small integration check against the real physics, scene, and mission components.</summary>
    public static class GameplayChecks
    {
        [MenuItem("Silent Extraction/4. Run Gameplay Checks (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play mode before running gameplay checks."); return; }
            var m = UnityEngine.Object.FindAnyObjectByType<MissionController>();
            bool hadBest = PlayerPrefs.HasKey("SilentExtraction.BestTime");
            float previousBest = PlayerPrefs.GetFloat("SilentExtraction.BestTime");
            try
            {
                Assert(m != null && m.Phase == MissionPhase.Briefing, "Run checks from a fresh briefing.");
                m.Begin();
                m.player.enabled = false;
                m.player.GetComponent<CharacterController>().enabled = false;
                m.guard.enabled = false;
                m.guard.Agent.isStopped = true;
                m.guard.Agent.Warp(new Vector3(5, 0, -6));
                m.guard.transform.rotation = Quaternion.identity;
                m.player.transform.position = new Vector3(5, 0, -1);
                Physics.SyncTransforms();
                Assert(m.guard.CanSeePlayer(), "Player in unobstructed forward cone must be visible.");
                m.player.transform.position = new Vector3(5, 0, -10);
                Physics.SyncTransforms();
                Assert(!m.guard.CanSeePlayer(), "Player behind guard must not be visible.");
                m.player.transform.position = new Vector3(5, 0, 12);
                Physics.SyncTransforms();
                Assert(!m.guard.CanSeePlayer(), "Player outside range must not be visible.");
                m.guard.Agent.Warp(new Vector3(-5, 0, 1));
                m.guard.transform.rotation = Quaternion.Euler(0, 90, 0);
                m.player.transform.position = new Vector3(4, 0, 1);
                Physics.SyncTransforms();
                Assert(!m.guard.CanSeePlayer(), "Central wall must block the line of sight.");
                m.guard.HearNoise(m.guard.transform.position + Vector3.back, 5);
                Assert(m.guard.State == GuardState.Investigate, "Audible noise must trigger investigation.");
                m.TogglePause();
                Assert(m.Phase == MissionPhase.Paused && Time.timeScale == 0, "Pause must freeze simulation.");
                m.TogglePause();
                Assert(m.Phase == MissionPhase.Playing && Time.timeScale == 1, "Resume must restore simulation.");
                m.player.transform.position = m.extraction.position;
                m.Interact();
                Assert(m.Phase == MissionPhase.Playing, "Extraction without data must not win.");
                m.player.transform.position = m.dataCore.position + Vector3.left;
                m.Interact();
                Assert(m.HasData && !m.dataCore.gameObject.activeSelf, "Interaction must collect data.");
                m.player.transform.position = m.extraction.position;
                m.Interact();
                Assert(m.Phase == MissionPhase.Won, "Data plus extraction interaction must win.");
                m.Caught();
                Assert(m.Phase == MissionPhase.Won, "A completed win cannot be overwritten by capture.");
                Debug.Log("SILENT_EXTRACTION: GAMEPLAY CHECKS PASS - sight, angle, range, occlusion, noise, pause, pickup, extraction and terminal-state protection.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
            finally
            {
                if (hadBest) PlayerPrefs.SetFloat("SilentExtraction.BestTime", previousBest);
                else PlayerPrefs.DeleteKey("SilentExtraction.BestTime");
                PlayerPrefs.Save();
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        static void Assert(bool condition, string reason)
        {
            if (!condition) throw new Exception("GAMEPLAY CHECK FAILED: " + reason);
        }
    }
}
