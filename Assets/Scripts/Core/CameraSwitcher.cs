using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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

    // FPS modunda gizlenecek dış gövde parçaları (kamera kliplemesini önlemek için)
    private Renderer windshieldRenderer;
    private Renderer roofRenderer;
    private Renderer pillarsRenderer;
    private Renderer windshieldGasketRenderer;
    private Renderer windshieldWipersRenderer;
    private Renderer windshieldWipersBaseRenderer;
    private Renderer bodyPanelsColor2Renderer;

    // Tekerlek görselleri — FPS modunda iç mekana klipleniyor
    private GameObject[] wheelVisuals;

    // Cockpit Dashboard mesh — RealCarVisual'deki asıl gösterge paneli ile çakışıyor
    private Renderer cockpitDashboardRenderer;

    private void Awake()
    {
        if (fpsCamera == null)
        {
            GameObject go = GameObject.Find("FpsCamera");
            if (go != null) fpsCamera = go.GetComponent<CinemachineCamera>();
        }
        if (chaseCamera == null)
        {
            GameObject go = GameObject.Find("ChaseCamera");
            if (go != null) chaseCamera = go.GetComponent<CinemachineCamera>();
        }
        if (cockpitParent == null)
        {
            GameObject car = GameObject.Find("Car");
            if (car != null)
            {
                Transform cockpit = car.transform.Find("Cockpit");
                if (cockpit != null) cockpitParent = cockpit.gameObject;
            }
        }

        if (inputActions != null)
            toggleAction = inputActions.FindActionMap("Driving", true).FindAction("ToggleCamera", true);

        // Find renderers on RealCarVisual parts that clip in FPS mode
        GameObject carObj = GameObject.Find("Car");
        if (carObj != null)
        {
            Transform rcv = carObj.transform.Find("RealCarVisual");
            if (rcv != null)
            {
                windshieldRenderer = FindRenderer(rcv, "BodyWindshield");
                roofRenderer = FindRenderer(rcv, "BodyRoofPanel");
                pillarsRenderer = FindRenderer(rcv, "BodyPillars");
                windshieldGasketRenderer = FindRenderer(rcv, "BodyWindshieldGasket");
                windshieldWipersRenderer = FindRenderer(rcv, "BodyWindshieldWipers");
                windshieldWipersBaseRenderer = FindRenderer(rcv, "BodyWindshieldWipersBase");
                bodyPanelsColor2Renderer = FindRenderer(rcv, "BodyPanelsColor2");

                // Tekerlek görsellerini bul
                string[] wheelNames = { "WheelFrontL", "WheelFrontR", "WheelRearL", "WheelRearR" };
                wheelVisuals = new GameObject[wheelNames.Length];
                for (int i = 0; i < wheelNames.Length; i++)
                {
                    Transform w = rcv.Find(wheelNames[i]);
                    if (w != null) wheelVisuals[i] = w.gameObject;
                }
            }

            // Cockpit altındaki eski Dashboard mesh'i — gerçek model dashboard ile çakışıyor
            Transform cockpit = carObj.transform.Find("Cockpit");
            if (cockpit != null)
            {
                Transform dashboard = cockpit.Find("Dashboard");
                if (dashboard != null)
                    cockpitDashboardRenderer = dashboard.GetComponent<Renderer>();
            }
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
        if (fpsCamera != null) fpsCamera.Priority = isFps ? 20 : 10;
        if (chaseCamera != null) chaseCamera.Priority = isFps ? 10 : 20;
        if (cockpitParent != null) cockpitParent.SetActive(isFps);

        // FPS modunda kameranın kliplemesini önlemek için dış gövde parçalarını gizle
        bool showExterior = !isFps;
        SetRendererEnabled(windshieldRenderer, showExterior);
        SetRendererEnabled(roofRenderer, showExterior);
        SetRendererEnabled(pillarsRenderer, showExterior);
        SetRendererEnabled(windshieldGasketRenderer, showExterior);
        SetRendererEnabled(windshieldWipersRenderer, showExterior);
        SetRendererEnabled(windshieldWipersBaseRenderer, showExterior);
        SetRendererEnabled(bodyPanelsColor2Renderer, showExterior);

        // Cockpit Dashboard mesh'i FPS modunda gizle (RealCarVisual'deki asıl dashboard ile çakışma)
        SetRendererEnabled(cockpitDashboardRenderer, false);

        // Tekerlek görsellerini FPS modunda gizle (iç mekana klipleniyor)
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

        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.SetFpsView(isFps);
    }

    private static void SetRendererEnabled(Renderer r, bool enabled)
    {
        if (r != null) r.enabled = enabled;
    }
}
}
