using UnityEngine;

namespace KlondikeSolitaire.Views
{
    [CreateAssetMenu(menuName = "Klondike/Animation Config")]
    public sealed class AnimationConfig : ScriptableObject
    {
        [SerializeField] private float _moveDuration;
        [SerializeField] private float _flipDuration;
        [SerializeField] private float _dealDelay;
        [SerializeField] private float _snapBackDuration;
        [SerializeField] private float _shakeAmplitude;
        [SerializeField] private float _cascadeSpeed;
        [SerializeField] private float _autoCompleteDelay;

        public float MoveDuration => _moveDuration;
        public float FlipDuration => _flipDuration;
        public float DealDelay => _dealDelay;
        public float SnapBackDuration => _snapBackDuration;
        public float ShakeAmplitude => _shakeAmplitude;
        public float CascadeSpeed => _cascadeSpeed;
        public float AutoCompleteDelay => _autoCompleteDelay;
    }
}
