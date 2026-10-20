# F3 ölçüm ayrımı

Kabul: son kaynak sürümüyle `mono_final` altındaki altı 100×5 kohort; son özet yalnız bu kaynak hash’ini alır. Unity doğrulaması `unity_verification` altındadır. `final/control` eski kaynak kohortlarını da içerir; hash filtresi onları dışlar.

`pressure_*`, `commitment_probe`, `focus_*` klasörleri yalnız tanıdır. Bu deneylerde çalışma ağacı değiştirilmeden geçici saf DLL varyantları derlendi. Bu yüzden ham rapordaki CodeVersion, **temel çalışma ağacının** kimliğidir; geçici DLL değişikliğinin kimliği değildir. Bu raporlar kaynak kimliği eşitmiş gibi kabul paydasına katılmaz. Geçici kod farkları `tani_kaynaklari` altında saklanır.

`e2_full/e2_holdout` ve `holdout`, mevcut DLL ile açık Balance seçeneklerinin ölçümüdür. Son varsayılanla 3.000 oyun ayrıca yeniden koşuldu; bunlar ana kabul paydasına eklenmez. `baseline_20261020` ilk F3 tesliminin tarihsel sonucudur. Kısmi iptal edilen işler kabul değildir.
