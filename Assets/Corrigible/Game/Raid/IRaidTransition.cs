using System.Collections;
using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Baskına giriş/çıkış geçişi (Docs/UI_Siginak Aşama 7, Soru 5: yalnız ekleme kancası). <see cref="RaidLauncher"/> kamerayı
    /// monitöre yaklaştırır; kanca varsa yaklaşmanın süresini verir ve yolu kendisi sürebilir, ardından sahne yüklenmeden
    /// önce kendi dizisini oynatır (beklenir), sahne açılınca ve kapanınca haber alır. Kanca yoksa (eski görünüm) akış değişmez.
    /// </summary>
    public interface IRaidTransition
    {
        /// <summary>Kamera yaklaşmasının süresi (s); 0 = başlatıcının kendi süresi.</summary>
        float ApproachSeconds { get; }

        /// <summary>Yaklaşma başlıyor (görüntü son kısımda tam ekran katmana devredilir).</summary>
        void BeginApproach();

        /// <summary>
        /// Kamerayı monitöre kanca götürür (sığınak: geri çekil → CRT'ye dön → yaklaş); bitince kamera <paramref name="target"/>
        /// pozunda ve <paramref name="fieldOfView"/> açısındadır. null: başlatıcının düz yaklaşması.
        /// </summary>
        IEnumerator Approach(Transform target, float fieldOfView);

        /// <summary>Yaklaşmadan sonra, sahne yüklenmeden önce: geçiş dizisi (atlanabilir). Bitene kadar yükleme bekler.</summary>
        IEnumerator Run();

        /// <summary>Baskın sahnesi açıldı: geçiş katmanı söner.</summary>
        void SceneLoaded();

        /// <summary>
        /// Baskın sahnesi kapandı, kamera hâlâ monitörde (<paramref name="from"/>): kanca onu yaklaşmanın tersi yoldan
        /// başlangıç pozuna götürür; bitene kadar girdi kilitli kalır (<see cref="RaidLauncher.IsTransitioning"/>), sonra
        /// başlatıcı kamerayı ve kilitleri geri verir. null: kamera hemen yerine döner.
        /// </summary>
        IEnumerator Depart(Transform from, float fieldOfView);

        /// <summary>Baskın sahnesi kapandı, masa geri geldi: dönüş dizisi (bekletmez; <see cref="Depart"/> ile aynı anda başlar).</summary>
        IEnumerator Return();
    }
}
