using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Polyjam2023
{
    public class PlayerHandPanel : MonoBehaviour
    {
        private PresentationManager presentationManager;
        private FloatingText floatingTextPrefab;
        private CardLibrary cardLibrary;
        private GameplayManager gameplayManager;
        private CardWidget cardWidgetPrefab;
        [SerializeField] private RectTransform cardWidgetsContainer;
        private List<CardWidget> cardWidgets = new();

        private void Awake()
        {
            var dependencyResolver = FindObjectOfType<DependencyResolver>();
            presentationManager = dependencyResolver.PresentationManager;
            floatingTextPrefab = dependencyResolver.FloatingTextPrefab;
            cardLibrary = dependencyResolver.CardLibrary;
            gameplayManager = dependencyResolver.GameplayManager;
            cardWidgetPrefab = dependencyResolver.CardWidgetPrefab;
            
            Assert.IsNotNull(presentationManager, $"Missing {nameof(presentationManager)} on {gameObject.name}.");
            Assert.IsNotNull(floatingTextPrefab, $"Missing {nameof(floatingTextPrefab)} on {gameObject.name}.");
            Assert.IsNotNull(cardLibrary, $"Missing {nameof(cardLibrary)} on {gameObject.name}.");
            Assert.IsNotNull(gameplayManager, $"Missing {nameof(gameplayManager)} on {gameObject.name}.");
            Assert.IsNotNull(cardWidgetPrefab, $"Missing {nameof(cardWidgetPrefab)} on {gameObject.name}.");
            Assert.IsNotNull(cardWidgetsContainer, $"Missing {nameof(cardWidgetsContainer)} on {gameObject.name}.");

            int childIndex = 0;
            for (; childIndex < cardWidgetsContainer.childCount;)
            {
                var cardWidget = cardWidgetsContainer.GetChild(0).GetComponent<CardWidget>();
                if (cardWidget == null)
                {
                    DestroyImmediate(cardWidgetsContainer.GetChild(0).gameObject);
                }
                else
                {
                    cardWidgets.Add(cardWidget);
                    ++childIndex;
                }
            }
            
            while (cardWidgets.Count > gameplayManager.GameState.PlayerHand.Cards.Count)
            {
                Destroy(cardWidgets[0].gameObject);
                cardWidgets.RemoveAt(0);
            }
            
            while (cardWidgets.Count < gameplayManager.GameState.PlayerHand.Cards.Count)
            {
                var newCardWidget = Instantiate(cardWidgetPrefab, cardWidgetsContainer);
                cardWidgets.Add(newCardWidget);
            }

            int i = 0;
            foreach (var cardEntry in gameplayManager.GameState.PlayerHand.Cards)
            {
                cardWidgets[i].SetPresentationData(gameplayManager.GameState.PlayerHand, cardLibrary.GetCardTemplate(cardEntry.name), cardEntry.quantity);
                ++i;
            }
            
            gameplayManager.GameState.PlayerHand.OnCardAdded += OnCardAdded;
            gameplayManager.GameState.PlayerHand.OnCardRemoved += OnCardRemoved;
        }

        private void OnDestroy()
        {
            gameplayManager.GameState.PlayerHand.OnCardAdded -= OnCardAdded;
            gameplayManager.GameState.PlayerHand.OnCardRemoved -= OnCardRemoved;
            presentationManager = null;
            floatingTextPrefab = null;
            cardLibrary = null;
            gameplayManager = null;
            cardWidgetPrefab = null;
            cardWidgetsContainer = null;
            cardWidgets.Clear();
        }

        private void OnCardAdded((string name, int quantity) entry)
        {
            var newFloatingText = Instantiate(floatingTextPrefab);
            newFloatingText.SetText("Card added");
            newFloatingText.gameObject.SetActive(false);
            presentationManager.AddPresentationTask(new PresentationTask
            (() =>
                {
                    CardWidget cardWidget = null;
                    if (entry.quantity == 1)
                    {
                        cardWidget = Instantiate(cardWidgetPrefab, cardWidgetsContainer);
                        cardWidgets.Add(cardWidget);
                    }
                    else
                    {
                        cardWidget = cardWidgets.FirstOrDefault(widget => widget.CardName == entry.name);
                    }
                    cardWidget.SetPresentationData(gameplayManager.GameState.PlayerHand, cardLibrary.GetCardTemplate(entry.name), entry.quantity);
                    
                    cardWidgets.ForEach(cardWidget => cardWidget.transform.SetParent(null));
                    cardWidgets = cardWidgets.OrderBy(widget => widget.CardName).ToList();
                    cardWidgets.ForEach(cardWidget => cardWidget.transform.SetParent(cardWidgetsContainer));
                    
                    newFloatingText.transform.SetParent(cardWidget.transform);
                    newFloatingText.transform.position = cardWidget.transform.position;
                    newFloatingText.gameObject.SetActive(true);
                },
                (float deltaTime) => { },
                () => { },
                () => newFloatingText == null
            ));
        }
        
        private void OnCardRemoved((string name, int quantity) entry)
        {
            var newFloatingText = Instantiate(floatingTextPrefab);
            newFloatingText.SetText("Card removed");
            newFloatingText.gameObject.SetActive(false);
            presentationManager.AddPresentationTask(new PresentationTask
            (() =>
                {
                    var cardWidget = cardWidgets.FirstOrDefault(widget => widget.CardName == entry.name);
                    if (entry.quantity == 0)
                    {
                        cardWidgets.Remove(cardWidget);
                        newFloatingText.transform.SetParent(cardWidgetsContainer.transform);
                        newFloatingText.transform.position = cardWidget.transform.position;
                        Destroy(cardWidget.gameObject);
                    }
                    else
                    {
                        cardWidget.SetPresentationData(gameplayManager.GameState.PlayerHand, cardLibrary.GetCardTemplate(entry.name), entry.quantity);
                        newFloatingText.transform.SetParent(cardWidget.transform);
                        newFloatingText.transform.position = cardWidget.transform.position;
                    }
                    newFloatingText.gameObject.SetActive(true);
                },
                (float deltaTime) => { },
                () => { },
                () => newFloatingText == null
            ));
        }
    }
}