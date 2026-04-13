# Klondike Solitaire

Klasik Klondike Solitaire kart oyunu. Projenin tamamı [Helm](https://github.com/XeldarAlz/helm) kullanılarak AI destekli otomasyon ile üretilmiştir.

Proje **9 fazda**, **45 görev** ve **335 test** ile tamamlandı — tüm testler başarılı.

## Performans

| Metrik | Değer |
|--------|-------|
| FPS | + ~1000 |
| Draw Call | 5 |
| Batch | 5 (40 saved by batching) |
| CPU (main) | ~0.9 ms |
| Scripts | ~0.131 ms |
| Rendering | ~0.040 ms |
| Texture | 5 / ~9.1 MB |
 
## Optimizasyon

- **Dynamic batching** — 42 sprite çizimi 2 batch'e düşürüldü
- **Strip sprite** — üst üste binen kartlar kırpılmış sprite kullanır, overdraw minimum
- **Gizli renderer** — yığınlarda sadece en üst kart renderlanır
- **Sprite atlas** — tek atlas, tüm kartlar aynı batch'te
- **Object pool** — 52 CardView başta yaratılır, runtime'da instantiate yok

## Mimari

```
Core/       → Saf C# (model, enum, mesaj) — Unity bağımlılığı yok
Systems/    → Oyun mantığı (deal, move, undo, hint, scoring)
Views/      → Unity katmanı (board, input, animasyon, UI)
```

- **VContainer** ile DI — her sistem sadece ihtiyacı olan bağımlılıkları alır
- **MessagePipe** ile pub/sub — sistemler arası iletişim loosely-coupled
- **Assembly definition** ile katmanlar birbirinden izole

## Dokümantasyon

Proje dokümantasyonu `docs/` klasöründe yer alır:

| Döküman | Açıklama |
|---------|----------|
| [GDD.md](docs/GDD.md) | Game Design Document — oyun tasarım kararları, mekanikler ve kurallar |
| [TDD.md](docs/TDD.md) | Technical Design Document — teknik mimari, sistem tasarımları ve bağımlılıklar |
| [WORKFLOW.md](docs/WORKFLOW.md) | Fazlar ve görevlerle yürütme planı |
| [PROGRESS.md](docs/PROGRESS.md) | Orkestratör ilerleme takibi |

## Testler

335 test, %100 başarı oranı:
- **EditMode (299):** Tüm core sistemler — deal, move, undo, hint, scoring, auto-complete, validation, enumeration, no-moves, game flow
- **PlayMode (36):** Entegrasyon testleri

## Tech

Unity 6 · URP · VContainer · MessagePipe · UniTask · PrimeTween · Input System
