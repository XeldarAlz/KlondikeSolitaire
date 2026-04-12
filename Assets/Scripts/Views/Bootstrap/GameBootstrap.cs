using KlondikeSolitaire.Systems;
using VContainer.Unity;

namespace KlondikeSolitaire.Views
{
    public sealed class GameBootstrap : IPostStartable
    {
        private readonly GameFlowSystem _gameFlow;

        public GameBootstrap(GameFlowSystem gameFlow)
        {
            _gameFlow = gameFlow;
        }

        public void PostStart()
        {
            _gameFlow.StartNewGame();
        }
    }
}
