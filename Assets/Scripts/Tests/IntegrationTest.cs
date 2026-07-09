using System.Collections;
using UnityEngine;
using TrafikParkuru.Core;
using TrafikParkuru.Vehicle;
using TrafikParkuru.Stations;

namespace TrafikParkuru.Tests
{
    public class IntegrationTest : MonoBehaviour
    {
        private CarController car;
        private Rigidbody carRb;
        private TrafficLightController lightController;

        private void Start()
        {
            StartCoroutine(RunTest());
        }

        private IEnumerator RunTest()
        {
            Debug.Log("--- ENTEGRASYON TESTİ BAŞLATILDI ---");
            yield return new WaitForSeconds(1.0f);

            // Oyuncu ve Kontrolcüleri bul
            car = FindAnyObjectByType<CarController>();
            if (car == null)
            {
                Debug.LogError("Test Hatası: CarController sahne içerisinde bulunamadı!");
                yield break;
            }
            carRb = car.GetComponent<Rigidbody>();
            lightController = FindAnyObjectByType<TrafficLightController>();

            // CarController girdilerini devredışı bırakarak kendi hareketimizi kontrol edelim
            car.enabled = false;

            // --- 1. İSTASYON: KIRMIZI IŞIK ---
            Debug.Log("1. İstasyon: Kırmızı Işık bölgesine gidiliyor...");
            yield return StartCoroutine(MoveCarThrough(new Vector3(0f, 0.5f, -78f), 15f));
            yield return new WaitForSeconds(0.5f);

            Debug.Log("Işıkta bekleniyor (Kırmızı ışığın yeşile dönmesi bekleniyor)...");
            while (lightController != null && lightController.CurrentState != TrafficLightState.Green)
            {
                carRb.linearVelocity = Vector3.zero;
                carRb.angularVelocity = Vector3.zero;
                yield return null;
            }

            Debug.Log("Işık yeşil oldu! Stop çizgisinden geçiliyor...");
            yield return StartCoroutine(MoveCarThrough(new Vector3(0f, 0.5f, -90f), 15f));
            yield return new WaitForSeconds(0.5f);

            // --- 2. İSTASYON: YAYA GEÇİDİ ---
            Debug.Log("2. İstasyon: Yaya geçidi durma çizgisine gidiliyor...");
            // Yaya geçidi Z: -50'ye taşındığı için, durma çizgisi yaklaşık Z = -56f
            yield return StartCoroutine(MoveCarThrough(new Vector3(0f, 0.5f, -56f), 15f));
            
            // Yayanın karşıdan karşıya geçmesi ve bizim durarak yol vermemiz bekleniyor
            Debug.Log("Yayanın karşıdan karşıya geçmesi bekleniyor...");
            PedestrianWalker walker = null;
            float spawnWait = 0f;
            while (walker == null && spawnWait < 2f)
            {
                walker = FindAnyObjectByType<PedestrianWalker>();
                spawnWait += Time.deltaTime;
                yield return null;
            }

            if (walker != null)
            {
                while (walker != null && walker.IsOnCrosswalk)
                {
                    carRb.linearVelocity = Vector3.zero;
                    carRb.angularVelocity = Vector3.zero;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(5f);
            }

            Debug.Log("Yaya geçidi tamamlanıyor, bölgeden çıkılıyor...");
            // Yaya geçidi Z = -50'den geçilip Z = -38'e ilerleniyor
            yield return StartCoroutine(MoveCarThrough(new Vector3(0f, 0.5f, -38f), 15f));
            yield return new WaitForSeconds(0.5f);

            // --- 3. İSTASYON: SAĞA DÖNÜŞ SİNYALİ ---
            Debug.Log("3. İstasyon: Sağa dönüş sinyal bölgesine giriliyor...");
            yield return StartCoroutine(MoveCarThrough(new Vector3(0f, 0.5f, -15f), 15f));
            yield return new WaitForSeconds(0.1f);

            Debug.Log("Sağ sinyal açılıyor...");
            var signal = car.GetComponent<SignalController>();
            if (signal != null)
            {
                signal.SetSignal(SignalState.Right);
            }

            // En az 1 saniye sinyal verilmesi gerekiyor
            yield return new WaitForSeconds(1.5f);

            Debug.Log("Kavşak dönüş noktasına ilerleniyor...");
            // Kavşak merkezi Z = -1.8f konumundadır (SideRoad sağ şerit)
            yield return StartCoroutine(MoveCarThrough(new Vector3(0f, 0.5f, -1.8f), 10f));
            
            Debug.Log("Araç sağa (East) döndürülüyor...");
            car.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            carRb.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return new WaitForFixedUpdate();

            Debug.Log("Yan yola giriş yapılıyor...");
            yield return StartCoroutine(MoveCarThrough(new Vector3(10f, 0.5f, -1.8f), 10f));
            yield return new WaitForSeconds(0.1f);

            if (signal != null)
            {
                signal.SetSignal(SignalState.None);
            }
            yield return new WaitForSeconds(0.4f);

            // --- 4. İSTASYON: HIZ SINIRI (25 km/s) ---
            Debug.Log("4. İstasyon: Hız sınırı bölgesine yaklaşıyor...");
            // SpeedZone X = 15f ila 40f arasındadır. Girmeden önce yavaşlıyoruz.
            // X = 10f'ten X = 14f'e kadar 2.22 m/s (8 km/s) hızla giderek bölgeye yavaş giriyoruz.
            yield return StartCoroutine(MoveCarThrough(new Vector3(14f, 0.5f, -1.8f), 2.22f));
            
            // Hız sınırı bölgesi içinde (X = 15f'ten X = 40f'e) yavaşça sürülüyor...
            Debug.Log("Hız sınırı bölgesi içinde yavaşça sürülüyor...");
            yield return StartCoroutine(MoveCarThrough(new Vector3(40f, 0.5f, -1.8f), 2.22f));
            yield return new WaitForSeconds(0.5f);

            Debug.Log("Bitiş çizgisine ilerleniyor...");
            // FinishTrigger X = 50'dedir.
            yield return StartCoroutine(MoveCarThrough(new Vector3(51f, 0.5f, -1.8f), 15f));
            yield return new WaitForSeconds(1.0f);

            Debug.Log("--- ENTEGRASYON TESTİ TAMAMLANDI ---");
            LogFinalResults();
        }

        private IEnumerator MoveCarThrough(Vector3 targetPosition, float speed)
        {
            Vector3 startPos = car.transform.position;
            float distance = Vector3.Distance(startPos, targetPosition);
            float duration = distance / speed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 newPos = Vector3.Lerp(startPos, targetPosition, t);
                
                car.transform.position = newPos;
                carRb.position = newPos;
                
                // Keep physics engine updated with velocity vector
                carRb.linearVelocity = (targetPosition - startPos).normalized * speed;
                yield return new WaitForFixedUpdate();
            }

            car.transform.position = targetPosition;
            carRb.position = targetPosition;
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();
        }

        private void LogFinalResults()
        {
            if (ScenarioManager.Instance == null) return;

            int rlScore = ScenarioManager.Instance.GetStageScore(GameStage.RedLight);
            int cwScore = ScenarioManager.Instance.GetStageScore(GameStage.Crosswalk);
            int turnScore = ScenarioManager.Instance.GetStageScore(GameStage.Turn);
            int szScore = ScenarioManager.Instance.GetStageScore(GameStage.SpeedZone);
            int totalScore = ScenarioManager.Instance.GetTotalScore();

            Debug.Log("====== SINAV SKOR ÖZETİ ======");
            Debug.Log($"1. Kırmızı Işık: {rlScore} / 25 Puan ({ScenarioManager.Instance.GetStageNote(GameStage.RedLight)})");
            Debug.Log($"2. Yaya Geçidi: {cwScore} / 25 Puan ({ScenarioManager.Instance.GetStageNote(GameStage.Crosswalk)})");
            Debug.Log($"3. Sağa Dönüş: {turnScore} / 25 Puan ({ScenarioManager.Instance.GetStageNote(GameStage.Turn)})");
            Debug.Log($"4. Hız Sınırı: {szScore} / 25 Puan ({ScenarioManager.Instance.GetStageNote(GameStage.SpeedZone)})");
            Debug.Log($"Toplam Puan: {totalScore} / 100 Puan");
            Debug.Log($"Harf Notu: {ScenarioManager.Instance.GetLetterRating()}");
            Debug.Log($"Toplam Süre: {ScenarioManager.Instance.ElapsedTime:F1} saniye");
            Debug.Log("=============================");
        }
    }
}
