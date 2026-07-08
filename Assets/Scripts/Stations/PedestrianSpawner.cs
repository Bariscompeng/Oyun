using UnityEngine;

namespace TrafikParkuru.Stations
{
    public class PedestrianSpawner : MonoBehaviour
    {
        [Header("Bağlı İstasyon")]
        [SerializeField] private CrosswalkStation crosswalkStation;

        [Header("Spawn Parametreleri")]
        [SerializeField] private float triggerZ = -90f; // Tetiklenme Z konumu
        [SerializeField] private float spawnX = -7f;
        [SerializeField] private float targetX = 7f;
        [SerializeField] private float crosswalkZ = -60f;
        [SerializeField] private float walkSpeed = 1.6f;

        [Header("Yaya Prefabı (Opsiyonel)")]
        [SerializeField] private GameObject pedestrianPrefab;

        private Transform playerTransform;
        private bool hasSpawned = false;

        private void Start()
        {
            if (crosswalkStation == null)
            {
                crosswalkStation = FindAnyObjectByType<CrosswalkStation>();
            }

            // Oyuncuyu bul
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (hasSpawned || playerTransform == null) return;

            // Oyuncu tetik Z noktasını gectiyse yayayı spawn et
            if (playerTransform.position.z >= triggerZ && playerTransform.position.z < crosswalkZ)
            {
                SpawnPedestrian();
            }
        }

        private void SpawnPedestrian()
        {
            hasSpawned = true;
            Debug.Log("PedestrianSpawner: Yaya üretiliyor...");

            Vector3 spawnPosition = new Vector3(spawnX, 0.05f, crosswalkZ);
            Vector3 targetPosition = new Vector3(targetX, 0.05f, crosswalkZ);

            GameObject pedestrianGo;

            if (pedestrianPrefab != null)
            {
                pedestrianGo = Instantiate(pedestrianPrefab, spawnPosition, Quaternion.identity);
                ApplyPedestrianMaterials(pedestrianGo);
            }
            else
            {
                // Prefab yoksa primitif bir kapsül oluştur
                pedestrianGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                pedestrianGo.name = "Pedestrian";
                pedestrianGo.transform.position = spawnPosition;
                pedestrianGo.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

                // Rigidbody ekle (Carpisma icin sart)
                Rigidbody rb = pedestrianGo.AddComponent<Rigidbody>();
                rb.mass = 60f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                // Materyal rengini değiştir (Görünürlük için)
                Renderer rend = pedestrianGo.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    newMat.SetColor("_BaseColor", Color.blue);
                    newMat.SetFloat("_Smoothness", 0.1f);
                    newMat.SetFloat("_Metallic", 0.0f);
                    rend.sharedMaterial = newMat;
                }
            }

            // PedestrianWalker bileşeni ekle veya al
            PedestrianWalker walker = pedestrianGo.GetComponent<PedestrianWalker>();
            if (walker == null)
            {
                walker = pedestrianGo.AddComponent<PedestrianWalker>();
            }

            // Yürüyüşü başlat
            walker.Initialize(spawnPosition, targetPosition, walkSpeed, crosswalkStation);

            // İstasyona kaydet
            if (crosswalkStation != null)
            {
                crosswalkStation.RegisterPedestrian(walker);
            }
        }

        private void ApplyPedestrianMaterials(GameObject ped)
        {
            if (ped == null) return;
            Renderer[] renderers = ped.GetComponentsInChildren<Renderer>(true);
            Color clothesColor = new Color[]
            {
                new Color(0.1f, 0.4f, 0.8f),  // Blue
                new Color(0.8f, 0.2f, 0.2f),  // Red
                new Color(0.2f, 0.6f, 0.3f),  // Green
                new Color(0.9f, 0.6f, 0.1f),  // Orange
                new Color(0.5f, 0.2f, 0.6f),  // Purple
                new Color(0.4f, 0.3f, 0.2f)   // Brown
            }[Random.Range(0, 6)];

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
    }
}
