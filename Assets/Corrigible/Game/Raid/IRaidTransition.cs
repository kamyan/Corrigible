using System.Collections;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Baskına giriş/çıkış geçişi (Docs/UI_Siginak Aşama 7, Soru 5: yalnız ekleme kancası). <see cref="RaidLauncher"/> kamerayı
    /// monitöre yaklaştırır; kanca varsa yaklaşmanın süresini verir, ardından sahne yüklenmeden önce kendi dizisini oynatır
    /// (beklenir), sahne açılınca ve kapanınca haber alır. Kanca yoksa (eski görünüm) akış değişmez.
    /// </summary>
    public interface IRaidTransition
    {
        /// <summary>Kamera yaklaşmasının süresi (s); 0 = başlatıcının kendi süresi.</summary>
        float ApproachSeconds { get; }

        /// <summary>Yaklaşma başlıyor (görüntü son kısımda tam ekran katmana devredilir).</summary>
        void BeginApproach();

        /// <summary>Yaklaşmadan sonra, sahne yüklenmeden önce: geçiş dizisi (atlanabilir). Bitene kadar yükleme bekler.</summary>
        IEnumerator Run();

        /// <summary>Baskın sahnesi açıldı: geçiş katmanı söner.</summary>
        void SceneLoaded();

        /// <summary>Baskın sahnesi kapandı, masa ve kamera geri geldi: dönüş dizisi (bekletmez).</summary>
        IEnumerator Return();
    }
}
