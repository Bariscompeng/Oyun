using UnityEngine;

namespace TrafikParkuru.Pedestrians
{
    public class PedestrianModelRandomizer : MonoBehaviour
    {
        [Header("Karakter Prefabları")]
        [SerializeField] private GameObject[] characterPrefabs;

        [Header("Referanslar")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator parentAnimator;
        [SerializeField] private RuntimeAnimatorController humanoidAnimatorController;

        private void Awake()
        {
            if (characterPrefabs == null || characterPrefabs.Length == 0)
            {
                Debug.LogWarning("PedestrianModelRandomizer: Karakter prefabları atanmamış!");
                return;
            }

            if (parentAnimator == null)
            {
                parentAnimator = GetComponent<Animator>();
            }

            if (modelRoot == null)
            {
                modelRoot = transform.Find("ModelRoot");
                if (modelRoot == null)
                {
                    modelRoot = transform;
                }
            }

            // Mevcut varsayılan modelleri (örneğin MichelleModel) temizle
            foreach (Transform child in modelRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            // Rastgele bir prefab seç
            GameObject selectedPrefab = characterPrefabs[Random.Range(0, characterPrefabs.Length)];
            if (selectedPrefab != null)
            {
                GameObject instance = Instantiate(selectedPrefab, modelRoot);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                // Modelin kendi Animator'üne humanoid yürüme denetleyicisini aktar
                Animator childAnimator = instance.GetComponent<Animator>();
                if (childAnimator != null)
                {
                    childAnimator.runtimeAnimatorController = humanoidAnimatorController;
                }

                if (parentAnimator != null)
                {
                    // Ebeveynin Animator bileşenini hemen sil, böylece GetComponentInChildren doğrudan çocuğunkini bulur
                    DestroyImmediate(parentAnimator);
                }

                // Eğer pakete ait CityPeople scripti varsa, kendi yürüme kodlarımızla çakışmaması için kaldır
                MonoBehaviour cityPeopleScript = instance.GetComponent("CityPeople") as MonoBehaviour;
                if (cityPeopleScript != null)
                {
                    Destroy(cityPeopleScript);
                }
            }
        }
    }
}
