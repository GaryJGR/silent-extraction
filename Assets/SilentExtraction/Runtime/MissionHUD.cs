using UnityEngine;

namespace SilentExtraction
{
    /// <summary>Immediate-mode prototype HUD; resolution-independent coordinates.</summary>
    public sealed class MissionHUD : MonoBehaviour
    {
        readonly Color mint = new Color(.43f, .98f, .8f);
        readonly Color ink = new Color(.88f, .94f, 1);
        readonly Color muted = new Color(.55f, .65f, .76f);
        GUIStyle label, title, small, button;
        void Init()
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 17, normal = { textColor = ink } };
            title = new GUIStyle(label) { fontSize = 42, fontStyle = FontStyle.Bold };
            small = new GUIStyle(label) { fontSize = 12, normal = { textColor = muted } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        }
        void Text(Rect rect, string value, GUIStyle style, Color? color = null)
        {
            Color previous = GUI.color;
            if (color.HasValue) GUI.color = color.Value;
            GUI.Label(rect, value, style);
            GUI.color = previous;
        }
        void Panel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
        void OnGUI()
        {
            var m = MissionController.Instance;
            if (m == null) return;
            if (label == null) Init();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            float w = Screen.width / scale, h = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            Panel(new Rect(0, 0, w, 99), new Color(.025f, .045f, .075f, .95f));
            Text(new Rect(28, 14, 500, 22), "S / E     SILENT EXTRACTION", label, mint);
            Text(new Rect(28, 48, 620, 35), m.HasData ? "02 / RETURN TO EXTRACTION" : "01 / INFILTRATE & SECURE THE DATA CORE", small);
            Text(new Rect(w - 310, 17, 270, 25), $"{m.Elapsed:000.0}s     /     DECOYS  {m.Decoys}", label);
            Text(new Rect(w - 310, 49, 300, 22), $"DETECTIONS {m.Detections}    |    TAB  /  AI DEBUG", small);

            float suspicion = m.guard.Suspicion;
            Panel(new Rect(w / 2 - 160, 111, 320, 5), new Color(.1f, .17f, .22f));
            if (suspicion > .001f) Panel(new Rect(w / 2 - 160, 111, 320 * suspicion, 5), m.guard.StateColor);
            Text(new Rect(w / 2 - 160, 121, 340, 25),
                m.guard.State == GuardState.Chase ? "DETECTED / BREAK LINE OF SIGHT" : suspicion > 0 ? "GUARD SUSPICION" : "STAY OUT OF THE AMBER FIELD OF VIEW", small);

            Panel(new Rect(0, h - 63, w, 63), new Color(.025f, .045f, .075f, .96f));
            Text(new Rect(27, h - 48, w - 40, 28), "WASD  Move    |    Hold C  Crouch    |    Shift  Sprint    |    E  Interact    |    Aim + Q  Decoy    |    Esc  Pause", small);
            if (m.Phase == MissionPhase.Playing)
            {
                Text(new Rect(28, h - 95, 600, 25), m.player.IsCrouching ? "CROUCHED / LOW VISIBILITY & QUIET" :
                    m.player.IsSprinting ? "SPRINTING / FOOTSTEPS CARRY FARTHER" : "WALKING / USE COVER", small, mint);
                if (!string.IsNullOrEmpty(m.Prompt))
                {
                    Panel(new Rect(w / 2 - 240, h - 150, 480, 43), new Color(.03f, .1f, .13f, .96f));
                    Text(new Rect(w / 2 - 220, h - 143, 440, 28), m.Prompt, label, mint);
                }
                if (!string.IsNullOrEmpty(m.Message)) Text(new Rect(28, 163, w - 60, 28), m.Message, small, mint);
            }
            if (m.DebugOverlay)
            {
                Panel(new Rect(25, 213, 295, 168), new Color(.025f, .045f, .075f, .92f));
                Text(new Rect(40, 225, 270, 25), "GUARD / LIVE TELEMETRY", small, mint);
                Text(new Rect(40, 254, 270, 120), $"State: {m.guard.State}\nLine of sight: {m.guard.SeesPlayer}\nSuspicion: {suspicion:P0}\nPlayer noise: {m.player.NoiseRadius:0.0} m", label);
            }
            if (m.Phase != MissionPhase.Playing) Overlay(m, w, h);
        }
        void Overlay(MissionController m, float w, float h)
        {
            Panel(new Rect(0, 99, w, h - 162), new Color(.015f, .03f, .055f, .78f));
            float x = w / 2 - 310, y = h / 2 - 170;
            string heading, copy, action;
            switch (m.Phase)
            {
                case MissionPhase.Briefing:
                    heading = "Enter quietly.\nLeave with the data.";
                    copy = "Infiltrate the facility, secure the cyan data core, then return\nto the green extraction pad. Walls block the guard's vision.\n\nCrouch to reduce detection. Aim and press Q to distract.\nIf spotted, break line of sight and change your route.";
                    action = "BEGIN INFILTRATION   /   ENTER";
                    break;
                case MissionPhase.Paused:
                    heading = "Operation paused.";
                    copy = "Your progress is held.\n\nResume when ready, or restart the operation.";
                    action = "RESUME   /   ESC";
                    break;
                case MissionPhase.Won:
                    heading = "Extraction complete.";
                    copy = $"Data secured in {m.Elapsed:0.0} seconds.\nDetections: {m.Detections}     |     Decoys remaining: {m.Decoys}\n\nBest extraction: {m.BestTime:0.0}s\n" + (m.Detections == 0 ? "GHOST / No confirmed sightings." : "Try a quieter route for a ghost extraction.");
                    action = "PLAY AGAIN   /   R";
                    break;
                default:
                    heading = "You've been caught.";
                    copy = "The guard reached your position.\n\nUse walls to break sight, crouch past patrols, and\nthrow a decoy to open a safer route.";
                    action = "RETRY OPERATION   /   R";
                    break;
            }
            Text(new Rect(x, y - 30, 620, 25), "FIELD OPERATION  /  001", small, mint);
            Text(new Rect(x, y, 710, 116), heading, title);
            Text(new Rect(x, y + 120, 700, 154), copy, label);
            if (GUI.Button(new Rect(x, y + 288, 420, 49), action, button))
            {
                if (m.Phase == MissionPhase.Briefing) m.Begin();
                else if (m.Phase == MissionPhase.Paused) m.TogglePause();
                else m.Restart();
            }
            if (m.Phase == MissionPhase.Paused && GUI.Button(new Rect(x + 435, y + 288, 145, 49), "RESTART", button)) m.Restart();
            if (GUI.Button(new Rect(x, y + 349, 580, 42), "QUIT", button)) QuitGame();
        }
        void QuitGame()
        {
            Time.timeScale = 1;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
