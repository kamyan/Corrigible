using TMPro;
using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>Baskın sahnesinin 3D parçaları, vurgu malzemeleri ve renkleri. Tahta (<see cref="RaidBoard3D"/>) buradan üretir.</summary>
    [CreateAssetMenu(menuName = "Corrigible/Raid Piece Library", fileName = "RaidPieceLibrary")]
    public sealed class RaidPieceLibrary : ScriptableObject
    {
        [Header("Zemin")]
        public GameObject floor;
        public GameObject wall;
        public GameObject cable;

        [Header("Dolaplar")]
        public RaidPiece core;
        public RaidPiece node;
        public RaidPiece civilian;
        public RaidPiece cooling;
        public RaidPiece spawner;
        [Tooltip("Kırılan dolabın enkazı.")]
        public GameObject rubble;

        [Header("Birimler")]
        public RaidPiece technician;
        public RaidPiece emp;
        public RaidPiece analyst;
        public RaidPiece drone;
        public RaidPiece maintenance;
        [Header("Bölge imza düşmanları")]
        public RaidPiece armoredDrone;
        public RaidPiece mortar;
        public RaidPiece bomber;
        public RaidPiece locker;
        public RaidPiece shieldDrone;
        public RaidPiece replicator;

        [Header("İşaretler")]
        [Tooltip("Robotun uyanacağı kare.")]
        public GameObject spawnMarker;
        [Tooltip("Karo vurgusu (derinlik testli, saydam).")]
        public Material highlight;
        [Tooltip("Oklar ve çerçeveler (her zaman üstte).")]
        public Material overlay;
        public TMP_FontAsset font;
        [Tooltip("Dünya içindeki yazılar (koyu konturlu).")]
        public Material labelMaterial;

        [Header("Renkler")]
        public Color move = new Color(0.36f, 0.78f, 0.85f, 0.42f);
        public Color target = new Color(0.94f, 0.69f, 0.25f, 1f);
        public Color danger = new Color(1f, 0.36f, 0.3f, 0.5f);
        public Color heal = new Color(0.45f, 0.85f, 0.5f, 0.4f);
        public Color hover = new Color(1f, 1f, 1f, 0.9f);
        public Color threat = new Color(1f, 0.36f, 0.3f, 1f);
        public Color good = new Color(0.45f, 0.85f, 0.5f, 1f);
        public Color ink = new Color(0.9f, 0.95f, 0.96f, 1f);

        public RaidPiece Object(string kind) => kind switch
        {
            "core" => core, "node" => node, "civ" => civilian, "cool" => cooling, "spawner" => spawner, _ => civilian,
        };

        // Unity nesnesinde ?? sahte null'u görmez
        static RaidPiece Or(RaidPiece piece, RaidPiece fallback) => piece != null ? piece : fallback;

        public RaidPiece Unit(string type) => type switch
        {
            "tek" => technician, "emp" => emp, "ana" => analyst, "dron" => drone, "bakim" => maintenance,
            // Yeni parça henüz atanmadıysa ailesinin parçası (havada olanlar dron, yerdekiler bakım robotu)
            "zdron" => Or(armoredDrone, drone), "kalkan" => Or(shieldDrone, drone), "havan" => Or(mortar, maintenance),
            "bomba" => Or(bomber, maintenance), "kilit" => Or(locker, maintenance), "cogalt" => Or(replicator, maintenance),
            _ => drone,
        };
    }
}
