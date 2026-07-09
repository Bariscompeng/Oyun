using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TrafikParkuru.Vehicle;

namespace TrafikParkuru.Core
{
    /// C tusu ile FPS kokpit ve dis takip kamerasi arasinda gecis.
    public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private CinemachineCamera fpsCamera;
    [SerializeField] private CinemachineCamera chaseCamera;
    [SerializeField] private GameObject cockpitParent;
    [SerializeField] private bool startInFps = true;

    private InputAction toggleAction;
    private bool isFps;

    private Renderer windshieldRenderer;
    private Renderer roofRenderer;
    private Renderer pillarsRenderer;
    private Renderer windshieldGasketRenderer;
    private Renderer windshieldWipersRenderer;
    private Renderer windshieldWipersBaseRenderer;
    private Renderer bodyPanelsColor2Renderer;

    // FPS modunda serbest bakış scripti — FPS modunda KAPALI olmalı ki arabanın rotasyonu gereksin
    private FpsLookAround fpsLookAround;

    // Tekerlek görselleri — FPS modunda iç mekana klipleniyor
    private GameObject[] wheelVisuals;

    // Cockpit Dashboard mesh — RealCarVisual'deki asıl gösterge paneli ile çakışıyor
    private Renderer cockpitDashboardRenderer;

    private void Awake()
    {
        GameObject carObj = GameObject.Find("Car");

        // ── FPS Kamera kurulumu ──────────────────────────────────────────────
        if (fpsCamera == null)
        {
            GameObject go = GameObject.Find("FpsCamera");
            if (go != null)
                fpsCamera = go.GetComponent<CinemachineCamera>();
        }

        // FpsCamera, Car'ın child'ı olduğu için kendi world transform'unu doğal olarak kullanır.
        // TrackingTarget ATANMIYOR — Cinemachine TrackingTarget olunca Car root'undan
        // offset hesaplar ve kamerayı yanlış yüksekliğe taşır.

        // FpsCamera'yı sürücü baş pozisyonuna yerleştir (runtime'da):
        // Direksiyon simidi world Y ≈ 0.91, Car root Y = 0.80, sürücü başı ≈ 0.20 üstte.
        // Local Y = (0.91 + 0.20) - 0.80 = 0.31 → direksiyonu ve konsolu tam önde gösterir.
        // Local Z: direksiyon simidi Z=-139.07, Car Z=-140 → local Z = +0.60 (hafif öne)
        if (fpsCamera != null && carObj != null)
        {
            fpsCamera.transform.localPosition = new Vector3(0f, 0.42f, 0.60f);
            fpsCamera.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            // Cinemachine Brain'deki Ana Kameranın near clip'ini küçült —
            // iç mekan detayları görünsün, kırpılma olmasın
            Camera mainCam = Camera.main;
            if (mainCam != null) mainCam.nearClipPlane = 0.05f;
        }

        // ── Chase Kamera ────────────────────────────────────────────────────
        if (chaseCamera == null)
        {
            GameObject go = GameObject.Find("ChaseCamera");
            if (go != null) chaseCamera = go.GetComponent<CinemachineCamera>();
        }

        // ── Cockpit parent ──────────────────────────────────────────────────
        if (cockpitParent == null && carObj != null)
        {
            Transform cockpit = carObj.transform.Find("Cockpit");
            if (cockpit != null) cockpitParent = cockpit.gameObject;
        }

        if (inputActions != null)
            toggleAction = inputActions.FindActionMap("Driving", true).FindAction("ToggleCamera", true);

        // ── Dış gövde renderer'ları ve diğer referanslar ────────────────────
        if (carObj != null)
        {
            Transform rcv = carObj.transform.Find("RealCarVisual");
            if (rcv != null)
            {
                windshieldRenderer         = FindRenderer(rcv, "BodyWindshield");
                roofRenderer               = FindRenderer(rcv, "BodyRoofPanel");
                pillarsRenderer            = FindRenderer(rcv, "BodyPillars");
                windshieldGasketRenderer   = FindRenderer(rcv, "BodyWindshieldGasket");
                windshieldWipersRenderer   = FindRenderer(rcv, "BodyWindshieldWipers");
                windshieldWipersBaseRenderer = FindRenderer(rcv, "BodyWindshieldWipersBase");
                bodyPanelsColor2Renderer   = FindRenderer(rcv, "BodyPanelsColor2");

                string[] wheelNames = { "WheelFrontL", "WheelFrontR", "WheelRearL", "WheelRearR" };
                wheelVisuals = new GameObject[wheelNames.Length];
                for (int i = 0; i < wheelNames.Length; i++)
                {
                    Transform w = rcv.Find(wheelNames[i]);
                    if (w != null) wheelVisuals[i] = w.gameObject;
                }
            }

            Transform cockpitT = carObj.transform.Find("Cockpit");
            if (cockpitT != null)
            {
                Transform dashboard = cockpitT.Find("Dashboard");
                if (dashboard != null)
                    cockpitDashboardRenderer = dashboard.GetComponent<Renderer>();
            }

            fpsLookAround = carObj.GetComponentInChildren<FpsLookAround>(true);
        }

        isFps = startInFps;
        ApplyPriorities();
    }

    private static Renderer FindRenderer(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<Renderer>() : null;
    }

    private void OnEnable()
    {
        if (toggleAction != null)
        {
            toggleAction.Enable();
            toggleAction.performed += OnToggle;
        }
    }

    private void OnDisable()
    {
        if (toggleAction != null)
            toggleAction.performed -= OnToggle;
    }

    private void OnToggle(InputAction.CallbackContext _)
    {
        isFps = !isFps;
        ApplyPriorities();
    }

    private void ApplyPriorities()
    {
        // FPS kamerası Follow target'ı = Car (Awake'de set edildi)
        // En yüksek önceliğe sahip kamera Cinemachine Brain tarafından kullanılır
        if (fpsCamera   != null) fpsCamera.Priority   = isFps ? 20 : 5;
        if (chaseCamera != null) chaseCamera.Priority = isFps ? 5  : 20;

        // Chase kamerasini sadece aktif olduğunda render etsin
        if (chaseCamera != null) chaseCamera.gameObject.SetActive(!isFps);

        if (cockpitParent != null) cockpitParent.SetActive(isFps);

        // ── Dış gövde görünürlüğü ────────────────────────────────────────────
        bool showExterior = !isFps;
        SetRendererEnabled(windshieldRenderer, showExterior);
        SetRendererEnabled(roofRenderer, showExterior);
        SetRendererEnabled(pillarsRenderer, showExterior);
        SetRendererEnabled(windshieldGasketRenderer, showExterior);
        SetRendererEnabled(windshieldWipersRenderer, showExterior);
        SetRendererEnabled(windshieldWipersBaseRenderer, showExterior);
        SetRendererEnabled(bodyPanelsColor2Renderer, showExterior);
        SetRendererEnabled(cockpitDashboardRenderer, false);

        if (wheelVisuals != null)
        {
            foreach (var wheel in wheelVisuals)
            {
                if (wheel != null)
                {
                    Renderer[] renderers = wheel.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                        r.enabled = showExterior;
                }
            }
        }

        // FpsLookAround FPS modunda kapalı — araç dönerken kamera arabayı takip etsin
        if (fpsLookAround != null)
            fpsLookAround.enabled = false; // Her iki modda da kapalı tutuyoruz

        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.SetFpsView(isFps);
    }

    private static void SetRendererEnabled(Renderer r, bool enabled)
    {
        if (r != null) r.enabled = enabled;
    }
}
}
