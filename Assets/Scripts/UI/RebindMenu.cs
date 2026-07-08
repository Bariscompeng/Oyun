using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrafikParkuru.UI
{
    public class RebindMenu : MonoBehaviour
    {
        [Header("Girdi Varlığı")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Buton Metinleri")]
        [SerializeField] private TMP_Text throttleText;
        [SerializeField] private TMP_Text brakeText;
        [SerializeField] private TMP_Text steerLeftText;
        [SerializeField] private TMP_Text steerRightText;
        [SerializeField] private TMP_Text signalLeftText;
        [SerializeField] private TMP_Text signalRightText;

        [Header("Durum Paneli")]
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private TMP_Text waitingText;

        [Header("Rebind Butonları")]
        [SerializeField] private UnityEngine.UI.Button throttleButton;
        [SerializeField] private UnityEngine.UI.Button brakeButton;
        [SerializeField] private UnityEngine.UI.Button steerLeftButton;
        [SerializeField] private UnityEngine.UI.Button steerRightButton;
        [SerializeField] private UnityEngine.UI.Button signalLeftButton;
        [SerializeField] private UnityEngine.UI.Button signalRightButton;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button resetButton;

        private const string RebindPrefsKey = "InputBindingOverrides";
        private InputActionMap drivingMap;

        private void Awake()
        {
            if (inputActions != null)
            {
                drivingMap = inputActions.FindActionMap("Driving", true);
            }
            
            LoadBindings();
        }

        private void Start()
        {
            UpdateAllButtonLabels();
            if (waitingPanel != null) waitingPanel.SetActive(false);

            if (throttleButton != null) throttleButton.onClick.AddListener(StartRebindThrottle);
            if (brakeButton != null) brakeButton.onClick.AddListener(StartRebindBrake);
            if (steerLeftButton != null) steerLeftButton.onClick.AddListener(StartRebindSteerLeft);
            if (steerRightButton != null) steerRightButton.onClick.AddListener(StartRebindSteerRight);
            if (signalLeftButton != null) signalLeftButton.onClick.AddListener(StartRebindSignalLeft);
            if (signalRightButton != null) signalRightButton.onClick.AddListener(StartRebindSignalRight);
            if (closeButton != null) closeButton.onClick.AddListener(() => {
                var pm = FindAnyObjectByType<PauseMenu>();
                if (pm != null) pm.ToggleRebindMenu(false);
            });
            if (resetButton != null) resetButton.onClick.AddListener(ResetBindings);
        }

        public void StartRebindThrottle()
        {
            StartRebinding("Throttle", 0, throttleText);
        }

        public void StartRebindBrake()
        {
            StartRebinding("Brake", 0, brakeText);
        }

        public void StartRebindSteerLeft()
        {
            // Steer genellikle bir Composite bind'dır. (örn. 1D Axis)
            // Index 1 genellikle sol (Negative) kısmıdır.
            StartRebinding("Steer", 1, steerLeftText);
        }

        public void StartRebindSteerRight()
        {
            // Index 2 genellikle sağ (Positive) kısmıdır.
            StartRebinding("Steer", 2, steerRightText);
        }

        public void StartRebindSignalLeft()
        {
            StartRebinding("SignalLeft", 0, signalLeftText);
        }

        public void StartRebindSignalRight()
        {
            StartRebinding("SignalRight", 0, signalRightText);
        }

        private void StartRebinding(string actionName, int bindingIndex, TMP_Text textComponent)
        {
            if (drivingMap == null) return;

            InputAction action = drivingMap.FindAction(actionName);
            if (action == null) return;

            // Mevcut girdileri devre dışı bırak
            drivingMap.Disable();

            if (waitingPanel != null)
            {
                waitingPanel.SetActive(true);
                waitingText.text = "Bir tuşa basın...";
            }

            textComponent.text = "...";

            var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation =>
                {
                    operation.Dispose();
                    drivingMap.Enable();
                    if (waitingPanel != null) waitingPanel.SetActive(false);
                    
                    UpdateAllButtonLabels();
                    SaveBindings();
                })
                .OnCancel(operation =>
                {
                    operation.Dispose();
                    drivingMap.Enable();
                    if (waitingPanel != null) waitingPanel.SetActive(false);
                    
                    UpdateAllButtonLabels();
                });

            rebindOperation.Start();
        }

        private void UpdateAllButtonLabels()
        {
            if (drivingMap == null) return;

            UpdateLabel("Throttle", 0, throttleText);
            UpdateLabel("Brake", 0, brakeText);
            UpdateLabel("Steer", 1, steerLeftText);
            UpdateLabel("Steer", 2, steerRightText);
            UpdateLabel("SignalLeft", 0, signalLeftText);
            UpdateLabel("SignalRight", 0, signalRightText);
        }

        private void UpdateLabel(string actionName, int bindingIndex, TMP_Text textComponent)
        {
            if (textComponent == null) return;

            InputAction action = drivingMap.FindAction(actionName);
            if (action == null) return;

            if (action.bindings.Count > bindingIndex)
            {
                textComponent.text = action.GetBindingDisplayString(bindingIndex);
            }
        }

        public void ResetBindings()
        {
            if (inputActions == null) return;

            foreach (var map in inputActions.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }

            PlayerPrefs.DeleteKey(RebindPrefsKey);
            PlayerPrefs.Save();
            UpdateAllButtonLabels();
        }

        private void SaveBindings()
        {
            if (inputActions == null) return;

            string overrides = inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(RebindPrefsKey, overrides);
            PlayerPrefs.Save();
        }

        private void LoadBindings()
        {
            if (inputActions == null) return;

            if (PlayerPrefs.HasKey(RebindPrefsKey))
            {
                string overrides = PlayerPrefs.GetString(RebindPrefsKey);
                inputActions.LoadBindingOverridesFromJson(overrides);
            }
        }
    }
}
