# Revizyon 3 · eksik portrelerin tarifleri (Aşama 3, Karar D8)

Paketin üreticisi (`threejs/r3/portraits.js`, `portraitSVG`) aynı parametrelerle; 300 × 340 viewBox, 847 × 960 PNG, 8 px
mürekkep çerçeve (paketteki on portreyle aynı biçim). Çıktı `Assets/Corrigible/Desk/Art/Rev3/Textures/portraits/p_<ad>.png`
(paketteki on portre de oraya kopyalandı). Portre adı izler: aynı adın sonekli kopyası (ör. "Suna 12") aynı portreyi kullanır;
robot olup olmamak portreyi değiştirmez.

Yeniden üretmek: bu tabloyu `PORTRAITS` biçiminde bir sayfaya koy, `portraitSVG(spec, { id })` ile çiz, tuvale 8 px çerçeveyle
yaz (uygulamadaki tarayıcı ve küçük bir yerel sunucu yeterli; Node gerekmez).

| ad | yapı | cinsiyet | yaş | yüz | saç | ton | kaş | ağız | yüz kılı | gözlük | yaka | çizgi |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| derya | cevik | f | 33 | oval | long | dark | arched | full | — | — | jacket | — |
| orhan | dayanikli | m | 58 | square | receding | grey | heavy | thin | beard | — | overall | 2 |
| meral | dengeli | f | 50 | round | bun | grey | soft | thin | — | round | sweater | — |
| arda | cevik | m | 24 | oval | crop | dark | straight | full | — | — | shirt | — |
| ece | cevik | f | 22 | heart | ponytail | light | arched | full | — | — | sweater | — |
| murat | guclu | m | 44 | square | sidepart | dark | heavy | thin | mustache | — | jacket | — |
| baran | guclu | m | 30 | long | crop | dark | heavy | thin | beard | — | overall | — |
| banu | dengeli | f | 38 | square | bob | dark | straight | thin | — | — | shirt | — |
| ozan | cevik | m | 27 | oval | curly | dark | soft | full | — | round | sweater | — |
| lale | dayanikli | f | 61 | round | scarf | grey | soft | thin | — | — | sweater | 2 |
| sinan | dengeli | m | 39 | long | sidepart | dark | straight | thin | — | — | uniform | — |
| asli | cevik | f | 26 | oval | long | dark | soft | full | — | — | jacket | — |
| kerem | dengeli | m | 35 | round | crop | light | straight | full | mustache | — | shirt | — |
| sema | guclu | f | 47 | long | bun | dark | arched | thin | — | — | uniform | — |
| tamer | dayanikli | m | 53 | round | cap | grey | heavy | thin | mustache | — | jacket | — |
| dilan | dengeli | f | 29 | heart | bob | dark | arched | full | — | — | overall | — |
| yalcin | dayanikli | m | 62 | square | receding | grey | heavy | thin | — | round | sweater | 2 |
| feride | guclu | f | 55 | oval | scarf | dark | soft | thin | — | — | shirt | — |
| umut | cevik | m | 19 | oval | curly | light | soft | full | — | — | sweater | — |
| ayca | guclu | f | 34 | round | ponytail | dark | straight | full | — | — | uniform | — |
| levent | guclu | m | 48 | square | sidepart | grey | straight | thin | beard | — | jacket | — |
| naz | cevik | f | 21 | heart | long | light | arched | full | — | — | shirt | — |
