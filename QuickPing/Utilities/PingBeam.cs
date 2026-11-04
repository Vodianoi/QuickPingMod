using UnityEngine;

namespace QuickPing.Utilities
{
    /// <summary>
    /// Vertical light beam that fades out where a ping lands, to find it from far away.
    /// </summary>
    internal class PingBeam : MonoBehaviour
    {
        private const float Height = 150f;
        private const float Width = 0.3f;
        private const float Lifetime = 3f;

        private static Material material;

        private LineRenderer line;
        private Color color;
        private float timer;

        public static void Show(Vector3 position, Color color)
        {
            if (!material)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (!shader) return;
                material = new Material(shader);
            }

            GameObject go = new GameObject("QuickPingBeam");
            go.transform.position = position;

            PingBeam beam = go.AddComponent<PingBeam>();
            beam.color = color;
            beam.line = go.AddComponent<LineRenderer>();
            beam.line.sharedMaterial = material;
            beam.line.useWorldSpace = true;
            beam.line.positionCount = 2;
            beam.line.SetPosition(0, position);
            beam.line.SetPosition(1, position + Vector3.up * Height);
            beam.line.startWidth = Width;
            beam.line.endWidth = Width;
            beam.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam.line.receiveShadows = false;
            beam.UpdateColor();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }
            UpdateColor();
        }

        private void UpdateColor()
        {
            float alpha = 1f - timer / Lifetime;
            line.startColor = new Color(color.r, color.g, color.b, 0.9f * alpha);
            line.endColor = new Color(color.r, color.g, color.b, 0f);
        }
    }
}
