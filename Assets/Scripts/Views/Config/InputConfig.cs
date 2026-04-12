using UnityEngine;

namespace KlondikeSolitaire.Views
{
    [CreateAssetMenu(menuName = "Klondike/Input Config")]
    public sealed class InputConfig : ScriptableObject
    {
        [SerializeField] private float _dragStartThreshold = 10f;
        [SerializeField] private float _doubleTapWindow = 0.3f;
        [SerializeField] private float _tapMaxDuration = 0.2f;

        public float DragStartThreshold => _dragStartThreshold;
        public float DoubleTapWindow => _doubleTapWindow;
        public float TapMaxDuration => _tapMaxDuration;
    }
}
