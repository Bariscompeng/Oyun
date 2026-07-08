using UnityEngine;
using System.Collections.Generic;

namespace TrafikParkuru.Core
{
    /// <summary>
    /// Haritaya statik (park halinde) ve hareketli NPC araçlar/yayalar yerleştirir.
    /// Tamamen performans dostu, basit matematiksel hareketler kullanır.
    /// </summary>
    public class NpcSpawner : MonoBehaviour
    {
        [Header("NPC Prefabları (Opsiyonel)")]
        [SerializeField] private GameObject npcCarPrefab;
        [SerializeField] private GameObject npcPedestrianPrefab;

        private List<GameObject> activeNpcs = new List<GameObject>();

        // Yol sabitleri
        // Ana yol: X = -5 → +5, merkez çizgi X = 0
        // Sağ şerit (ileri): X ≈ +1.8  (merkez)
        // Sol şerit (geri):  X ≈ -1.8  (merkez)
        // Yan yol: Z ekseninde, Z = -5 → +5 arası kavşak bölgesi
        private const float rightLaneX = 1.8f;
        private const float leftLaneX = -1.8f;
        private const float spawnY = 0.1f; // Yol yüzeyi seviyesi (raycast ile düzeltilecek)
        private const float sideRoadRightZ = -1.8f;
        private const float sideRoadLeftZ = 1.8f;

        private void Start()
        {
            SpawnMovingCars();
            SpawnSidewalkPedestrians();
        }

        private struct SpawnData
        {
            public Vector3 pos;
            public float angle;
            public Vector3[] loop;
            public float speed;
        }

        private void SpawnMovingCars()
        {
            // === SAĞ ŞERİT: +Z yönünde ilerliyor (x = +1.8) ===
            Vector3[] rightLaneForward = new Vector3[]
            {
                new Vector3(rightLaneX, spawnY, -145f),
                new Vector3(rightLaneX, spawnY, -100f),
                new Vector3(rightLaneX, spawnY, -50f),
                new Vector3(rightLaneX, spawnY, 0f),
                new Vector3(rightLaneX, spawnY, 50f),
                new Vector3(rightLaneX, spawnY, 100f),
                new Vector3(rightLaneX, spawnY, 145f),
            };

            // === SOL ŞERİT: -Z yönünde ilerliyor (x = -1.8) ===
            Vector3[] leftLaneForward = new Vector3[]
            {
                new Vector3(leftLaneX, spawnY, 145f),
                new Vector3(leftLaneX, spawnY, 100f),
                new Vector3(leftLaneX, spawnY, 50f),
                new Vector3(leftLaneX, spawnY, 0f),
                new Vector3(leftLaneX, spawnY, -50f),
                new Vector3(leftLaneX, spawnY, -100f),
                new Vector3(leftLaneX, spawnY, -145f),
            };

            // === SAĞ ŞERİT → Kavşaktan sağa dönen rota ===
            Vector3[] rightTurnRoute = new Vector3[]
            {
                new Vector3(rightLaneX, spawnY, -145f),
                new Vector3(rightLaneX, spawnY, -20f),
                new Vector3(rightLaneX, spawnY, -5f),
                // Kavşakta sağa dönüş (yumuşak viraj)
                new Vector3(3.5f, spawnY, -1.8f),
                new Vector3(5f, spawnY, sideRoadRightZ),
                new Vector3(15f, spawnY, sideRoadRightZ),
                new Vector3(30f, spawnY, sideRoadRightZ),
                new Vector3(50f, spawnY, sideRoadRightZ),
            };

            // === Yan yoldan ana yola çıkan rota (soldan sağa) ===
            Vector3[] sideToMainRoute = new Vector3[]
            {
                new Vector3(50f, spawnY, sideRoadLeftZ),
                new Vector3(30f, spawnY, sideRoadLeftZ),
                new Vector3(15f, spawnY, sideRoadLeftZ),
                new Vector3(5f, spawnY, sideRoadLeftZ),
                // Kavşakta sola dönüş
                new Vector3(3.5f, spawnY, 1.8f),
                new Vector3(rightLaneX, spawnY, 5f),
                new Vector3(rightLaneX, spawnY, 50f),
                new Vector3(rightLaneX, spawnY, 100f),
                new Vector3(rightLaneX, spawnY, 145f),
            };

            // Spawn pozisyonları
            SpawnData[] spawns = new SpawnData[]
            {
                // === Ana yol sağ şerit (+Z yönü, ilerleyen) ===
                new SpawnData { pos = new Vector3(rightLaneX, spawnY, -125f), angle = 0f,   loop = rightLaneForward, speed = Random.Range(6f, 9f) },
                new SpawnData { pos = new Vector3(rightLaneX, spawnY, -60f),  angle = 0f,   loop = rightLaneForward, speed = Random.Range(6f, 9f) },
                new SpawnData { pos = new Vector3(rightLaneX, spawnY, 30f),   angle = 0f,   loop = rightLaneForward, speed = Random.Range(6f, 9f) },
                new SpawnData { pos = new Vector3(rightLaneX, spawnY, 90f),   angle = 0f,   loop = rightLaneForward, speed = Random.Range(6f, 9f) },

                // === Ana yol sol şerit (-Z yönü, karşıdan gelen) ===
                new SpawnData { pos = new Vector3(leftLaneX, spawnY, 120f),  angle = 180f, loop = leftLaneForward,  speed = Random.Range(5f, 8f) },
                new SpawnData { pos = new Vector3(leftLaneX, spawnY, 60f),   angle = 180f, loop = leftLaneForward,  speed = Random.Range(5f, 8f) },
                new SpawnData { pos = new Vector3(leftLaneX, spawnY, -20f),  angle = 180f, loop = leftLaneForward,  speed = Random.Range(5f, 8f) },
                new SpawnData { pos = new Vector3(leftLaneX, spawnY, -90f),  angle = 180f, loop = leftLaneForward,  speed = Random.Range(5f, 8f) },

                // === Kavşaktan sağa dönen araç ===
                new SpawnData { pos = new Vector3(rightLaneX, spawnY, -40f), angle = 0f,   loop = rightTurnRoute,   speed = Random.Range(5f, 7f) },

                // === Yan yoldan ana yola çıkan araç ===
                new SpawnData { pos = new Vector3(40f, spawnY, sideRoadLeftZ), angle = 270f, loop = sideToMainRoute, speed = Random.Range(5f, 7f) },

                // === Yan yol düz trafik ===
                new SpawnData { pos = new Vector3(20f, spawnY, sideRoadRightZ), angle = 90f, loop = new Vector3[] {
                    new Vector3(10f, spawnY, sideRoadRightZ),
                    new Vector3(25f, spawnY, sideRoadRightZ),
                    new Vector3(45f, spawnY, sideRoadRightZ),
                }, speed = Random.Range(5f, 7f) },
            };

            foreach (var spawn in spawns)
            {
                GameObject car = CreateNpcCar(spawn.pos, Quaternion.Euler(0f, spawn.angle, 0f));
                car.name = "MovingNPC_Car";

                // NpcDriver bileşenini ekle
                var driver = car.AddComponent<NpcDriver>();
                driver.waypoints = spawn.loop;
                driver.speed = spawn.speed;
                driver.rotationSpeed = 4f;
                driver.arrivalDistance = 4f;
                driver.detectionDistance = 15f;
                driver.stopDistance = 5f;
                driver.groundOffset = 0.4f;
            }
        }

        private void SpawnSidewalkPedestrians()
        {
            // Kaldırımda yürüyen yayalar
            Vector3[] pedPositions = new Vector3[]
            {
                new Vector3(-5.5f, 0.9f, -110f),
                new Vector3(5.5f, 0.9f, -70f),
                new Vector3(-5.5f, 0.9f, -30f),
                new Vector3(5.5f, 0.9f, 10f),
                new Vector3(5.5f, 0.9f, 50f)
            };

            foreach (var pos in pedPositions)
            {
                GameObject ped = CreateNpcPedestrian(pos, Random.Range(1.8f, 2.4f));
                ped.name = "NPC_Pedestrian";
            }
        }

        private GameObject CreateNpcCar(Vector3 position, Quaternion rotation)
        {
            GameObject car;
            if (npcCarPrefab != null)
            {
                car = Instantiate(npcCarPrefab, position, rotation);
                car.name = "NPC_Car";
                car.transform.localScale = new Vector3(55f, 55f, 55f);
                ApplyNpcCarUpgrades(car);
            }
            else
            {
                // Basit renkli bir kutu oluştur
                car = GameObject.CreatePrimitive(PrimitiveType.Cube);
                car.name = "NPC_Car";
                car.transform.position = position;
                car.transform.rotation = rotation;
                car.transform.localScale = new Vector3(1.6f, 0.8f, 3.5f);

                Renderer rend = car.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    newMat.SetColor("_BaseColor", GetRandomCarColor());
                    newMat.SetFloat("_Smoothness", 0.7f);
                    newMat.SetFloat("_Metallic", 0.5f);
                    rend.sharedMaterial = newMat;
                }

                // Tekerlek taklidi
                CreateWheelVisuals(car);

                Rigidbody rb = car.GetComponent<Rigidbody>();
                if (rb == null) rb = car.AddComponent<Rigidbody>();
                rb.mass = 1500f;
                rb.isKinematic = true;
            }

            car.transform.SetParent(transform, true);
            activeNpcs.Add(car);
            return car;
        }

        private void ApplyNpcCarUpgrades(GameObject car)
        {
            if (car == null) return;

            // 0. Platform Kaldırma (Fabric nesnesini tamamen yok et)
            var fabricList = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in car.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name.ToLower().Contains("fabric"))
                {
                    fabricList.Add(child.gameObject);
                }
            }
            foreach (var f in fabricList)
            {
                DestroyImmediate(f);
            }

            // 1. Fiziksel Çarpıştırıcı ve Rigidbody Kurulumu
            BoxCollider col = car.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = car.AddComponent<BoxCollider>();
            }
            col.center = new Vector3(0f, 0.012f, 0.005f);
            col.size = new Vector3(0.03f, 0.024f, 0.07f);

            Rigidbody rb = car.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = car.AddComponent<Rigidbody>();
            }
            rb.mass = 1500f;
            rb.isKinematic = true;

            // 2. Materyal Giydirme
            Renderer[] renderers = car.GetComponentsInChildren<Renderer>(true);
            Color bodyColor = GetRandomDetailedCarColor();
            foreach (var r in renderers)
            {
                Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                if (r.gameObject.name.Contains("Glass") || r.gameObject.name.Contains("glass") || r.gameObject.name.Contains("Windshield") || r.gameObject.name.Contains("Window"))
                {
                    // Parlak Koyu Cam
                    newMat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f, 0.95f));
                    newMat.SetFloat("_Smoothness", 0.9f);
                    newMat.SetFloat("_Metallic", 0.9f);
                    r.sharedMaterial = newMat;
                }
                else if (r.gameObject.name.Contains("ToyCar") || r.gameObject.name.Contains("Body") || r.gameObject.name.Contains("body") || r.gameObject.name == car.name)
                {
                    // Metalik Araba Boyası
                    newMat.SetColor("_BaseColor", bodyColor);
                    newMat.SetFloat("_Smoothness", 0.85f);
                    newMat.SetFloat("_Metallic", 0.8f);
                    r.sharedMaterial = newMat;
                }
                else if (r.gameObject.name.Contains("Wheel") || r.gameObject.name.Contains("wheel") || r.gameObject.name.Contains("Tire") || r.gameObject.name.Contains("tire"))
                {
                    // Mat Lastik Kauçuğu
                    newMat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.12f));
                    newMat.SetFloat("_Smoothness", 0.15f);
                    newMat.SetFloat("_Metallic", 0.05f);
                    r.sharedMaterial = newMat;
                }
                else
                {
                    // Diğer Parçalar
                    newMat.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.22f));
                    newMat.SetFloat("_Smoothness", 0.5f);
                    newMat.SetFloat("_Metallic", 0.5f);
                    r.sharedMaterial = newMat;
                }
            }
        }

        private Color GetRandomDetailedCarColor()
        {
            Color[] colors = new Color[]
            {
                new Color(0.65f, 0.08f, 0.08f), // Crimson red
                new Color(0.08f, 0.08f, 0.09f), // Midnight black
                new Color(0.95f, 0.95f, 0.95f), // Pearl white
                new Color(0.4f, 0.42f, 0.45f),  // Slate gray
                new Color(0.05f, 0.35f, 0.75f), // Electric blue
                new Color(0.9f, 0.7f, 0.05f),   // Sunburst yellow
                new Color(0.05f, 0.5f, 0.25f)   // Forest green
            };
            return colors[Random.Range(0, colors.Length)];
        }

        private GameObject CreateNpcPedestrian(Vector3 position, float walkSpeed)
        {
            GameObject ped;
            if (npcPedestrianPrefab != null)
            {
                ped = Instantiate(npcPedestrianPrefab, position, Quaternion.identity);
                ped.name = "NPC_Pedestrian";
                ApplyNpcPedestrianUpgrades(ped);
            }
            else
            {
                ped = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ped.name = "NPC_Pedestrian";
                ped.transform.position = position;
                ped.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

                Renderer rend = ped.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    newMat.SetColor("_BaseColor", Color.green);
                    newMat.SetFloat("_Smoothness", 0.1f);
                    newMat.SetFloat("_Metallic", 0.0f);
                    rend.sharedMaterial = newMat;
                }
            }

            ped.transform.SetParent(transform, true);
            activeNpcs.Add(ped);

            // Sağa/sola veya ileri/geri küçük gezinme scripti ekle
            var wander = ped.AddComponent<SimpleWanderer>();
            wander.speed = walkSpeed;
            wander.rangeZ = 15f;

            return ped;
        }

        private void ApplyNpcPedestrianUpgrades(GameObject ped)
        {
            if (ped == null) return;

            Renderer[] renderers = ped.GetComponentsInChildren<Renderer>(true);
            Color clothesColor = GetRandomPedestrianColor();
            foreach (var r in renderers)
            {
                Texture mainTex = null;
                if (r.sharedMaterial != null)
                {
                    if (r.sharedMaterial.HasProperty("_BaseMap")) mainTex = r.sharedMaterial.GetTexture("_BaseMap");
                    else if (r.sharedMaterial.HasProperty("_MainTex")) mainTex = r.sharedMaterial.GetTexture("_MainTex");
                }

                Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mainTex != null)
                {
                    newMat.SetTexture("_BaseMap", mainTex);
                    newMat.SetColor("_BaseColor", Color.Lerp(Color.white, clothesColor, 0.4f));
                }
                else
                {
                    newMat.SetColor("_BaseColor", clothesColor);
                }
                newMat.SetFloat("_Smoothness", 0.1f);
                newMat.SetFloat("_Metallic", 0.0f);
                r.sharedMaterial = newMat;
            }
        }

        private Color GetRandomPedestrianColor()
        {
            Color[] colors = new Color[]
            {
                new Color(0.1f, 0.4f, 0.8f),  // Blue
                new Color(0.8f, 0.2f, 0.2f),  // Red
                new Color(0.2f, 0.6f, 0.3f),  // Green
                new Color(0.9f, 0.6f, 0.1f),  // Orange
                new Color(0.5f, 0.2f, 0.6f),  // Purple
                new Color(0.4f, 0.3f, 0.2f)   // Brown
            };
            return colors[Random.Range(0, colors.Length)];
        }

        private Color GetRandomCarColor()
        {
            Color[] colors = new Color[] { Color.red, Color.gray, Color.black, Color.white, new Color(0f, 0.5f, 1f) };
            return colors[Random.Range(0, colors.Length)];
        }

        private void CreateWheelVisuals(GameObject parent)
        {
            Vector3[] wheelOffsets = new Vector3[]
            {
                new Vector3(-0.9f, -0.3f, 1.2f),
                new Vector3(0.9f, -0.3f, 1.2f),
                new Vector3(-0.9f, -0.3f, -1.2f),
                new Vector3(0.9f, -0.3f, -1.2f)
            };

            foreach (var offset in wheelOffsets)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(parent.transform, false);
                wheel.transform.localPosition = offset;
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
                
                Renderer r = wheel.GetComponent<Renderer>();
                if (r != null) r.material.color = Color.black;
                
                Collider col = wheel.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
        }
    }

    // Basit ileri/geri hareket scripti
    public class SimpleMover : MonoBehaviour
    {
        public Vector3 direction = Vector3.forward;
        public float speed = 5f;
        public float destroyDistance = 200f;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, startPos) > destroyDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    // Kaldırımda gezinme scripti
    public class SimpleWanderer : MonoBehaviour
    {
        public float speed = 2.0f;
        public float rangeZ = 10f;
        private Vector3 startPos;
        private int direction = 1;
        private Animator animator;

        private void Start()
        {
            startPos = transform.position;
            // Çocuk nesnelerdeki Animator'ı bul (Pedestrian modelinde Animator alt nesnededir)
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            transform.position += Vector3.forward * direction * speed * Time.deltaTime;

            if (animator != null)
            {
                // CesiumManWalk varsayılan hızı hızlıdır, yürüme hızına göre animasyon hızını oranlayalım
                animator.speed = speed / 2.2f;
            }

            if (direction > 0 && transform.position.z > startPos.z + rangeZ)
            {
                direction = -1;
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else if (direction < 0 && transform.position.z < startPos.z - rangeZ)
            {
                direction = 1;
                transform.rotation = Quaternion.identity;
            }
        }
    }
}
