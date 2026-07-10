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

            // 2. Yaya geçidi nesnelerinin taşınması (Z = -50) ve değişkenlerin güncellenmesi
            GameObject zebra = GameObject.Find("ZebraCrossing");
            if (zebra != null)
            {
                zebra.transform.position = new Vector3(0f, 0.11f, -50f);
                EditorUtility.SetDirty(zebra);
                Debug.Log("AutoRunModifications: Moved ZebraCrossing to Z=-50");
            }

            GameObject crosswalkStation = GameObject.Find("CrosswalkStation");
            if (crosswalkStation != null)
            {
                crosswalkStation.transform.position = new Vector3(0f, 0.05f, -50f);
                var comp = crosswalkStation.GetComponent<CrosswalkStation>();
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    so.FindProperty("zebraZ").floatValue = -50f;
                    so.FindProperty("entryZ").floatValue = -62f;
                    so.FindProperty("exitZ").floatValue = -42f;
                    so.FindProperty("stageToComplete").enumValueIndex = (int)GameStage.Crosswalk;
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(crosswalkStation);
                Debug.Log("AutoRunModifications: Moved CrosswalkStation to Z=-50 and updated zebraZ/entryZ/exitZ");
            }

            GameObject spawner = GameObject.Find("PedestrianSpawner");
            if (spawner != null)
            {
                spawner.transform.position = new Vector3(0f, 0f, -50f);
                var comp = spawner.GetComponent<PedestrianSpawner>();
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    so.FindProperty("crosswalkZ").floatValue = -50f;
                    so.FindProperty("triggerZ").floatValue = -80f;
                    
                    var pedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pedestrian.prefab");
                    if (pedPrefab != null)
                    {
                        so.FindProperty("pedestrianPrefab").objectReferenceValue = pedPrefab;
                    }
                    else
                    {
                        Debug.LogWarning("AutoRunModifications: Assets/Prefabs/Pedestrian.prefab not found!");
                    }
                    
                    var targetStation = crosswalkStation != null ? crosswalkStation.GetComponent<CrosswalkStation>() : null;
                    if (targetStation != null)
                    {
                        so.FindProperty("crosswalkStation").objectReferenceValue = targetStation;
                    }
                    
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(spawner);
                Debug.Log("AutoRunModifications: Moved PedestrianSpawner to Z=-50, updated triggers, assigned Pedestrian prefab");
            }

            // 2c. İkinci Yaya Geçidi Nesnelerinin Oluşturulması/Güncellenmesi (Z = -15)
            GameObject zebra2 = GameObject.Find("ZebraCrossing2");
            if (zebra2 == null)
            {
                if (zebra != null)
                {
                    zebra2 = GameObject.Instantiate(zebra);
                    zebra2.name = "ZebraCrossing2";
                    Undo.RegisterCreatedObjectUndo(zebra2, "Create ZebraCrossing2");
                }
            }
            if (zebra2 != null)
            {
                zebra2.transform.position = new Vector3(25f, 0.11f, 30f);
                zebra2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                for (int i = 0; i < zebra2.transform.childCount; i++)
                {
                    Transform stripe = zebra2.transform.GetChild(i);
                    stripe.localPosition = new Vector3(-3f + i * 1.5f, 0f, 0f);
                    stripe.localRotation = Quaternion.identity;
                }
                EditorUtility.SetDirty(zebra2);
                Debug.Log("AutoRunModifications: Created/Updated ZebraCrossing2 at Z=30, X=25 (rotated 90)");
            }

            GameObject crosswalkStation2 = GameObject.Find("CrosswalkStation2");
            if (crosswalkStation2 == null)
            {
                if (crosswalkStation != null)
                {
                    crosswalkStation2 = GameObject.Instantiate(crosswalkStation);
                    crosswalkStation2.name = "CrosswalkStation2";
                    Undo.RegisterCreatedObjectUndo(crosswalkStation2, "Create CrosswalkStation2");
                }
            }
            if (crosswalkStation2 != null)
            {
                crosswalkStation2.transform.position = new Vector3(25f, 0.05f, 30f);
                crosswalkStation2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                var comp = crosswalkStation2.GetComponent<CrosswalkStation>();
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    so.FindProperty("zebraZ").floatValue = 25f;
                    so.FindProperty("entryZ").floatValue = 37f;
                    so.FindProperty("exitZ").floatValue = 17f;
                    so.FindProperty("stageToComplete").enumValueIndex = (int)GameStage.Crosswalk2;
                    so.FindProperty("useXAxis").boolValue = true;
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(crosswalkStation2);
                Debug.Log("AutoRunModifications: Created/Updated CrosswalkStation2 at Z=30, X=25");
            }

            GameObject spawner2 = GameObject.Find("PedestrianSpawner2");
            if (spawner2 == null)
            {
                if (spawner != null)
                {
                    spawner2 = GameObject.Instantiate(spawner);
                    spawner2.name = "PedestrianSpawner2";
                    Undo.RegisterCreatedObjectUndo(spawner2, "Create PedestrianSpawner2");
                }
            }
            if (spawner2 != null)
            {
                spawner2.transform.position = new Vector3(25f, 0f, 30f);
                var comp = spawner2.GetComponent<PedestrianSpawner>();
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    so.FindProperty("crosswalkZ").floatValue = 25f;
                    so.FindProperty("triggerZ").floatValue = 40f;
                    so.FindProperty("spawnX").floatValue = 25f;
                    so.FindProperty("targetX").floatValue = 35f;
                    so.FindProperty("useXAxis").boolValue = true;
                    
                    var pedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pedestrian.prefab");
                    if (pedPrefab != null)
                    {
                        so.FindProperty("pedestrianPrefab").objectReferenceValue = pedPrefab;
                    }
                    
                    var targetStation = crosswalkStation2 != null ? crosswalkStation2.GetComponent<CrosswalkStation>() : null;
                    if (targetStation != null)
                    {
                        so.FindProperty("crosswalkStation").objectReferenceValue = targetStation;
                    }
                    
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(spawner2);
                Debug.Log("AutoRunModifications: Created/Updated PedestrianSpawner2 at Z=30, X=25");
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
                signRight.transform.position = new Vector3(55.50f, 0f, 10f);
                signRight.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EditorUtility.SetDirty(signRight);
                
                var tmpText = signRight.GetComponentInChildren<TextMeshPro>();
                if (tmpText != null)
                {
                    tmpText.text = "25";
                    EditorUtility.SetDirty(tmpText);
                }
                Debug.Log("AutoRunModifications: Moved SpeedLimitSign_Right to Segment 2 (55.5, 0, 10)");
            }

            GameObject signLeft = GameObject.Find("SpeedLimitSign_Left");
            if (signLeft != null)
            {
                signLeft.transform.position = new Vector3(44.50f, 0f, 10f);
                signLeft.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EditorUtility.SetDirty(signLeft);

                var tmpText = signLeft.GetComponentInChildren<TextMeshPro>();
                if (tmpText != null)
                {
                    tmpText.text = "25";
                    EditorUtility.SetDirty(tmpText);
                }
                Debug.Log("AutoRunModifications: Moved SpeedLimitSign_Left to Segment 2 (44.5, 0, 10)");
            }

            // 5. Hız Sınırı İstasyonunu yan yola taşıma (Segment 2)
            GameObject speedStation = GameObject.Find("SpeedZoneStation");
            if (speedStation != null)
            {
                speedStation.transform.position = new Vector3(50f, 0f, 27.50f);
                speedStation.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                
                var col = speedStation.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.center = new Vector3(0f, 1.50f, 0f);
                    col.size = new Vector3(10f, 4f, 15f);
                }
                
                EditorUtility.SetDirty(speedStation);
                Debug.Log("AutoRunModifications: Moved SpeedZoneStation to Segment 2 (50, 0, 27.5)");
            }

            // 6. Bitiş çizgisini yan yol sonuna taşıma (Segment 3)
            GameObject finishTrigger = GameObject.Find("FinishTrigger");
            if (finishTrigger != null)
            {
                finishTrigger.transform.position = new Vector3(10f, 0f, 30f);
                finishTrigger.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                
                var col = finishTrigger.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = finishTrigger.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;
                col.center = new Vector3(0f, 1.50f, 0f);
                col.size = new Vector3(10f, 5f, 2f);
                
                EditorUtility.SetDirty(finishTrigger);
                Debug.Log("AutoRunModifications: Moved FinishTrigger to Segment 3 (10, 0, 30)");
            }

            GameObject finishPlane = GameObject.Find("FinishCheckerPlane");
            if (finishPlane != null)
            {
                finishPlane.transform.position = new Vector3(10f, 0.02f, 30f);
                finishPlane.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                EditorUtility.SetDirty(finishPlane);
                Debug.Log("AutoRunModifications: Moved FinishCheckerPlane to Segment 3 (10, 0.02, 30)");
            }

            // 6b. Yan Yol ve Kaldırımları Uzatma / Döngü Oluşturma
            Material roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoadMat.mat");
            Material sidewalkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SidewalkMat.mat");
            Material markingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoadMarkingMat.mat");

            // Çakışan binayı deaktive et (Building_R_40 Segment 3'ü kapatıyor)
            GameObject b40 = GameObject.Find("Building_R_40");
            if (b40 != null)
            {
                b40.SetActive(false);
                EditorUtility.SetDirty(b40);
                Debug.Log("AutoRunModifications: Deactivated Building_R_40 to clear side road path");
            }

            // Segment 1 (Güncelleme)
            GameObject sideRoad = GameObject.Find("SideRoad");
            if (sideRoad != null)
            {
                sideRoad.transform.position = new Vector3(25f, 0.05f, 0f);
                sideRoad.transform.localScale = new Vector3(50f, 0.10f, 10f);
                EditorUtility.SetDirty(sideRoad);
            }

            // Segment 2 (Yeni veya Güncelleme)
            GetOrCreatePrimitive("SideRoad_Turn", PrimitiveType.Cube, new Vector3(50f, 0.05f, 15f), new Vector3(10f, 0.10f, 40f), roadMat);

            // Segment 3 (Yeni veya Güncelleme)
            GetOrCreatePrimitive("SideRoad_Merge", PrimitiveType.Cube, new Vector3(25f, 0.05f, 30f), new Vector3(50f, 0.10f, 10f), roadMat);

            // Segment 1 Kaldırımları (Güncelleme)
            GameObject swNorth = GameObject.Find("Sidewalk_Side_North");
            if (swNorth != null)
            {
                swNorth.transform.position = new Vector3(25f, 0.08f, 5.50f);
                swNorth.transform.localScale = new Vector3(40f, 0.15f, 1f);
                EditorUtility.SetDirty(swNorth);
            }
            GameObject swSouth = GameObject.Find("Sidewalk_Side_South");
            if (swSouth != null)
            {
                swSouth.transform.position = new Vector3(30f, 0.08f, -5.50f);
                swSouth.transform.localScale = new Vector3(50f, 0.15f, 1f);
                EditorUtility.SetDirty(swSouth);
            }

            // Segment 2 Kaldırımları
            GetOrCreatePrimitive("Sidewalk_Side2_East", PrimitiveType.Cube, new Vector3(55.50f, 0.08f, 15f), new Vector3(1f, 0.15f, 40f), sidewalkMat);
            GetOrCreatePrimitive("Sidewalk_Side2_West", PrimitiveType.Cube, new Vector3(44.50f, 0.08f, 15f), new Vector3(1f, 0.15f, 20f), sidewalkMat);

            // Segment 3 Kaldırımları
            GetOrCreatePrimitive("Sidewalk_Side3_North", PrimitiveType.Cube, new Vector3(30f, 0.08f, 35.50f), new Vector3(50f, 0.15f, 1f), sidewalkMat);
            GetOrCreatePrimitive("Sidewalk_Side3_South", PrimitiveType.Cube, new Vector3(25f, 0.08f, 24.50f), new Vector3(40f, 0.15f, 1f), sidewalkMat);

            // Ana Yol Kaldırımını Bölme (Sidewalk_Right_North)
            GameObject swRightNorth = GameObject.Find("Sidewalk_Right_North");
            if (swRightNorth != null)
            {
                swRightNorth.name = "Sidewalk_Right_North_Part1";
            }
            GetOrCreatePrimitive("Sidewalk_Right_North_Part1", PrimitiveType.Cube, new Vector3(5.50f, 0.08f, 15.00f), new Vector3(1f, 0.15f, 20f), sidewalkMat);
            GetOrCreatePrimitive("Sidewalk_Right_North_Part2", PrimitiveType.Cube, new Vector3(5.50f, 0.08f, 92.50f), new Vector3(1f, 0.15f, 115f), sidewalkMat);

            // 6c. Şerit İşaretlerini Temizle ve Yeniden Oluştur
            GameObject markingsParent = GameObject.Find("EnvironmentScenery/RoadMarkings");
            Transform markingsTransform = markingsParent != null ? markingsParent.transform : null;

            // Eski yan yol şeritlerini sil
            foreach (var go in allStripeGos)
            {
                if (go != null && (go.name.StartsWith("Custom_SideRoad") || go.name.StartsWith("SideRoadMarking")))
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            // Segment 1 Şeritleri (X = 5f to 50f, Z = 0f)
            for (float x = 7f; x <= 45f; x += 6f)
            {
                CreateQuadMarking("SideRoadMarking_Center1_" + x, markingsTransform, new Vector3(x, 0.11f, 0f), Quaternion.Euler(90f, 90f, 0f), new Vector3(0.15f, 3f, 1f), markingMat);
            }
            CreateQuadMarking("SideRoadMarking_Border1_Left", markingsTransform, new Vector3(25f, 0.11f, 4.85f), Quaternion.Euler(90f, 90f, 0f), new Vector3(0.12f, 40f, 1f), markingMat);
            CreateQuadMarking("SideRoadMarking_Border1_Right", markingsTransform, new Vector3(30f, 0.11f, -4.85f), Quaternion.Euler(90f, 90f, 0f), new Vector3(0.12f, 50f, 1f), markingMat);

            // Segment 2 Şeritleri (Z = 0f to 30f, X = 50f)
            for (float z = 3f; z <= 27f; z += 6f)
            {
                CreateQuadMarking("SideRoadMarking_Center2_" + z, markingsTransform, new Vector3(50f, 0.11f, z), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.15f, 3f, 1f), markingMat);
            }
            CreateQuadMarking("SideRoadMarking_Border2_East", markingsTransform, new Vector3(54.85f, 0.11f, 15f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.12f, 40f, 1f), markingMat);
            CreateQuadMarking("SideRoadMarking_Border2_West", markingsTransform, new Vector3(45.15f, 0.11f, 15f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.12f, 20f, 1f), markingMat);

            // Segment 3 Şeritleri (X = 5f to 50f, Z = 30f)
            for (float x = 7f; x <= 45f; x += 6f)
            {
                CreateQuadMarking("SideRoadMarking_Center3_" + x, markingsTransform, new Vector3(x, 0.11f, 30f), Quaternion.Euler(90f, 90f, 0f), new Vector3(0.15f, 3f, 1f), markingMat);
            }
            CreateQuadMarking("SideRoadMarking_Border3_North", markingsTransform, new Vector3(30f, 0.11f, 34.85f), Quaternion.Euler(90f, 90f, 0f), new Vector3(0.12f, 50f, 1f), markingMat);
            CreateQuadMarking("SideRoadMarking_Border3_South", markingsTransform, new Vector3(25f, 0.11f, 25.15f), Quaternion.Euler(90f, 90f, 0f), new Vector3(0.12f, 40f, 1f), markingMat);

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
                        speedTextTrans.localScale = new Vector3(-0.08f, 0.08f, 0.08f);
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
                        signalTextTrans.localScale = new Vector3(-0.05f, 0.05f, 0.05f);
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
                        gearTextTrans.localScale = new Vector3(-0.05f, 0.05f, 0.05f);
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

        private static GameObject GetOrCreatePrimitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
            }
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null)
            {
                rend.sharedMaterial = mat;
            }
            EditorUtility.SetDirty(go);
            return go;
        }

        private static GameObject CreateQuadMarking(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            if (parent != null) quad.transform.SetParent(parent, false);
            
            quad.transform.position = pos;
            quad.transform.rotation = rot;
            quad.transform.localScale = scale;
            
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            
            Renderer r = quad.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            
            EditorUtility.SetDirty(quad);
            return quad;
        }
    }
}
