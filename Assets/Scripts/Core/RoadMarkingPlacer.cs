using UnityEngine;

namespace TrafikParkuru.Core
{
    /// <summary>
    /// Yol üzerine şerit işaretleri yerleştirir:
    /// - Ortada kesikli sarı çizgi (dashed center line)
    /// - İki kenarda sürekli beyaz çizgiler (solid edge lines)
    /// Awake'de çalışır, sahnedeki "Road" objesinin boyutlarına göre hesaplar.
    /// </summary>
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

            // Yol boyutlarını hesapla
            // Road scale (10, 0.1, 300) → gerçek boyut = Plane ise 10*10=100, Cube ise 1*10=10
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
                // fallback
                roadHalfWidth = 5f;
                roadStartZ = -150f;
                roadEndZ = 150f;
            }

            CreateMaterials();
            PlaceMarkings();
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

        private void PlaceMarkings()
        {
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
                dash.GetComponent<Renderer>().sharedMaterial = whiteMat;

                z += dashLength + gapLength;
                dashIndex++;
            }

            // 2. Sol kenar çizgisi (sürekli beyaz)
            float edgeOffset = roadHalfWidth - 0.3f; // kenardan biraz içeride
            CreateSolidLine("LeftEdgeLine", parent.transform, -edgeOffset, whiteMat);

            // 3. Sağ kenar çizgisi (sürekli beyaz)
            CreateSolidLine("RightEdgeLine", parent.transform, edgeOffset, whiteMat);
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

        private GameObject CreateQuad(string name, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);

            // Collider gereksiz, kaldır
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return quad;
        }
    }
}
