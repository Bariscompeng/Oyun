using UnityEngine;

namespace TrafikParkuru.Stations
{
    [RequireComponent(typeof(Collider))]
    public class PedestrianWalker : MonoBehaviour
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float speed = 1.5f;

        [Header("Görsel Eksen Düzeltmesi")]
        [Tooltip("Eğer yaya yerde yatıyorsa bu ekseni (örneğin X: -90, 0 veya 90) ayarlayabilirsiniz.")]
        [SerializeField] private Vector3 visualRotationOffset = Vector3.zero; 

        private Vector3 startPosition;
        private bool isWalking = false;
        private bool hitByCar = false;
        private CrosswalkStation station;

        public bool IsOnCrosswalk
        {
            get
            {
                if (hitByCar) return false;
                
                if (station != null && station.UseXAxis)
                {
                    return transform.position.z > 25.5f && transform.position.z < 34.5f;
                }
                
                return transform.position.x > -5.5f && transform.position.x < 5.5f;
            }
        }

        private Animator animator;

        private void Start()
        {
            startPosition = transform.position;
            animator = GetComponentInChildren<Animator>();

            // Görsel modelin eksenini düzelt (İlk child genelde modelin kendisidir)
            if (transform.childCount > 0)
            {
                Transform visualModel = transform.GetChild(0);
                visualModel.localRotation = Quaternion.Euler(visualRotationOffset);
            }
        }

        public void Initialize(Vector3 start, Vector3 target, float walkSpeed, CrosswalkStation crosswalkStation)
        {
            transform.position = start;
            startPosition = start;
            targetPosition = target;
            speed = walkSpeed;
            station = crosswalkStation;
            isWalking = true;
            hitByCar = false;

            // Yüzünü hedefe dön
            Vector3 direction = (targetPosition - start).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void Update()
        {
            if (!isWalking || hitByCar) return;

            // hedefe dogru duz yürü
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (animator != null)
            {
                animator.speed = speed / 2.2f;
            }

            // Sürekli hedef yöne bak (arabaya bakmayı önle)
            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }

            // Hedefe ulasti mi?
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isWalking = false;
                if (animator != null) animator.speed = 0f;
                Destroy(gameObject, 1f); // 1 saniye sonra yok et
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hitByCar) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                hitByCar = true;
                isWalking = false;
                if (animator != null) animator.speed = 0f;
                Debug.LogWarning("PedestrianWalker: Yaya araba tarafından ezildi!");

                // Fiziksel etki ekle (fırla)
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.AddForce((transform.position - collision.transform.position).normalized * 10f + Vector3.up * 5f, ForceMode.Impulse);
                }

                // Istasyona rapor et
                if (station != null)
                {
                    station.OnPedestrianHit();
                }
            }
        }
    }
}
