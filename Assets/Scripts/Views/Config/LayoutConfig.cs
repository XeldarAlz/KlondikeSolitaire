using UnityEngine;

namespace KlondikeSolitaire.Views
{
    [CreateAssetMenu(menuName = "Klondike/Layout Config")]
    public sealed class LayoutConfig : ScriptableObject
    {
        [SerializeField] private Vector2 _cardSize;
        [SerializeField] private float _faceDownYOffset;
        [SerializeField] private float _faceUpYOffset;

        public Vector2 CardSize => _cardSize;
        public float FaceDownYOffset => _faceDownYOffset;
        public float FaceUpYOffset => _faceUpYOffset;
    }
}
