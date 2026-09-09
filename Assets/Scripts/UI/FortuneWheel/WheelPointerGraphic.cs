using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI.FortuneWheel
{
    // A mesh keeps the pointer visible regardless of the selected font or language.
    public sealed class WheelPointerGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            mesh.AddVert(new Vector3(rect.center.x - rect.width * 0.25f, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.center.x + rect.width * 0.25f, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.center.x, rect.yMax - rect.height * 0.6f), color, Vector2.zero);
            mesh.AddTriangle(0, 1, 2);
        }
    }
}
