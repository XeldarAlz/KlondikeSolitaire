using System;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Systems;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace KlondikeSolitaire.Views
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private LayoutConfig _layoutConfig;
        [SerializeField] private AnimationConfig _animationConfig;
        [SerializeField] private ScoringConfig _scoringConfig;
        [SerializeField] private InputConfig _inputConfig;
        [SerializeField] private CardSpriteMapping _cardSpriteMapping;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BoardModel>(Lifetime.Singleton);
            builder.Register<ScoreModel>(Lifetime.Singleton);
            builder.Register<GamePhaseModel>(Lifetime.Singleton);
            builder.Register<DealModel>(Lifetime.Singleton);
            builder.RegisterInstance(new System.Random());

            builder.RegisterInstance(_layoutConfig);
            builder.RegisterInstance(_animationConfig);
            builder.RegisterInstance(_inputConfig);
            builder.RegisterInstance(_cardSpriteMapping);
            builder.RegisterInstance(_scoringConfig.ToScoringTable());

            builder.Register<CardAnimator>(Lifetime.Singleton);

            builder.Register<DealSystem>(Lifetime.Singleton);
            builder.Register<MoveValidationSystem>(Lifetime.Singleton);
            builder.Register<MoveEnumerator>(Lifetime.Singleton);
            builder.Register<ScoringSystem>(Lifetime.Singleton);

            builder.Register<MoveExecutionSystem>(Lifetime.Singleton);
            builder.Register<UndoSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HintSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<AutoCompleteSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<NoMovesDetectionSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<GameFlowSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            var options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<CardMovedMessage>(options);
            builder.RegisterMessageBroker<CardFlippedMessage>(options);
            builder.RegisterMessageBroker<ScoreChangedMessage>(options);
            builder.RegisterMessageBroker<GamePhaseChangedMessage>(options);
            builder.RegisterMessageBroker<AutoCompleteAvailableMessage>(options);
            builder.RegisterMessageBroker<UndoAvailabilityChangedMessage>(options);
            builder.RegisterMessageBroker<HintHighlightMessage>(options);
            builder.RegisterMessageBroker<HintClearedMessage>(options);
            builder.RegisterMessageBroker<BoardStateChangedMessage>(options);
            builder.RegisterMessageBroker<NoMovesDetectedMessage>(options);
            builder.RegisterMessageBroker<WinDetectedMessage>(options);
            builder.RegisterMessageBroker<NewGameRequestedMessage>(options);
            builder.RegisterMessageBroker<DealCompletedMessage>(options);
            builder.RegisterMessageBroker<DealAnimationCompletedMessage>(options);
            builder.RegisterMessageBroker<UndoRequestedMessage>(options);
            builder.RegisterMessageBroker<HintRequestedMessage>(options);
            builder.RegisterMessageBroker<AutoCompleteRequestedMessage>(options);

            builder.RegisterComponentInHierarchy<BoardView>();
            builder.RegisterComponentInHierarchy<HintView>();
            builder.RegisterComponentInHierarchy<InputView>();
            builder.RegisterComponentInHierarchy<DragView>();
            builder.RegisterComponentInHierarchy<HudView>();
            builder.RegisterComponentInHierarchy<OverlayView>();
            builder.RegisterComponentInHierarchy<WinCascadeView>();

            builder.RegisterEntryPoint<GameBootstrap>();
        }
    }
}
