using UnityEngine;

namespace TrafikParkuru.Core
{
    /// <summary>
    /// NPC araçların belirli yolları (waypoint) takip ederek hareket etmesini
    /// ve önlerinde oyuncu veya başka bir araç olduğunda durmasını sağlar.
    /// Yerçekimi raycast ile sağlanır (kinematic rigidbody).
    /// </summary>
    public class NpcDriver : MonoBehaviour
    {
        [Header("Hareket Ayarları")]
        public Vector3[] waypoints;
        public float speed = 8f;
        public float rotationSpeed = 5f;
        public float arrivalDistance = 3f;

        [Header("Engelden Kaçınma")]
        public float detectionDistance = 12f;
        public float stopDistance = 6f;
        public LayerMask obstacleLayers;

        [Header("Yükseklik Ayarı")]
        public float groundOffset = 0.4f; // Araba gövdesinin yol yüzeyinden yüksekliği
        public float groundRayLength = 5f;

        private int currentWaypointIndex = 0;
        private float currentSpeed = 0f;
        private Transform playerTransform;
        private float targetGroundY;

        private void Start()
        {
            currentSpeed = speed;

            // Oyuncuyu bul
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Car");
            if (player != null)
            {
                playerTransform = player.transform;
            }

            // En yakın waypoint'i bul ve oradan başla
            if (waypoints != null && waypoints.Length > 0)
            {
                float minDistance = float.MaxValue;
                int closestIndex = 0;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    float dist = Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z),
                        new Vector3(waypoints[i].x, 0, waypoints[i].z));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestIndex = i;
                    }
                }
                currentWaypointIndex = closestIndex;
            }

            // Başlangıçta yere oturt
            SnapToGround();
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            // 1. Yol takibi (Waypoint)
            Vector3 targetPoint = waypoints[currentWaypointIndex];
            // Yüksekliği kendi yüksekliğimizle eşitleyelim ki havaya/yere yönelmesin
            targetPoint.y = transform.position.y;

            Vector3 direction = (targetPoint - transform.position);
            direction.y = 0f;
            float distanceToWaypoint = direction.magnitude;
            direction = direction.normalized;

            // Hedefe ulaşıldı mı?
            if (distanceToWaypoint < arrivalDistance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                targetPoint = waypoints[currentWaypointIndex];
                targetPoint.y = transform.position.y;
                direction = (targetPoint - transform.position);
                direction.y = 0f;
                direction = direction.normalized;
            }

            // 2. Engel Kontrolü
            bool shouldStop = false;
            float slowFactor = 1f;

            // Önümüzde başka bir araç/engel var mı diye Raycast ile kontrol et (birincil engel tespiti)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.3f + transform.forward * 1.0f;
                RaycastHit hit;

                // Merkez ışını
                if (Physics.Raycast(rayOrigin, transform.forward, out hit, detectionDistance))
                {
                    if (hit.collider.attachedRigidbody != null && !hit.collider.CompareTag("Road"))
                    {
                        float dist = hit.distance;
                        if (dist < stopDistance)
                        {
                            shouldStop = true;
                        }
                        else
                        {
                            // Yavaşla
                            slowFactor = Mathf.Clamp01((dist - stopDistance) / (detectionDistance - stopDistance));
                        }
                    }
                }
            }

            // Oyuncu tespiti — sadece dar açıda ve aynı şeritteyse (karşı şeriti etkilemesin)
            if (!shouldStop && playerTransform != null)
            {
                Vector3 toPlayer = playerTransform.position - transform.position;
                toPlayer.y = 0f;
                float distToPlayer = toPlayer.magnitude;

                if (distToPlayer < detectionDistance && distToPlayer > 0.1f)
                {
                    float angle = Vector3.Angle(transform.forward, toPlayer.normalized);

                    // Sadece 25° içinde ve aynı şerit genişliğinde ise dur
                    if (angle < 25f)
                    {
                        // Yanal uzaklık kontrolü (aynı şeritte mi?)
                        float lateralDist = Mathf.Abs(Vector3.Dot(toPlayer, transform.right));
                        if (lateralDist < 2.0f) // Şerit genişliği içinde
                        {
                            if (distToPlayer < stopDistance)
                            {
                                shouldStop = true;
                            }
                            else
                            {
                                slowFactor = Mathf.Min(slowFactor,
                                    Mathf.Clamp01((distToPlayer - stopDistance) / (detectionDistance - stopDistance)));
                            }
                        }
                    }
                }
            }

            // 3. Hız Ayarı
            float targetSpeed = shouldStop ? 0f : speed * slowFactor;
            // Yumuşak duruş ve kalkış
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speed * 2f * Time.deltaTime);

            // 4. Hareket ve Dönüş
            if (currentSpeed > 0.01f)
            {
                // İleri hareket et
                transform.position += transform.forward * currentSpeed * Time.deltaTime;

                // Hedefe doğru yumuşakça dön
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }

            // 5. Yere oturt (her frame)
            SnapToGround();
        }

        /// <summary>
        /// Aracı raycast ile yol yüzeyine oturtur.
        /// </summary>
        private void SnapToGround()
        {
            Vector3 rayStart = transform.position + Vector3.up * 2f;
            RaycastHit hit;

            // Aşağı doğru raycast — sadece yol ve zemin katmanlarına çarp
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundRayLength + 2f))
            {
                targetGroundY = hit.point.y + groundOffset;
            }

            // Y pozisyonunu yumuşak geçişle güncelle
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, targetGroundY, 10f * Time.deltaTime);
            transform.position = pos;

            // Aracın rotasyonunu sadece Y ekseninde tut (eğilme olmasın)
            Vector3 euler = transform.eulerAngles;
            euler.x = 0f;
            euler.z = 0f;
            transform.eulerAngles = euler;
        }

        // Editörde yolları çizelim
        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Gizmos.DrawSphere(waypoints[i], 0.5f);
                int next = (i + 1) % waypoints.Length;
                Gizmos.DrawLine(waypoints[i], waypoints[next]);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.3f + transform.forward * 1.0f, transform.forward * detectionDistance);
        }
    }
}
