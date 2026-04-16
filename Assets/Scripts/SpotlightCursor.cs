using UnityEngine;

public class SpotlightCursor : MonoBehaviour
{
    public Material spotlightMaterial;
    //public RectTransform panelRect; // full-screen black panel
    public float spotlightRadius = 0.2f; // normalized UV (0-1)
    public float edgeSoftness = 0.05f;   // normalized UV (0-1)
    //public Canvas canvas; // assign if using Screen Space - Camera

    void Update()
    {
        Vector2 viewportPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        spotlightMaterial.SetVector("_CursorPosition", new Vector4(viewportPos.x, viewportPos.y, 0, 0));
        spotlightMaterial.SetFloat("_SpotlightRadius", spotlightRadius);
        spotlightMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
    }

    //void Update()
    //{
    //    Vector2 localPoint;
    //    RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //        panelRect,
    //        Input.mousePosition,
    //        canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null,
    //        out localPoint
    //    );

    //    // Convert local point to UV coordinates 0-1
    //    Vector2 uv = new Vector2(
    //        (localPoint.x / panelRect.rect.width) + 0.5f,
    //        (localPoint.y / panelRect.rect.height) + 0.5f
    //    );

    //    // DO NOT flip Y — this caused the mirrored effect

    //    spotlightMaterial.SetVector("_CursorPosition", new Vector4(uv.x, uv.y, 0, 0));
    //    spotlightMaterial.SetFloat("_SpotlightRadius", spotlightRadius);
    //    spotlightMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
    //}
}