using KlondikeSolitaire.Core;
using UnityEngine;

namespace KlondikeSolitaire.Views
{
    [CreateAssetMenu(menuName = "Klondike/Scoring Config")]
    public sealed class ScoringConfig : ScriptableObject
    {
        [SerializeField] private int _wasteToTableau = 5;
        [SerializeField] private int _wasteToFoundation = 10;
        [SerializeField] private int _tableauToFoundation = 10;
        [SerializeField] private int _foundationToTableau = -15;
        [SerializeField] private int _flipCard = 5;

        public int WasteToTableau => _wasteToTableau;
        public int WasteToFoundation => _wasteToFoundation;
        public int TableauToFoundation => _tableauToFoundation;
        public int FoundationToTableau => _foundationToTableau;
        public int FlipCard => _flipCard;

        public ScoringTable ToScoringTable()
        {
            return new ScoringTable(
                wasteToTableau: _wasteToTableau,
                wasteToFoundation: _wasteToFoundation,
                tableauToFoundation: _tableauToFoundation,
                foundationToTableau: _foundationToTableau,
                flipCard: _flipCard);
        }
    }
}
