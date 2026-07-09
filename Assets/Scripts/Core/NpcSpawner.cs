using UnityEngine;
using System.Collections;

namespace TrafikParkuru.Core
{
    /// <summary>
    /// Haritaya NPC araçlar ve yayalar yerleştirir.
    ///
    /// Sahne sabitleri:
    ///   Yol yüzeyi     Y = 0.10
    ///   Kaldırım yüzeyi Y = 0.155  (Y=0.08, scale Y=0.15)
    ///   Sağ şerit (→+Z) X = +1.8
    ///   Sol şerit (→-Z) X = -1.8
    ///   Sol kaldırım   X = -5.5
    ///   Sağ kaldırım   X = +5.5
    ///   Yaya geçidi    Z = -60
    /// </summary>
    public class NpcSpawner : MonoBehaviour
    {
        [Header("NPC Prefabları")]
        [SerializeField] private GameObject npcCarPrefab;
        [SerializeField] private GameObject npcPedestrianPrefab;

        // ── Sahne sabitleri ─────────────────────────────────────────────────
        private const float RoadY      = 0.10f;   // Yol yüzey Y
        private const float SidewalkY  = 0.16f;   // Kaldırım yüzey Y
        private const float RightLaneX = 1.8f;    // Sağ şerit X (+Z yönü)
        private const float LeftLaneX  = -1.8f;   // Sol şerit X (-Z yönü)
        private const float SideRoadRightZ = -1.8f; // Yan yol sağ şerit Z
        private const float SideRoadLeftZ  =  1.8f; // Yan yol sol şerit Z
        private const float LeftSidewalkX  = -5.5f;
        private const float RightSidewalkX =  5.5f;
        private const float CrosswalkZ     = -60f;  // Yaya geçidi Z

        // Araç spawn Y = RoadY; NpcDriver.ComputeLockedY BoxCollider bounds'tan doğru pivot Y'yi hesaplar.

        private void Start()
        {
            SpawnCars();
            SpawnPedestrians();
            SpawnCrosswalkMarking(); // Kaldırımdan kaldırıma tam yaya geçidi şeritleri
        }

        // ════════════════════════════════════════════════════════════════════
        // ARAÇLAR
        // ════════════════════════════════════════════════════════════════════

        private void SpawnCars()
        {
            float carY = RoadY; // NpcDriver.ComputeLockedY collider bounds'tan gerçek yüksekliği hesaplar

            // Sağ şerit waypoint'leri (+Z yönü)
            Vector3[] rightLoop = {
                new Vector3(RightLaneX, carY, -130f),
                new Vector3(RightLaneX, carY,  -70f),
                new Vector3(RightLaneX, carY,    0f),
                new Vector3(RightLaneX, carY,   70f),
                new Vector3(RightLaneX, carY,  130f),
            };

            // Sol şerit waypoint'leri (-Z yönü)
            Vector3[] leftLoop = {
                new Vector3(LeftLaneX, carY,  130f),
                new Vector3(LeftLaneX, carY,   70f),
                new Vector3(LeftLaneX, carY,    0f),
                new Vector3(LeftLaneX, carY,  -70f),
                new Vector3(LeftLaneX, carY, -130f),
            };

            // Yan yol düz gidiş (+X yönü)
            Vector3[] sideLoop = {
                new Vector3(10f,  carY, SideRoadRightZ),
                new Vector3(30f,  carY, SideRoadRightZ),
                new Vector3(50f,  carY, SideRoadRightZ),
            };

            // Spawn noktaları: (pozisyon, açı, waypoints, hız)
            var cars = new (Vector3 pos, float yaw, Vector3[] wps, float spd)[]
            {
                // Sağ şerit — 3 araç aralıklı
                (new Vector3(RightLaneX, carY, -110f), 0f,   rightLoop, Random.Range(6f, 8f)),
                (new Vector3(RightLaneX, carY,  -20f), 0f,   rightLoop, Random.Range(6f, 8f)),
                (new Vector3(RightLaneX, carY,   80f), 0f,   rightLoop, Random.Range(6f, 8f)),

                // Sol şerit — 3 araç aralıklı
                (new Vector3(LeftLaneX,  carY,  110f), 180f, leftLoop,  Random.Range(5f, 7f)),
                (new Vector3(LeftLaneX,  carY,   10f), 180f, leftLoop,  Random.Range(5f, 7f)),
                (new Vector3(LeftLaneX,  carY,  -80f), 180f, leftLoop,  Random.Range(5f, 7f)),

                // Yan yol — 1 araç
                (new Vector3(15f, carY, SideRoadRightZ), 90f, sideLoop, Random.Range(4f, 6f)),
            };

            foreach (var c in cars)
            {
                GameObject car = CreateNpcCar(c.pos, Quaternion.Euler(0f, c.yaw, 0f));

                NpcDriver driver       = car.AddComponent<NpcDriver>();
                driver.waypoints       = c.wps;
                driver.speed           = c.spd;
                driver.rotationSpeed   = 4f;
                driver.arrivalDistance = 4f;
                driver.detectionDistance = 14f;
                driver.stopDistance    = 5f;
                driver.roadSurfaceY    = RoadY;
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
                SetupNpcCarPhysics(car);
                PaintNpcCar(car);
            }
            else
            {
                car = CreateBoxCar(position, rotation);
            }

            car.transform.SetParent(transform, true);
            return car;
        }

        private void SetupNpcCarPhysics(GameObject car)
        {
            // Fabric vs. gereksiz objeleri kaldır
            var toDelete = new System.Collections.Generic.List<GameObject>();
            foreach (Transform t in car.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.ToLower().Contains("fabric"))
                    toDelete.Add(t.gameObject);
            foreach (var g in toDelete) DestroyImmediate(g);

            // BoxCollider
            BoxCollider col = car.GetComponent<BoxCollider>();
            if (col == null) col = car.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.012f, 0.005f);
            col.size   = new Vector3(0.03f, 0.024f, 0.07f);

            // Rigidbody — kinematic (NpcDriver kendi hareketi kontrol eder)
            Rigidbody rb = car.GetComponent<Rigidbody>();
            if (rb == null) rb = car.AddComponent<Rigidbody>();
            rb.mass        = 1500f;
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        private void PaintNpcCar(GameObject car)
        {
            Color bodyColor = RandomCarColor();
            foreach (Renderer r in car.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                string n = r.gameObject.name.ToLower();
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                if (n.Contains("glass") || n.Contains("windshield") || n.Contains("window"))
                {
                    m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.05f, 0.9f));
                    m.SetFloat("_Smoothness", 0.92f);
                    m.SetFloat("_Metallic",   0.9f);
                }
                else if (n.Contains("wheel") || n.Contains("tire"))
                {
                    m.SetColor("_BaseColor", new Color(0.10f, 0.10f, 0.10f));
                    m.SetFloat("_Smoothness", 0.1f);
                    m.SetFloat("_Metallic",   0.0f);
                }
                else if (n.Contains("body") || n.Contains("toycar") || n == car.name.ToLower())
                {
                    m.SetColor("_BaseColor", bodyColor);
                    m.SetFloat("_Smoothness", 0.85f);
                    m.SetFloat("_Metallic",   0.8f);
                }
                else
                {
                    m.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.20f));
                    m.SetFloat("_Smoothness", 0.4f);
                    m.SetFloat("_Metallic",   0.4f);
                }
                r.sharedMaterial = m;
            }
        }

        private GameObject CreateBoxCar(Vector3 position, Quaternion rotation)
        {
            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "NPC_Car";
            car.transform.position   = position;
            car.transform.rotation   = rotation;
            car.transform.localScale = new Vector3(1.6f, 0.8f, 3.5f);

            Renderer rend = car.GetComponent<Renderer>();
            if (rend != null)
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", RandomCarColor());
                m.SetFloat("_Smoothness", 0.7f);
                m.SetFloat("_Metallic",   0.5f);
                rend.sharedMaterial = m;
            }

            Rigidbody rb = car.GetComponent<Rigidbody>() ?? car.AddComponent<Rigidbody>();
            rb.mass = 1500f; rb.isKinematic = true; rb.useGravity = false;
            return car;
        }

        // ════════════════════════════════════════════════════════════════════
        // YAYALAR
        // ════════════════════════════════════════════════════════════════════

        private void SpawnPedestrians()
        {
            // Sol kaldırımda ileri/geri yürüyenler (3 kişi)
            SpawnSidewalkWalker(new Vector3(LeftSidewalkX, SidewalkY, -100f), direction: 1);
            SpawnSidewalkWalker(new Vector3(LeftSidewalkX, SidewalkY,  -30f), direction:-1);
            SpawnSidewalkWalker(new Vector3(LeftSidewalkX, SidewalkY,   50f), direction: 1);

            // Sağ kaldırımda ileri/geri yürüyenler (2 kişi)
            SpawnSidewalkWalker(new Vector3(RightSidewalkX, SidewalkY,  -80f), direction:-1);
            SpawnSidewalkWalker(new Vector3(RightSidewalkX, SidewalkY,   20f), direction: 1);

            // Yaya geçidinden geçenler (2 kişi — sol → sağ ve sağ → sol)
            SpawnCrosswalkPedestrian(startX: LeftSidewalkX  - 1f, endX: RightSidewalkX + 1f);
            SpawnCrosswalkPedestrian(startX: RightSidewalkX + 1f, endX: LeftSidewalkX  - 1f);
        }

        private void SpawnSidewalkWalker(Vector3 startPos, int direction)
        {
            GameObject ped = CreatePedestrian(startPos);
            var walker = ped.AddComponent<SidewalkWalker>();
            walker.sidewalkY  = SidewalkY;
            walker.speed      = Random.Range(1.4f, 2.0f);
            walker.rangeZ     = Random.Range(20f, 35f);
            walker.initialDir = direction;
        }

        private void SpawnCrosswalkPedestrian(float startX, float endX)
        {
            Vector3 startPos = new Vector3(startX, SidewalkY, CrosswalkZ);
            GameObject ped   = CreatePedestrian(startPos);
            var crosser      = ped.AddComponent<CrosswalkWalker>();
            crosser.sidewalkY  = SidewalkY;
            crosser.startX     = startX;
            crosser.endX       = endX;
            crosser.crosswalkZ = CrosswalkZ;
            crosser.speed      = Random.Range(1.2f, 1.8f);
            crosser.waitTime   = Random.Range(3f, 8f);  // Başlamadan önce bekle
        }

        private GameObject CreatePedestrian(Vector3 position)
        {
            GameObject ped;
            if (npcPedestrianPrefab != null)
            {
                ped = Instantiate(npcPedestrianPrefab, position, Quaternion.identity);
                ped.name = "NPC_Pedestrian";
                ApplyPedestrianColors(ped);
            }
            else
            {
                ped = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ped.name = "NPC_Pedestrian";
                ped.transform.position   = position;
                ped.transform.localScale = new Vector3(0.4f, 0.85f, 0.4f);
                Renderer r = ped.GetComponent<Renderer>();
                if (r != null)
                {
                    Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.SetColor("_BaseColor", RandomPedestrianColor());
                    r.sharedMaterial = m;
                }
            }

            // Rigidbody (kinematic — script kontrol eder)
            Rigidbody rb = ped.GetComponent<Rigidbody>() ?? ped.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            ped.transform.SetParent(transform, true);
            return ped;
        }

        private void ApplyPedestrianColors(GameObject ped)
        {
            Color c = RandomPedestrianColor();
            foreach (Renderer r in ped.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                // Orijinal texture'ı koru, sadece renge karıştır
                Texture tex = null;
                if (r.sharedMaterial != null)
                {
                    if (r.sharedMaterial.HasProperty("_BaseMap")) tex = r.sharedMaterial.GetTexture("_BaseMap");
                    else if (r.sharedMaterial.HasProperty("_MainTex")) tex = r.sharedMaterial.GetTexture("_MainTex");
                }
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (tex != null) m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", tex != null ? Color.Lerp(Color.white, c, 0.35f) : c);
                m.SetFloat("_Smoothness", 0.05f);
                r.sharedMaterial = m;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // YAYA GEÇİDİ MARKALARI — kaldırımdan kaldırıma tam şeritler
        // ════════════════════════════════════════════════════════════════════

        private void SpawnCrosswalkMarking()
        {
            // Sahne'deki ZebraCrossing yolun ortasını kaplıyor (X:-3..+3).
            // Kaldırım bölgelerini de kapatmak için kaldırım üstüne şeritler ekliyoruz.
            // Şerit parametreleri sahneyle eşleştirildi:
            //   Genişlik (X) : 0.60  |  Yükseklik (Y) : 0.02  |  Derinlik (Z) : 4.0
            //   Renk: Beyaz   |  Y pozisyonu: 0.07 (yol/kaldırım üstünde)

            const float stripeW  = 0.60f;
            const float stripeD  = 4.0f;    // Z boyutu (yaya geçidi derinliği)
            const float stripeH  = 0.02f;
            const float stripeY  = 0.07f;   // Zemin üstünde ince tabaka
            const float spacing  = 1.50f;   // Şerit aralığı (sahneyle aynı)

            // Sol kaldırım şeritleri: X = -5.5 ± (kaldırım genişliği/2)
            // Kaldırım X merkezi = -5.5, genişlik ≈ 1 unit → şeritler X: -5.75 ve -5.25
            float[] leftSidewalkX  = { -4.75f, -5.25f };
            float[] rightSidewalkX = {  4.75f,  5.25f };

            Material whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            whiteMat.SetColor("_BaseColor", Color.white);
            whiteMat.SetFloat("_Smoothness", 0.05f);

            GameObject crosswalkParent = new GameObject("NPC_CrosswalkExtension");
            crosswalkParent.transform.SetParent(transform, false);

            // Sol kaldırım şeritleri
            foreach (float sx in leftSidewalkX)
                CreateStripe(crosswalkParent.transform, sx, stripeY, CrosswalkZ,
                             stripeW, stripeH, stripeD, whiteMat);

            // Sağ kaldırım şeritleri
            foreach (float sx in rightSidewalkX)
                CreateStripe(crosswalkParent.transform, sx, stripeY, CrosswalkZ,
                             stripeW, stripeH, stripeD, whiteMat);
        }

        private static void CreateStripe(Transform parent, float x, float y, float z,
                                         float w, float h, float d, Material mat)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.name = "CrosswalkStripe";
            Destroy(s.GetComponent<BoxCollider>()); // Fizik etkileşimi istemiyoruz
            s.transform.SetParent(parent, false);
            s.transform.localPosition = new Vector3(x, y, z);
            s.transform.localScale    = new Vector3(w, h, d);
            Renderer r = s.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        // ── Renk yardımcıları ────────────────────────────────────────────────
        private static readonly Color[] CarColors = {
            new Color(0.62f, 0.07f, 0.07f), // Crimson
            new Color(0.06f, 0.06f, 0.08f), // Black
            new Color(0.93f, 0.93f, 0.93f), // White
            new Color(0.38f, 0.40f, 0.43f), // Gray
            new Color(0.04f, 0.32f, 0.72f), // Blue
            new Color(0.88f, 0.68f, 0.04f), // Yellow
            new Color(0.04f, 0.48f, 0.24f), // Green
        };

        private static readonly Color[] PedColors = {
            new Color(0.10f, 0.38f, 0.76f),
            new Color(0.76f, 0.18f, 0.18f),
            new Color(0.18f, 0.56f, 0.28f),
            new Color(0.86f, 0.56f, 0.08f),
            new Color(0.46f, 0.18f, 0.58f),
            new Color(0.38f, 0.28f, 0.18f),
        };

        private static Color RandomCarColor()        => CarColors[Random.Range(0, CarColors.Length)];
        private static Color RandomPedestrianColor() => PedColors[Random.Range(0, PedColors.Length)];
    }

    // ════════════════════════════════════════════════════════════════════════
    // KALDIRM YÜRÜYÜŞÜ — kaldırımda ileri/geri yürür
    // ════════════════════════════════════════════════════════════════════════
    public class SidewalkWalker : MonoBehaviour
    {
        public float sidewalkY  = 0.16f;
        public float speed      = 1.6f;
        public float rangeZ     = 25f;
        public int   initialDir = 1;   // +1 = +Z, -1 = -Z

        private Vector3  startPos;
        private int      dir;
        private Animator anim;

        private void Start()
        {
            startPos = transform.position;
            dir      = initialDir;
            anim     = GetComponentInChildren<Animator>();
            FaceDir();
            SnapY();
        }

        private void Update()
        {
            transform.position += Vector3.forward * dir * speed * Time.deltaTime;
            SnapY();

            if (anim != null) anim.speed = speed / 2.0f;

            // Döndür
            if (dir > 0 && transform.position.z > startPos.z + rangeZ)
            { dir = -1; FaceDir(); }
            else if (dir < 0 && transform.position.z < startPos.z - rangeZ)
            { dir =  1; FaceDir(); }
        }

        private void FaceDir()
            => transform.rotation = Quaternion.Euler(0f, dir > 0 ? 0f : 180f, 0f);

        private void SnapY()
        {
            Vector3 p = transform.position;
            p.y = sidewalkY;
            transform.position = p;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // YAYA GEÇİDİ YÜRÜYÜŞÜ — soldan sağa (veya tersi) geçiş yapar, bekler, tekrar
    // ════════════════════════════════════════════════════════════════════════
    public class CrosswalkWalker : MonoBehaviour
    {
        public float sidewalkY  = 0.16f;
        public float startX;
        public float endX;
        public float crosswalkZ = -60f;
        public float speed      = 1.5f;
        public float waitTime   = 5f;  // İlk başlamadan önce bekleme

        private enum State { Waiting, Walking, Pause }
        private State  state = State.Waiting;
        private float  timer;
        private float  currentStartX;
        private float  currentEndX;
        private Animator anim;

        private void Start()
        {
            anim  = GetComponentInChildren<Animator>();
            timer = waitTime;

            // Yaya geçidi Z'sinde başlat
            Vector3 p = transform.position;
            p.z = crosswalkZ;
            p.y = sidewalkY;
            transform.position = p;

            currentStartX = startX;
            currentEndX   = endX;
            FaceTarget();
        }

        private void Update()
        {
            SnapY();

            switch (state)
            {
                case State.Waiting:
                    timer -= Time.deltaTime;
                    if (anim != null) anim.speed = 0f;
                    if (timer <= 0f)
                    {
                        state = State.Walking;
                        FaceTarget();
                    }
                    break;

                case State.Walking:
                    if (anim != null) anim.speed = speed / 1.8f;

                    // X ekseninde ilerle
                    float dir   = Mathf.Sign(currentEndX - transform.position.x);
                    transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);

                    // Hedefe ulaştı mı?
                    bool arrived = dir > 0
                        ? transform.position.x >= currentEndX
                        : transform.position.x <= currentEndX;

                    if (arrived)
                    {
                        // Pozisyonu kilitle, kısa bekleme sonra geri dön
                        Vector3 pos = transform.position; pos.x = currentEndX;
                        transform.position = pos;

                        // Başlangıç/bitiş swap et
                        (currentStartX, currentEndX) = (currentEndX, currentStartX);
                        state = State.Pause;
                        timer = Random.Range(4f, 10f);
                    }
                    break;

                case State.Pause:
                    if (anim != null) anim.speed = 0f;
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        state = State.Walking;
                        FaceTarget();
                    }
                    break;
            }
        }

        private void FaceTarget()
        {
            float dir = currentEndX - transform.position.x;
            transform.rotation = Quaternion.Euler(0f, dir > 0 ? 90f : -90f, 0f);
        }

        private void SnapY()
        {
            Vector3 p = transform.position;
            p.y = sidewalkY;
            p.z = crosswalkZ;  // Z'yi sabit tut — yaya geçidi boyunca yürür
            transform.position = p;
        }
    }
}
