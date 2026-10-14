using UnityEngine;
using UnityEngine.Rendering;

namespace SilentExtraction
{
    /// <summary>A ray-clipped ground projection makes the guard's perception readable.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VisionCone : MonoBehaviour
    {
        public GuardBrain guard;
        public Material baseMaterial;
        const int Segments = 48;
        Mesh mesh;
        Vector3[] vertices = new Vector3[Segments + 2];
        int[] triangles = new int[Segments * 3];
        Material material;
        void Awake()
        {
            mesh = new Mesh { name = "Vision cone (runtime)" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().mesh = mesh;
            material = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            GetComponent<MeshRenderer>().material = material;
            GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < Segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        void LateUpdate()
        {
            if (guard == null) return;
            Vector3 origin = guard.transform.position;
            vertices[0] = transform.InverseTransformPoint(origin + Vector3.up * .06f);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = guard.transform.eulerAngles.y - guard.fieldOfView * .5f + guard.fieldOfView * i / Segments;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                float range = guard.viewDistance;
                if (Physics.Raycast(origin + Vector3.up * 1.45f, direction, out RaycastHit hit, range,
                    guard.obstructionMask, QueryTriggerInteraction.Ignore)) range = hit.distance;
                vertices[i + 1] = transform.InverseTransformPoint(origin + direction * range + Vector3.up * .06f);
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            Color color = guard.StateColor;
            color.a = .13f + guard.Suspicion * .12f;
            material.SetColor("_BaseColor", color);
        }
        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
