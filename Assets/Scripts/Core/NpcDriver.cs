using UnityEngine;

namespace TrafikParkuru.Core
{
    /// <summary>
    /// NPC araçların waypoint takibi, engel tespiti ve zemine kilitleme.
    /// BoxCollider sınırları kullanılarak prefab pivot/scale'den bağımsız şekilde
    /// araç alt kenarı yol yüzeyine (roadSurfaceY) oturur.
    /// </summary>
    public class NpcDriver : MonoBehaviour
    {
        [Header("Hareket Ayarları")]
        public Vector3[] waypoints;
        public float speed           = 7f;
        public float rotationSpeed   = 5f;
        public float arrivalDistance = 3f;

        [Header("Engelden Kaçınma")]
        public float detectionDistance = 12f;
        public float stopDistance      = 5f;

        [Header("Zemin Ayarı")]
        public float roadSurfaceY = 0.10f; // Yol yüzeyi world Y

        [Header("Döngü")]
        // true → son waypoint'e varınca ilk waypoint'e TELEPORT (düz yol şeritleri için).
        // false → normal döngü (kapalı devre için).
        public bool loopTeleport = true;

        private int       currentWaypointIndex = 0;
        private float     currentSpeed         = 0f;
        private Transform playerTransform;
        private float     lockedY;      // Pivot'un sabit tutulacağı Y değeri
        private BoxCollider boxCol;

        private void Start()
        {
            currentSpeed = speed;
            boxCol       = GetComponent<BoxCollider>();

            // Oyuncuyu bul
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Car");
            if (player != null) playerTransform = player.transform;

            // En yakın waypoint
            FindClosestWaypoint();

            // lockedY = collider alt kenarını yol yüzeyine getiren pivot Y
            lockedY = ComputeLockedY();
            SnapY();
        }

        /// <summary>
        /// BoxCollider'ın dünya uzayı alt kenarını roadSurfaceY'ye eşitlemek için
        /// gereken pivot Y değerini hesaplar. Prefab scale/pivot'undan bağımsızdır.
        /// </summary>
        private float ComputeLockedY()
        {
            if (boxCol == null) return roadSurfaceY + 0.4f;

            // Collider merkezi ve yarı yüksekliği — dünya uzayında
            float worldCenterY = transform.TransformPoint(boxCol.center).y;
            float worldHalfH   = boxCol.size.y * Mathf.Abs(transform.lossyScale.y) * 0.5f;
            float colBottom    = worldCenterY - worldHalfH;

            // Mevcut alt kenar ile istenen (roadSurfaceY) arasındaki fark
            float delta = roadSurfaceY - colBottom;
            return transform.position.y + delta;
        }

        private void FindClosestWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            float minDist = float.MaxValue;
            int   closest = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                float d = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(waypoints[i].x,       waypoints[i].z));
                if (d < minDist) { minDist = d; closest = i; }
            }
            currentWaypointIndex = closest;
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            // 1. Hedef waypoint (Y yoksayılır)
            Vector3 target   = waypoints[currentWaypointIndex];
            Vector3 toTarget = new Vector3(target.x - transform.position.x, 0f,
                                           target.z - transform.position.z);
            if (toTarget.magnitude < arrivalDistance)
            {
                int nextIdx = (currentWaypointIndex + 1) % waypoints.Length;

                if (loopTeleport && nextIdx == 0)
                {
                    // Son waypoint'e ulaşıldı — başlangıca TELEPORT
                    // (Geri dönmek yerine şırınlanarak aynı yönde devam eder)
                    Vector3 tp = transform.position;
                    tp.x = waypoints[0].x;
                    tp.z = waypoints[0].z;
                    transform.position = tp;
                    currentWaypointIndex = 0;
                }
                else
                {
                    currentWaypointIndex = nextIdx;
                }

                target   = waypoints[currentWaypointIndex];
                toTarget = new Vector3(target.x - transform.position.x, 0f,
                                       target.z - transform.position.z);
            }
            Vector3 dir = toTarget.normalized;

            // 2. Engel tespiti
            bool  shouldStop = false;
            float slowFactor = 1f;

            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, detectionDistance))
            {
                if (hit.collider.attachedRigidbody != null)
                {
                    if (hit.distance < stopDistance) shouldStop = true;
                    else slowFactor = Mathf.Clamp01(
                        (hit.distance - stopDistance) / (detectionDistance - stopDistance));
                }
            }

            // Oyuncu tespiti
            if (!shouldStop && playerTransform != null)
            {
                Vector3 toP = playerTransform.position - transform.position;
                toP.y = 0f;
                float dP = toP.magnitude;
                if (dP < detectionDistance && dP > 0.1f)
                {
                    float angle   = Vector3.Angle(transform.forward, toP.normalized);
                    float lateral = Mathf.Abs(Vector3.Dot(toP, transform.right));
                    if (angle < 25f && lateral < 2.0f)
                    {
                        if (dP < stopDistance) shouldStop = true;
                        else slowFactor = Mathf.Min(slowFactor,
                            Mathf.Clamp01((dP - stopDistance) / (detectionDistance - stopDistance)));
                    }
                }
            }

            // 3. Hız
            float targetSpd = shouldStop ? 0f : speed * slowFactor;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpd, speed * 2f * Time.deltaTime);

            // 4. Hareket + Dönüş
            if (currentSpeed > 0.01f)
            {
                transform.position += transform.forward * currentSpeed * Time.deltaTime;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir, Vector3.up), rotationSpeed * Time.deltaTime);
            }

            // 5. Her frame yola kilitle
            SnapY();
        }

        private void SnapY()
        {
            Vector3 pos = transform.position;
            pos.y = lockedY;
            transform.position = pos;

            Vector3 e = transform.eulerAngles;
            e.x = 0f; e.z = 0f;
            transform.eulerAngles = e;
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Gizmos.DrawSphere(waypoints[i], 0.3f);
                Gizmos.DrawLine(waypoints[i], waypoints[(i + 1) % waypoints.Length]);
            }
        }
    }
}
