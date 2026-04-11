using UnityEngine;

namespace KlondikeSolitaire.Views
{
    [CreateAssetMenu(menuName = "Klondike/Input Config")]
    public sealed class InputConfig : ScriptableObject
    {
        [SerializeField] private float _dragStartThreshold = 10f;
        [SerializeField] private float _doubleTapWindow = 300f;
        [SerializeField] private float _tapMaxDuration = 200f;

        public float DragStartThreshold => _dragStartThreshold;
        public float DoubleTapWindow => _doubleTapWindow;
        public float TapMaxDuration => _tapMaxDuration;
    }
}
