using UnityEngine;

namespace SilentExtraction
{
    public sealed class PulseEffect : MonoBehaviour
    {
        LineRenderer line;
        Material material;
        float age;
        Color color;
        public static void Create(Vector3 position, Color color)
        {
            var pulse = new GameObject("Decoy sound pulse").AddComponent<PulseEffect>();
            pulse.transform.position = position + Vector3.up * .12f;
            pulse.color = color;
        }
        void Start()
        {
            line = gameObject.AddComponent<LineRenderer>();
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            line.widthMultiplier = .055f;
        }
        void Update()
        {
            age += Time.deltaTime;
            material.SetColor("_BaseColor", color * Mathf.Max(.1f, 1 - age / 2));
            float radius = .25f + age * 3;
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
            }
            if (age > 2) Destroy(gameObject);
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
