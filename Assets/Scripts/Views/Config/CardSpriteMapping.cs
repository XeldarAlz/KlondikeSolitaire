using KlondikeSolitaire.Core;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    [CreateAssetMenu(menuName = "Klondike/Card Sprite Mapping")]
    public sealed class CardSpriteMapping : ScriptableObject
    {
        [SerializeField] private Sprite[] _faceSprites;
        [SerializeField] private Sprite[] _faceStripSprites;
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private Sprite _backStripSprite;
        [SerializeField] private Sprite _baseSprite;

        public Sprite BackSprite => _backSprite;
        public Sprite BackStripSprite => _backStripSprite;
        public Sprite BaseSprite => _baseSprite;

        public Sprite GetFaceSprite(Suit suit, Rank rank)
        {
            int index = (int)suit * BoardModel.RANK_COUNT + ((int)rank - 1);
            return _faceSprites[index];
        }

        public Sprite GetFaceStripSprite(Suit suit, Rank rank)
        {
            int index = (int)suit * BoardModel.RANK_COUNT + ((int)rank - 1);
            return _faceStripSprites[index];
        }
    }
}
