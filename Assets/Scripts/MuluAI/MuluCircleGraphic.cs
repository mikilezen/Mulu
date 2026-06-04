using UnityEngine;
using UnityEngine.UI;

namespace MuluAI
{
    [AddComponentMenu("Mulu AI/UI/Circle Graphic")]
    public sealed class MuluCircleGraphic : MaskableGraphic
    {
        [SerializeField] private int segmentCount = 64;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            int segments = Mathf.Clamp(segmentCount, 24, 128);
            Vector2 center = rect.center;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center;
            vh.AddVert(vertex);

            for (int i = 0; i <= segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(vertex);
            }

            for (int i = 1; i <= segments; i++)
            {
                vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
