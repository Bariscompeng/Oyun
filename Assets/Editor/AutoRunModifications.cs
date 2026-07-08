using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TrafikParkuru.Core;
using TrafikParkuru.Vehicle;
using TrafikParkuru.Stations;

namespace TrafikParkuru.Editor
{
    [InitializeOnLoad]
    public static class AutoRunModifications
    {
        static AutoRunModifications()
        {
            // Bu statik constructor derleme bitince otomatik çağrılır
            // Her derlemede çalışmasını garanti etmek için delayCall ile doğrudan çağırıyoruz
            EditorApplication.delayCall += RunModifications;
        }

        [MenuItem("Tools/Apply Scene Modifications")]
        public static void ForceRunModifications()
        {
            RunModifications();
        }

        private static Material CreateMaterial(Color color, float smoothness, float metallic)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            return mat;
        }

        private static void RunModifications()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log($"AutoRunModifications: Modifying scene: {activeScene.name}");

            // 1. Sarı kutuların kaldırılması (Marker1 - Marker5)
            for (int i = 1; i <= 5; i++)
            {
                GameObject marker = GameObject.Find($"Marker{i}");
                if (marker != null)
                {
                    Debug.Log($"AutoRunModifications: Deactivating {marker.name}");
                    marker.SetActive(false);
                    EditorUtility.SetDirty(marker);
                }
            }

            // 2. Yaya geçidi nesnelerinin taşınması (Z = -60) ve değişkenlerin güncellenmesi
            GameObject zebra = GameObject.Find("ZebraCrossing");
            if (zebra != null)
            {
                zebra.transform.position = new Vector3(0f, 0.06f, -60f);
                EditorUtility.SetDirty(zebra);
                Debug.Log("AutoRunModifications: Moved ZebraCrossing to Z=-60");
            }

            GameObject crosswalkStation = GameObject.Find("CrosswalkStation");
            if (crosswalkStation != null)
            {
                crosswalkStation.transform.position = new Vector3(0f, 0.05f, -60f);
                var comp = crosswalkStation.GetComponent<CrosswalkStation>();
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    so.FindProperty("zebraZ").floatValue = -60f;
                    so.FindProperty("entryZ").floatValue = -72f;
                    so.FindProperty("exitZ").floatValue = -52f;
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(crosswalkStation);
                Debug.Log("AutoRunModifications: Moved CrosswalkStation to Z=-60 and updated zebraZ/entryZ/exitZ");
            }

            GameObject spawner = GameObject.Find("PedestrianSpawner");
            if (spawner != null)
            {
                spawner.transform.position = new Vector3(0f, 0f, -60f);
                var comp = spawner.GetComponent<PedestrianSpawner>();
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    so.FindProperty("crosswalkZ").floatValue = -60f;
                    so.FindProperty("triggerZ").floatValue = -90f;
                    
                    var pedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pedestrian.prefab");
                    if (pedPrefab != null)
                    {
                        so.FindProperty("pedestrianPrefab").objectReferenceValue = pedPrefab;
                    }
                    else
                    {
                        Debug.LogWarning("AutoRunModifications: Assets/Prefabs/Pedestrian.prefab not found!");
                    }
                    
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(spawner);
                Debug.Log("AutoRunModifications: Moved PedestrianSpawner to Z=-60, updated triggers, assigned Pedestrian prefab");
            }

            // 2b. Trafik Işığı ve Durma Çizgisi nesnelerinin taşınması (Z = -70, yaya geçidinden 10m önce)
            GameObject trafficLight = GameObject.Find("TrafficLight");
            if (trafficLight != null)
            {
                trafficLight.transform.position = new Vector3(6f, 0f, -70f);
                EditorUtility.SetDirty(trafficLight);
                Debug.Log("AutoRunModifications: Moved TrafficLight to Z=-70");
            }

            GameObject stopLineVisual = GameObject.Find("Custom_StopLineStation1");
            if (stopLineVisual != null)
            {
                stopLineVisual.transform.position = new Vector3(2.4f, 0.06f, -70f);
                EditorUtility.SetDirty(stopLineVisual);
                Debug.Log("AutoRunModifications: Moved Custom_StopLineStation1 to Z=-70");
            }

            GameObject stopLineTrigger = GameObject.Find("StopLineTrigger");
            if (stopLineTrigger != null)
            {
                stopLineTrigger.transform.position = new Vector3(1.5f, 1.5f, -69f);
                
                var col = stopLineTrigger.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = stopLineTrigger.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;
                col.size = new Vector3(8f, 5f, 2f);
                
                EditorUtility.SetDirty(stopLineTrigger);
                Debug.Log("AutoRunModifications: Moved StopLineTrigger to Z=-69, set BoxCollider size");
            }

            // Temizleme: Eski Z = -60'taki Custom_ZebraStripe nesneleri (ZebraCrossing dışında kalanlar)
            var allStripeGos = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allStripeGos)
            {
                if (go.name.StartsWith("Custom_ZebraStripe_") && go.transform.parent == null)
                {
                    Debug.Log($"AutoRunModifications: Destroying duplicate zebra stripe: {go.name}");
                    Undo.DestroyObjectImmediate(go);
                }
            }

            // 3. Şerit ihlali tespit bileşeni ekleme (Car)
            GameObject car = GameObject.Find("Car");
            if (car != null)
            {
                var detector = car.GetComponent<LaneDetector>();
                if (detector == null)
                {
                    detector = car.AddComponent<LaneDetector>();
                    EditorUtility.SetDirty(car);
                    Debug.Log("AutoRunModifications: Added LaneDetector to Car");
                }
            }

            // 4. Hız Sınırı Tabelalarını yan yola taşıma ve tabela metnini 25 yapma
            GameObject signRight = GameObject.Find("SpeedLimitSign_Right");
            if (signRight != null)
            {
                signRight.transform.position = new Vector3(20f, 0f, -5.5f);
                signRight.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                EditorUtility.SetDirty(signRight);
                
                var tmpText = signRight.GetComponentInChildren<TextMeshPro>();
                if (tmpText != null)
                {
                    tmpText.text = "25";
                    EditorUtility.SetDirty(tmpText);
                }
                Debug.Log("AutoRunModifications: Moved SpeedLimitSign_Right to X=20, Z=-5.5, set text to 25");
            }

            GameObject signLeft = GameObject.Find("SpeedLimitSign_Left");
            if (signLeft != null)
            {
                signLeft.transform.position = new Vector3(20f, 0f, 5.5f);
                signLeft.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                EditorUtility.SetDirty(signLeft);

                var tmpText = signLeft.GetComponentInChildren<TextMeshPro>();
                if (tmpText != null)
                {
                    tmpText.text = "25";
                    EditorUtility.SetDirty(tmpText);
                }
                Debug.Log("AutoRunModifications: Moved SpeedLimitSign_Left to X=20, Z=5.5, set text to 25");
            }

            // 5. Hız Sınırı İstasyonunu yan yola taşıma (X = 32)
            GameObject speedStation = GameObject.Find("SpeedZoneStation");
            if (speedStation != null)
            {
                speedStation.transform.position = new Vector3(32f, 0f, 0f);
                speedStation.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                EditorUtility.SetDirty(speedStation);
                Debug.Log("AutoRunModifications: Moved SpeedZoneStation to X=32");
            }

            // 6. Bitiş çizgisini yan yol sonuna taşıma (X = 50)
            GameObject finishTrigger = GameObject.Find("FinishTrigger");
            if (finishTrigger != null)
            {
                finishTrigger.transform.position = new Vector3(50f, 0f, 0f);
                finishTrigger.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                
                var col = finishTrigger.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = finishTrigger.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;
                col.size = new Vector3(10f, 5f, 2f);
                
                EditorUtility.SetDirty(finishTrigger);
                Debug.Log("AutoRunModifications: Moved FinishTrigger to X=50, set BoxCollider size");
            }

            GameObject finishPlane = GameObject.Find("FinishCheckerPlane");
            if (finishPlane != null)
            {
                finishPlane.transform.position = new Vector3(50f, 0.02f, 0f);
                finishPlane.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                EditorUtility.SetDirty(finishPlane);
                Debug.Log("AutoRunModifications: Moved FinishCheckerPlane to X=50");
            }

            // 7. NPC Spawner oluşturma ve bileşeni ekleme/ayarlama
            GameObject spawnerGo = GameObject.Find("NPC_Spawner");
            if (spawnerGo == null)
            {
                spawnerGo = new GameObject("NPC_Spawner");
                spawnerGo.AddComponent<NpcSpawner>();
                Undo.RegisterCreatedObjectUndo(spawnerGo, "Create NPC Spawner");
                Debug.Log("AutoRunModifications: Created NPC_Spawner GameObject");
            }

            var npcSpawnerComp = spawnerGo.GetComponent<NpcSpawner>();
            if (npcSpawnerComp != null)
            {
                SerializedObject so = new SerializedObject(npcSpawnerComp);
                var npcCarPrefabProp = so.FindProperty("npcCarPrefab");
                var npcPedestrianPrefabProp = so.FindProperty("npcPedestrianPrefab");
                
                var carModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/ToyCar.glb");
                var pedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pedestrian.prefab");
                
                if (carModel != null) npcCarPrefabProp.objectReferenceValue = carModel;
                if (pedPrefab != null) npcPedestrianPrefabProp.objectReferenceValue = pedPrefab;
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(spawnerGo);
                Debug.Log("AutoRunModifications: Assigned NPC spawner car model and pedestrian prefab");
            }

            // 8. FpsLookAround Kamera hassasiyetlerini yumuşatma
            GameObject cameraGo = GameObject.Find("FpsCamera");
            if (cameraGo != null)
            {
                var lookComp = cameraGo.GetComponent<FpsLookAround>();
                if (lookComp != null)
                {
                    SerializedObject so = new SerializedObject(lookComp);
                    so.FindProperty("keyboardSpeed").floatValue = 25f;
                    so.FindProperty("mouseSensitivity").floatValue = 0.03f;
                    so.FindProperty("smoothTime").floatValue = 0.25f;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(cameraGo);
                    Debug.Log("AutoRunModifications: Optimized FpsLookAround sensitivity (keyboardSpeed=25, mouseSensitivity=0.03, smoothTime=0.25)");
                }
            }

            // 9. FPS Gerçekçi Kokpit & Gösterge Paneli Kurulumu
            GameObject cockpit = GameObject.Find("Cockpit");
            if (cockpit != null)
            {
                Transform dashboard = cockpit.transform.Find("Dashboard");
                if (dashboard != null)
                {
                    // Gösterge Paneli (Instrument Cluster)
                    Transform cluster = dashboard.Find("InstrumentCluster");
                    if (cluster == null)
                    {
                        GameObject clusterGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        clusterGo.name = "InstrumentCluster";
                        cluster = clusterGo.transform;
                        cluster.SetParent(dashboard, false);
                        cluster.localPosition = new Vector3(-0.4f, 0.13f, -0.21f);
                        cluster.localScale = new Vector3(0.5f, 0.18f, 0.02f);
                        
                        var col = clusterGo.GetComponent<Collider>();
                        if (col != null) Object.DestroyImmediate(col);

                        var rend = clusterGo.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.sharedMaterial = CreateMaterial(new Color(0.08f, 0.08f, 0.09f), 0.1f, 0.0f);
                        }
                    }

                    // Kadranlar
                    Transform leftDial = cluster.Find("LeftDial");
                    if (leftDial == null)
                    {
                        GameObject dialGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        dialGo.name = "LeftDial";
                        leftDial = dialGo.transform;
                        leftDial.SetParent(cluster, false);
                        leftDial.localPosition = new Vector3(-0.15f, 0f, -0.51f);
                        leftDial.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        leftDial.localScale = new Vector3(0.12f, 0.01f, 0.12f);

                        var col = dialGo.GetComponent<Collider>();
                        if (col != null) Object.DestroyImmediate(col);

                        var rend = dialGo.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.sharedMaterial = CreateMaterial(new Color(0.15f, 0.15f, 0.15f), 0.2f, 0.0f);
                        }
                    }

                    Transform rightDial = cluster.Find("RightDial");
                    if (rightDial == null)
                    {
                        GameObject dialGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        dialGo.name = "RightDial";
                        rightDial = dialGo.transform;
                        rightDial.SetParent(cluster, false);
                        rightDial.localPosition = new Vector3(0.15f, 0f, -0.51f);
                        rightDial.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        rightDial.localScale = new Vector3(0.12f, 0.01f, 0.12f);

                        var col = dialGo.GetComponent<Collider>();
                        if (col != null) Object.DestroyImmediate(col);

                        var rend = dialGo.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.sharedMaterial = CreateMaterial(new Color(0.15f, 0.15f, 0.15f), 0.2f, 0.0f);
                        }
                    }

                    // Determine parent for texts (physical dashboard if available, fallback to cluster)
                    Transform realCarVisual = cockpit.transform.parent != null ? cockpit.transform.parent.Find("RealCarVisual") : null;
                    Transform dash = realCarVisual?.Find("InteriorSteeringDash");
                    Transform textParent = (dash != null) ? dash : cluster;

                    // Disable mesh renderers on the old procedural dashboard parts
                    var clusterRend = cluster.GetComponent<Renderer>();
                    if (clusterRend != null) clusterRend.enabled = false;

                    // Hız Metni (TextMeshPro)
                    Transform speedTextTrans = dashboard.Find("InstrumentCluster/SpeedText");
                    if (speedTextTrans == null && dash != null) speedTextTrans = dash.Find("SpeedText");
                    TextMeshPro speedTMP = null;
                    if (speedTextTrans == null)
                    {
                        GameObject txtGo = new GameObject("SpeedText");
                        speedTextTrans = txtGo.transform;
                        speedTextTrans.SetParent(textParent, false);
                    }
                    else
                    {
                        speedTextTrans.SetParent(textParent, false);
                    }

                    if (textParent == dash)
                    {
                        speedTextTrans.localPosition = new Vector3(0.01f, -0.19f, 0.125f);
                        speedTextTrans.localRotation = Quaternion.Euler(270f, 180f, 0f);
                        speedTextTrans.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                    }
                    else
                    {
                        speedTextTrans.localPosition = new Vector3(0f, 0.01f, -0.52f);
                        speedTextTrans.localRotation = Quaternion.Euler(0f, 180f, 0f);
                        speedTextTrans.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    }

                    speedTMP = speedTextTrans.GetComponent<TextMeshPro>();
                    if (speedTMP == null) speedTMP = speedTextTrans.gameObject.AddComponent<TextMeshPro>();
                    speedTMP.alignment = TextAlignmentOptions.Center;
                    speedTMP.fontSize = 5f;
                    speedTMP.color = (textParent == dash) ? new Color(1f, 0.6f, 0f) : Color.white;
                    speedTMP.text = "0";

                    // KM/H Yazısı (Only needed for fallback cluster, disable if on dash)
                    Transform kmhTextTrans = dashboard.Find("InstrumentCluster/KmhText");
                    if (kmhTextTrans == null && dash != null) kmhTextTrans = dash.Find("KmhText");
                    if (kmhTextTrans == null)
                    {
                        GameObject txtGo = new GameObject("KmhText");
                        kmhTextTrans = txtGo.transform;
                        kmhTextTrans.SetParent(textParent, false);
                        kmhTextTrans.localPosition = new Vector3(0f, -0.04f, -0.52f);
                        kmhTextTrans.localRotation = Quaternion.Euler(0f, 180f, 0f);
                        kmhTextTrans.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        
                        var tmp = txtGo.AddComponent<TextMeshPro>();
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.fontSize = 3f;
                        tmp.color = Color.gray;
                        tmp.text = "KM/H";
                    }
                    if (textParent == dash)
                    {
                        kmhTextTrans.gameObject.SetActive(false);
                    }
                    else
                    {
                        kmhTextTrans.gameObject.SetActive(true);
                    }

                    // Sinyaller
                    Transform signalTextTrans = dashboard.Find("InstrumentCluster/SignalText");
                    if (signalTextTrans == null && dash != null) signalTextTrans = dash.Find("SignalText");
                    TextMeshPro signalTMP = null;
                    if (signalTextTrans == null)
                    {
                        GameObject txtGo = new GameObject("SignalText");
                        signalTextTrans = txtGo.transform;
                        signalTextTrans.SetParent(textParent, false);
                    }
                    else
                    {
                        signalTextTrans.SetParent(textParent, false);
                    }

                    if (textParent == dash)
                    {
                        signalTextTrans.localPosition = new Vector3(0.01f, -0.19f, 0.155f);
                        signalTextTrans.localRotation = Quaternion.Euler(270f, 180f, 0f);
                        signalTextTrans.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    }
                    else
                    {
                        signalTextTrans.localPosition = new Vector3(0f, 0.06f, -0.52f);
                        signalTextTrans.localRotation = Quaternion.Euler(0f, 180f, 0f);
                        signalTextTrans.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                    }

                    signalTMP = signalTextTrans.GetComponent<TextMeshPro>();
                    if (signalTMP == null) signalTMP = signalTextTrans.gameObject.AddComponent<TextMeshPro>();
                    signalTMP.alignment = TextAlignmentOptions.Center;
                    signalTMP.fontSize = 4f;
                    signalTMP.color = (textParent == dash) ? new Color(1f, 0.6f, 0f) : Color.gray;
                    signalTMP.text = "<  >";

                    // Vites Göstergesi
                    Transform gearTextTrans = dashboard.Find("InstrumentCluster/GearText");
                    if (gearTextTrans == null && dash != null) gearTextTrans = dash.Find("GearText");
                    TextMeshPro gearTMP = null;
                    if (gearTextTrans == null)
                    {
                        GameObject txtGo = new GameObject("GearText");
                        gearTextTrans = txtGo.transform;
                        gearTextTrans.SetParent(textParent, false);
                    }
                    else
                    {
                        gearTextTrans.SetParent(textParent, false);
                    }

                    if (textParent == dash)
                    {
                        gearTextTrans.localPosition = new Vector3(-0.06f, -0.19f, 0.12f);
                        gearTextTrans.localRotation = Quaternion.Euler(270f, 180f, 0f);
                        gearTextTrans.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    }
                    else
                    {
                        gearTextTrans.localPosition = new Vector3(-0.15f, -0.02f, -0.52f);
                        gearTextTrans.localRotation = Quaternion.Euler(0f, 180f, 0f);
                        gearTextTrans.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                    }

                    gearTMP = gearTextTrans.GetComponent<TextMeshPro>();
                    if (gearTMP == null) gearTMP = gearTextTrans.gameObject.AddComponent<TextMeshPro>();
                    gearTMP.alignment = TextAlignmentOptions.Center;
                    gearTMP.fontSize = 4f;
                    gearTMP.color = (textParent == dash) ? new Color(1f, 0.6f, 0f) : new Color(1f, 0.5f, 0f);
                    gearTMP.text = "P";

                    // Bilgi Ekranı (Orta Konsol)
                    Transform console = dashboard.Find("CenterConsole");
                    if (console == null)
                    {
                        GameObject consoleGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        consoleGo.name = "CenterConsole";
                        console = consoleGo.transform;
                        console.SetParent(dashboard, false);
                        console.localPosition = new Vector3(0.1f, -0.1f, -0.21f);
                        console.localScale = new Vector3(0.35f, 0.3f, 0.02f);

                        var col = consoleGo.GetComponent<Collider>();
                        if (col != null) Object.DestroyImmediate(col);

                        var rend = consoleGo.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.sharedMaterial = CreateMaterial(new Color(0.1f, 0.1f, 0.1f), 0.15f, 0.05f);
                        }

                        // Ekran Alt Nesnesi
                        GameObject screenGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        screenGo.name = "Screen";
                        Transform screen = screenGo.transform;
                        screen.SetParent(console, false);
                        screen.localPosition = new Vector3(0f, 0.05f, -0.51f);
                        screen.localScale = new Vector3(0.9f, 0.55f, 0.1f);

                        var sCol = screenGo.GetComponent<Collider>();
                        if (sCol != null) Object.DestroyImmediate(sCol);

                        var sRend = screenGo.GetComponent<Renderer>();
                        if (sRend != null)
                        {
                            sRend.sharedMaterial = CreateMaterial(new Color(0.05f, 0.1f, 0.15f), 0.8f, 0.0f); // Glowing blueish
                        }

                        // Ekran Yazısı
                        GameObject screenTxt = new GameObject("ScreenText");
                        screenTxt.transform.SetParent(screen, false);
                        screenTxt.transform.localPosition = new Vector3(0f, 0f, -0.52f);
                        screenTxt.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                        screenTxt.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        var sTmp = screenTxt.AddComponent<TextMeshPro>();
                        sTmp.alignment = TextAlignmentOptions.Center;
                        sTmp.fontSize = 2.5f;
                        sTmp.color = Color.cyan;
                        sTmp.text = "GPS ACTIVE\nTRAFFIC OK";
                    }

                    var consoleRend = console.GetComponent<Renderer>();
                    if (consoleRend != null) consoleRend.enabled = false;

                    // Ensure dashboard itself is active so DashboardDisplay runs
                    dashboard.gameObject.SetActive(true);

                    // DashboardDisplay scriptini ekle/güncelle
                    var display = dashboard.GetComponent<DashboardDisplay>();
                    if (display == null)
                    {
                        display = dashboard.gameObject.AddComponent<DashboardDisplay>();
                    }

                    SerializedObject displaySO = new SerializedObject(display);
                    displaySO.FindProperty("speedText").objectReferenceValue = speedTMP;
                    displaySO.FindProperty("signalText").objectReferenceValue = signalTMP;
                    displaySO.FindProperty("gearText").objectReferenceValue = gearTMP;
                    displaySO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(dashboard.gameObject);
                    Debug.Log("AutoRunModifications: physical cockpit / dashboard texts built and wired.");

                    BuildCabinInterior(cockpit.transform);

                    // Deactivate CenterConsole and InstrumentCluster GameObjects
                    console = dashboard.Find("CenterConsole");
                    if (console != null)
                    {
                        console.gameObject.SetActive(false);
                        EditorUtility.SetDirty(console.gameObject);
                    }
                    cluster = dashboard.Find("InstrumentCluster");
                    if (cluster != null)
                    {
                        cluster.gameObject.SetActive(false);
                        EditorUtility.SetDirty(cluster.gameObject);
                    }
                }
            }

            // Shift RealCarVisual and Cockpit down by 0.53f to eliminate hovering and align wheels
            GameObject playerCar = GameObject.Find("Car");
            if (playerCar != null)
            {
                Transform rcv = playerCar.transform.Find("RealCarVisual");
                if (rcv != null)
                {
                    rcv.localPosition = new Vector3(0.0f, -0.53f, 0.0f);
                    EditorUtility.SetDirty(rcv);
                }
                Transform cp = playerCar.transform.Find("Cockpit");
                if (cp != null)
                {
                    cp.localPosition = new Vector3(0.0f, -0.53f, 0.0f);
                    EditorUtility.SetDirty(cp);
                }

                // Add BoxCollider for car body stability
                var box = playerCar.GetComponent<BoxCollider>();
                if (box == null) box = playerCar.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.5f, 0f);
                box.size = new Vector3(1.8f, 0.8f, 4.2f);
                EditorUtility.SetDirty(playerCar);

                // Disable CapsuleColliders on WheelVisuals children to let WheelColliders work properly
                Transform wv = playerCar.transform.Find("WheelVisuals");
                if (wv != null)
                {
                    for (int i = 0; i < wv.childCount; i++)
                    {
                        var cc = wv.GetChild(i).GetComponent<CapsuleCollider>();
                        if (cc != null)
                        {
                            cc.enabled = false;
                            EditorUtility.SetDirty(wv.GetChild(i).gameObject);
                        }
                    }
                }

                // Adjust FpsCamera height to match shifted cockpit
                Transform fpsCam = playerCar.transform.Find("FpsCamera");
                if (fpsCam != null)
                {
                    fpsCam.localPosition = new Vector3(0.00f, 0.954f, 0.35f);
                    EditorUtility.SetDirty(fpsCam);
                }

                // Recursively set layer to "Player" (Layer 8)
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer != -1)
                {
                    SetLayerRecursive(playerCar, playerLayer);
                    Debug.Log("AutoRunModifications: Set layer of Car and all descendants to Player.");
                }
            }

            // Adjust CenterMirrorFrame and CenterMirrorCam
            GameObject mirrorFrame = GameObject.Find("Car/Cockpit/CenterMirrorFrame");
            if (mirrorFrame != null)
            {
                mirrorFrame.transform.localPosition = new Vector3(0.00f, 1.45f, 0.85f);
                mirrorFrame.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
                mirrorFrame.transform.localScale = new Vector3(0.24f, 0.06f, 0.01f);
                EditorUtility.SetDirty(mirrorFrame);
            }
            GameObject mirrorCam = GameObject.Find("Car/Cockpit/CenterMirrorCam");
            if (mirrorCam != null)
            {
                mirrorCam.transform.localPosition = new Vector3(0.00f, 1.45f, 0.85f);
                mirrorCam.transform.localRotation = Quaternion.Euler(5f, 180f, 0f);
                
                // Add UniversalAdditionalCameraData for URP rendering
                var uacd = mirrorCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (uacd == null) uacd = mirrorCam.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                uacd.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
                
                EditorUtility.SetDirty(mirrorCam);
            }

            // Sahneyi kaydet
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("AutoRunModifications: Scene saved successfully!");
        }

        private static void BuildCabinInterior(Transform cockpit)
        {
            // Eski CabinShell varsa sil
            Transform oldShell = cockpit.Find("CabinShell");
            if (oldShell != null)
            {
                Object.DestroyImmediate(oldShell.gameObject);
            }
            return;

            GameObject shellGo = new GameObject("CabinShell");
            shellGo.transform.SetParent(cockpit, false);
            Transform shell = shellGo.transform;

            // Malzemeler
            Material darkTrimMat = CreateMaterial(new Color(0.12f, 0.12f, 0.13f), 0.1f, 0f);
            Material roofMat = CreateMaterial(new Color(0.15f, 0.15f, 0.16f), 0.05f, 0f);
            Material seatFabricMat = CreateMaterial(new Color(0.18f, 0.18f, 0.2f), 0.1f, 0f);
            Material chromeMat = CreateMaterial(new Color(0.85f, 0.85f, 0.88f), 0.9f, 0.9f);
            Material leatherBootMat = CreateMaterial(new Color(0.08f, 0.08f, 0.09f), 0.05f, 0f);
            Material metalRodMat = CreateMaterial(new Color(0.8f, 0.8f, 0.8f), 0.85f, 0.8f);

            // A-Sütunları (A-Pillars)
            CreatePrimitivePart(shell, PrimitiveType.Cube, "LeftAPillar", new Vector3(-0.9f, 1.6f, 0.4f), Quaternion.Euler(30f, 0f, 10f), new Vector3(0.08f, 0.9f, 0.08f), darkTrimMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "RightAPillar", new Vector3(0.9f, 1.6f, 0.4f), Quaternion.Euler(30f, 0f, -10f), new Vector3(0.08f, 0.9f, 0.08f), darkTrimMat);

            // Tavan ve Üst Çerçeve
            CreatePrimitivePart(shell, PrimitiveType.Cube, "HeaderBar", new Vector3(0f, 1.95f, 0.15f), Quaternion.identity, new Vector3(1.8f, 0.08f, 0.15f), darkTrimMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "RoofPanel", new Vector3(0f, 2.0f, -0.6f), Quaternion.identity, new Vector3(1.8f, 0.04f, 1.5f), roofMat);

            // Sürücü Koltuğu (Driver Seat)
            CreatePrimitivePart(shell, PrimitiveType.Cube, "DriverSeatBase", new Vector3(-0.4f, 0.3f, -0.6f), Quaternion.identity, new Vector3(0.55f, 0.25f, 0.55f), seatFabricMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "DriverSeatBackrest", new Vector3(-0.4f, 0.75f, -0.85f), Quaternion.Euler(12f, 0f, 0f), new Vector3(0.55f, 0.75f, 0.15f), seatFabricMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "DriverSeatHeadrest", new Vector3(-0.4f, 1.2f, -0.92f), Quaternion.identity, new Vector3(0.26f, 0.2f, 0.12f), seatFabricMat);

            // Yolcu Koltuğu (Passenger Seat)
            CreatePrimitivePart(shell, PrimitiveType.Cube, "PassengerSeatBase", new Vector3(0.4f, 0.3f, -0.6f), Quaternion.identity, new Vector3(0.55f, 0.25f, 0.55f), seatFabricMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "PassengerSeatBackrest", new Vector3(0.4f, 0.75f, -0.85f), Quaternion.Euler(12f, 0f, 0f), new Vector3(0.55f, 0.75f, 0.15f), seatFabricMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "PassengerSeatHeadrest", new Vector3(0.4f, 1.2f, -0.92f), Quaternion.identity, new Vector3(0.26f, 0.2f, 0.12f), seatFabricMat);

            // Kapı İç Döşemeleri (Left & Right Door Panels)
            CreatePrimitivePart(shell, PrimitiveType.Cube, "LeftDoorPanel", new Vector3(-0.95f, 0.8f, -0.4f), Quaternion.identity, new Vector3(0.05f, 0.8f, 1.5f), darkTrimMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "LeftArmrest", new Vector3(-0.9f, 0.8f, -0.4f), Quaternion.identity, new Vector3(0.08f, 0.08f, 0.7f), seatFabricMat);
            CreatePrimitivePart(shell, PrimitiveType.Cylinder, "LeftDoorHandle", new Vector3(-0.89f, 0.95f, -0.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.015f, 0.08f, 0.015f), chromeMat);

            CreatePrimitivePart(shell, PrimitiveType.Cube, "RightDoorPanel", new Vector3(0.95f, 0.8f, -0.4f), Quaternion.identity, new Vector3(0.05f, 0.8f, 1.5f), darkTrimMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "RightArmrest", new Vector3(0.9f, 0.8f, -0.4f), Quaternion.identity, new Vector3(0.08f, 0.08f, 0.7f), seatFabricMat);
            CreatePrimitivePart(shell, PrimitiveType.Cylinder, "RightDoorHandle", new Vector3(0.89f, 0.95f, -0.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.015f, 0.08f, 0.015f), chromeMat);

            // Vites Konsolu (Center Shifter Console)
            CreatePrimitivePart(shell, PrimitiveType.Cube, "ShifterBase", new Vector3(0f, 0.35f, -0.3f), Quaternion.identity, new Vector3(0.24f, 0.35f, 0.7f), darkTrimMat);
            CreatePrimitivePart(shell, PrimitiveType.Cube, "GearBoot", new Vector3(0f, 0.54f, -0.25f), Quaternion.identity, new Vector3(0.12f, 0.04f, 0.12f), leatherBootMat);
            CreatePrimitivePart(shell, PrimitiveType.Cylinder, "GearLever", new Vector3(0f, 0.65f, -0.25f), Quaternion.Euler(10f, 0f, 0f), new Vector3(0.012f, 0.1f, 0.012f), metalRodMat);
            CreatePrimitivePart(shell, PrimitiveType.Sphere, "GearKnob", new Vector3(0.01f, 0.75f, -0.23f), Quaternion.identity, new Vector3(0.045f, 0.045f, 0.045f), leatherBootMat);

            // Arka Duvar
            CreatePrimitivePart(shell, PrimitiveType.Cube, "BackWall", new Vector3(0f, 0.9f, -1.1f), Quaternion.identity, new Vector3(1.8f, 1.2f, 0.05f), darkTrimMat);
        }

        private static void CreatePrimitivePart(Transform parent, PrimitiveType type, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            
            // Collider'ı yok et (fizik çakışması olmasın)
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = material;
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
