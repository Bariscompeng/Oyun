using UnityEngine;

public class CockpitController : MonoBehaviour
{
    [Header("Ayna Kameraları")]
    public Camera leftMirrorCam;
    public Camera centerMirrorCam;
    public Camera rightMirrorCam;

    [Header("Ayna Mesh Yüzeyleri")]
    public Renderer leftMirrorRenderer;
    public Renderer centerMirrorRenderer;
    public Renderer rightMirrorRenderer;

    private RenderTexture leftRT;
    private RenderTexture centerRT;
    private RenderTexture rightRT;

    private Material leftMat;
    private Material centerMat;
    private Material rightMat;

    private void Start()
    {
        // 1. Render Texture'ları oluştur (Performans için dengeli çözünürlükler)
        leftRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        leftRT.name = "LeftMirror_RT";
        leftRT.filterMode = FilterMode.Bilinear;
        leftRT.wrapMode = TextureWrapMode.Clamp;

        rightRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        rightRT.name = "RightMirror_RT";
        rightRT.filterMode = FilterMode.Bilinear;
        rightRT.wrapMode = TextureWrapMode.Clamp;

        centerRT = new RenderTexture(512, 128, 16, RenderTextureFormat.ARGB32);
        centerRT.name = "CenterMirror_RT";
        centerRT.filterMode = FilterMode.Bilinear;
        centerRT.wrapMode = TextureWrapMode.Clamp;

        // 2. Kameralara Render Texture'ları ata
        if (leftMirrorCam != null) leftMirrorCam.targetTexture = leftRT;
        if (rightMirrorCam != null) rightMirrorCam.targetTexture = rightRT;
        if (centerMirrorCam != null) centerMirrorCam.targetTexture = centerRT;

        // 3. Materyalleri oluştur ve atamaları yap
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");

        if (leftMirrorRenderer != null && unlitShader != null)
        {
            leftMat = new Material(unlitShader);
            leftMat.name = "LeftMirror_Mat";
            leftMat.mainTexture = leftRT;
            leftMirrorRenderer.sharedMaterial = leftMat;
        }

        if (rightMirrorRenderer != null && unlitShader != null)
        {
            rightMat = new Material(unlitShader);
            rightMat.name = "RightMirror_Mat";
            rightMat.mainTexture = rightRT;
            rightMirrorRenderer.sharedMaterial = rightMat;
        }

        if (centerMirrorRenderer != null && unlitShader != null)
        {
            centerMat = new Material(unlitShader);
            centerMat.name = "CenterMirror_Mat";
            centerMat.mainTexture = centerRT;
            centerMirrorRenderer.sharedMaterial = centerMat;
        }

        // Exclude Player layer from the mirror cameras' culling masks
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            int playerMask = 1 << playerLayer;
            if (leftMirrorCam != null) leftMirrorCam.cullingMask &= ~playerMask;
            if (rightMirrorCam != null) rightMirrorCam.cullingMask &= ~playerMask;
            if (centerMirrorCam != null) centerMirrorCam.cullingMask &= ~playerMask;
        }
    }

    private void OnDestroy()
    {
        // Bellek sızıntılarını önlemek için temizlik yap
        if (leftMirrorCam != null) leftMirrorCam.targetTexture = null;
        if (rightMirrorCam != null) rightMirrorCam.targetTexture = null;
        if (centerMirrorCam != null) centerMirrorCam.targetTexture = null;

        if (leftRT != null) { leftRT.Release(); Destroy(leftRT); }
        if (rightRT != null) { rightRT.Release(); Destroy(rightRT); }
        if (centerRT != null) { centerRT.Release(); Destroy(centerRT); }

        if (leftMat != null) Destroy(leftMat);
        if (rightMat != null) Destroy(rightMat);
        if (centerMat != null) Destroy(centerMat);
    }
}
