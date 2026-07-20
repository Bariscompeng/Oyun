using UnityEngine;

namespace TrafikParkuru.Core
{
    /// <summary>
    /// Yol üzerine şerit işaretleri yerleştirir:
    /// - Ortada kesikli sarı çizgi (dashed center line)
    /// - İki kenarda sürekli beyaz çizgiler (solid edge lines)
    /// Awake'de çalışır, sahnedeki "Road" objesinin boyutlarına göre hesaplar.
    /// </summary>
    [ExecuteAlways]
    public class RoadMarkingPlacer : MonoBehaviour
    {
        [Header("Yol Referansı")]
        [SerializeField] private Transform road;

        [Header("Çizgi Boyutları")]
        [SerializeField] private float dashLength = 3f;
        [SerializeField] private float gapLength = 3f;
        [SerializeField] private float lineWidth = 0.15f;
        [SerializeField] private float edgeLineWidth = 0.12f;

        [Header("Materyaller")]
        [SerializeField] private Material whiteMat;
        [SerializeField] private Material yellowMat;

        private float roadHalfWidth;
        private float roadStartZ;
        private float roadEndZ;
        private float markingY = 0.11f; // yol yüzeyinin hemen üstü

        private void Awake()
        {
            // Eğer sahneye zaten kaydedilmişse tekrar oluşturma (edit modunda değilken)
            if (Application.isPlaying && transform.Find("RoadMarkings") != null)
            {
                return;
            }

            // Edit modunda veya sahne boşsa oluştur
            if (road == null)
            {
                GameObject roadGo = GameObject.Find("Road");
                if (roadGo != null) road = roadGo.transform;
            }

            if (road != null)
            {
                Renderer roadRenderer = road.GetComponent<Renderer>();
                if (roadRenderer != null)
                {
                    Bounds b = roadRenderer.bounds;
                    roadHalfWidth = b.size.x / 2f;
                    roadStartZ = b.min.z;
                    roadEndZ = b.max.z;
                }
            }

            // Eğer oyunu başlatıyorsak ve sahne boşsa runtime oluştur
            if (Application.isPlaying)
            {
                PlaceMarkings();
            }
        }

        private void CreateMaterials()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

            if (whiteMat == null)
            {
                whiteMat = new Material(unlitShader);
                whiteMat.color = Color.white;
                whiteMat.name = "WhiteLaneMat";
            }

            if (yellowMat == null)
            {
                yellowMat = new Material(unlitShader);
                yellowMat.color = new Color(1f, 0.85f, 0f); // parlak sarı
                yellowMat.name = "YellowLaneMat";
            }
        }

        [ContextMenu("Clear Road Markings")]
        public void ClearMarkings()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "RoadMarkings")
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        [ContextMenu("Generate Road Markings")]
        public void PlaceMarkings()
        {
            ClearMarkings();

            if (road == null)
            {
                GameObject roadGo = GameObject.Find("Road");
                if (roadGo != null) road = roadGo.transform;
            }

            if (road == null)
            {
                Debug.LogError("RoadMarkingPlacer: Road nesnesi bulunamadı!");
                return;
            }

            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null)
            {
                Bounds b = roadRenderer.bounds;
                roadHalfWidth = b.size.x / 2f;
                roadStartZ = b.min.z;
                roadEndZ = b.max.z;
            }
            else
            {
                roadHalfWidth = 5f;
                roadStartZ = -150f;
                roadEndZ = 150f;
            }

            CreateMaterials();

            // Ana parent
            GameObject parent = new GameObject("RoadMarkings");
            parent.transform.SetParent(transform, false);
            parent.transform.position = Vector3.zero;

            // 1. Kesikli sarı orta çizgi
            float z = roadStartZ;
            int dashIndex = 0;
            while (z < roadEndZ)
            {
                GameObject dash = CreateQuad($"CenterDash_{dashIndex}", parent.transform);
                dash.transform.position = new Vector3(0f, markingY, z + dashLength / 2f);
                dash.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                dash.transform.localScale = new Vector3(lineWidth, dashLength, 1f);
                dash.GetComponent<Renderer>().sharedMaterial = whiteMat; // Beyaz materyal atandı

                z += dashLength + gapLength;
                dashIndex++;
            }

            // 2. Sol kenar çizgisi (sürekli beyaz)
            float edgeOffset = roadHalfWidth - 0.3f; // kenardan biraz içeride
            CreateSolidLine("LeftEdgeLine", parent.transform, -edgeOffset, whiteMat);

            // 3. Sağ kenar çizgisi (sürekli beyaz) — kavşaklarda boşluk bırakılarak 3 parça halinde
            CreateSolidLineSegment("RightEdgeLine_Part1", parent.transform, edgeOffset, roadStartZ, -5f, whiteMat);
            CreateSolidLineSegment("RightEdgeLine_Part2", parent.transform, edgeOffset, 5f, 25f, whiteMat);
            CreateSolidLineSegment("RightEdgeLine_Part3", parent.transform, edgeOffset, 35f, roadEndZ, whiteMat);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        private void CreateSolidLine(string name, Transform parent, float xPos, Material mat)
        {
            float totalLength = roadEndZ - roadStartZ;
            float centerZ = (roadStartZ + roadEndZ) / 2f;

            GameObject line = CreateQuad(name, parent);
            line.transform.position = new Vector3(xPos, markingY, centerZ);
            line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            line.transform.localScale = new Vector3(edgeLineWidth, totalLength, 1f);
            line.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void CreateSolidLineSegment(string name, Transform parent, float xPos, float startZ, float endZ, Material mat)
        {
            float length = endZ - startZ;
            if (length <= 0f) return;
            float centerZ = (startZ + endZ) / 2f;

            GameObject line = CreateQuad(name, parent);
            line.transform.position = new Vector3(xPos, markingY, centerZ);
            line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            line.transform.localScale = new Vector3(edgeLineWidth, length, 1f);
            line.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private GameObject CreateQuad(string name, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);

            // Collider gereksiz, kaldır
            Collider col = quad.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            return quad;
        }
    }
}
