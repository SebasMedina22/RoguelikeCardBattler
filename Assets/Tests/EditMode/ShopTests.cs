using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.Shop;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode de la Tienda (Sub-PR 3D). Validan los helpers static puros
    /// (TryPurchase, BuildStock), RunState.RemoveCardFromDeck y la inyección de
    /// ítems vía OnShopStockBuilt. Patrón: RunState directo sin TurnManager
    /// (todos los hooks de la Tienda corren fuera de combate).
    /// </summary>
    public class ShopTests
    {
        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────

        private static CardDefinition MakeCard(string id, string name, CardRarity rarity, ElementType element)
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, name, "", 1,
                CardType.Attack, rarity, CardTarget.SingleEnemy,
                new List<string>(), new List<EffectRef>(), element);
            return card;
        }

        private static CardDeckEntry MakeEntry(string id, string name, CardRarity rarity, ElementType element)
        {
            return CardDeckEntry.CreateSingle(MakeCard(id, name, rarity, element));
        }

        private static RelicDefinition MakeRelic(string name)
        {
            RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
            def.DisplayName = name;
            def.Hooks = new RelicHook[0];
            return def;
        }

        private static ShopConfig MakeConfig(
            List<CardDeckEntry> cards,
            List<RelicDefinition> relics,
            int cardSlots = 3,
            int relicSlots = 2)
        {
            ShopConfig config = ScriptableObject.CreateInstance<ShopConfig>();
            config.SetDebugData(
                cards, relics,
                cardSlots, relicSlots,
                common: 50, uncommon: 75, rare: 100,
                relicPriceValue: 75,
                removeBase: 75, removeStep: 5);
            return config;
        }

        private static ShopItem CardItem(int price, CardDeckEntry payload) =>
            new ShopItem(ShopItemKind.Card, "Carta", "", price, cardPayload: payload);

        // ────────────────────────────────────────
        // TryPurchase — oro
        // ────────────────────────────────────────

        [Test]
        public void TryPurchase_Card_DeductsExactGold()
        {
            RunState rs = new RunState { Gold = 100 };
            ShopItem item = CardItem(50, MakeEntry("c1", "Carta", CardRarity.Common, ElementType.None));

            Assert.IsTrue(ShopNodeController.TryPurchase(rs, item));
            Assert.AreEqual(50, rs.Gold);
            Assert.IsTrue(item.Purchased);
        }

        [Test]
        public void TryPurchase_InsufficientGold_NoChange()
        {
            RunState rs = new RunState { Gold = 30 };
            ShopItem item = CardItem(50, MakeEntry("c1", "Carta", CardRarity.Common, ElementType.None));

            Assert.IsFalse(ShopNodeController.TryPurchase(rs, item));
            Assert.AreEqual(30, rs.Gold);
            Assert.AreEqual(0, rs.Deck.Count);
            Assert.IsFalse(item.Purchased);
        }

        [Test]
        public void TryPurchase_AlreadyPurchased_ReturnsFalse()
        {
            RunState rs = new RunState { Gold = 100 };
            ShopItem item = CardItem(50, MakeEntry("c1", "Carta", CardRarity.Common, ElementType.None));

            Assert.IsTrue(ShopNodeController.TryPurchase(rs, item));
            Assert.IsFalse(ShopNodeController.TryPurchase(rs, item), "Re-comprar el mismo ítem es no-op.");
            Assert.AreEqual(50, rs.Gold, "El segundo intento no debe descontar oro.");
        }

        // ────────────────────────────────────────
        // TryPurchase — carta / Retazo
        // ────────────────────────────────────────

        [Test]
        public void TryPurchase_Card_AddsCloneToDeck()
        {
            RunState rs = new RunState { Gold = 100 };
            CardDeckEntry payload = MakeEntry("c1", "Carta", CardRarity.Common, ElementType.None);
            ShopItem item = CardItem(50, payload);

            ShopNodeController.TryPurchase(rs, item);

            Assert.AreEqual(1, rs.Deck.Count);
            Assert.AreNotSame(payload, rs.Deck[0], "El mazo debe recibir un CLON, no la referencia del payload.");
        }

        [Test]
        public void TryPurchase_Relic_AddsWithAcquisitionOrder()
        {
            RunState rs = new RunState { Gold = 100 };
            rs.AddRelic(MakeRelic("Previo")); // order 0

            RelicDefinition bought = MakeRelic("Comprado");
            ShopItem item = new ShopItem(ShopItemKind.Relic, "Comprado", "", 75, relicPayload: bought);

            Assert.IsTrue(ShopNodeController.TryPurchase(rs, item));
            Assert.AreEqual(2, rs.Relics.Count);
            Assert.AreSame(bought, rs.Relics[1].Definition);
            Assert.AreEqual(1, rs.Relics[1].AcquisitionOrder);
            Assert.AreEqual(25, rs.Gold);
        }

        // ────────────────────────────────────────
        // RemoveCardFromDeck + servicio eliminar
        // ────────────────────────────────────────

        [Test]
        public void RemoveCardFromDeck_RemovesPresent_ReturnsFalseIfMissing()
        {
            RunState rs = new RunState();
            CardDeckEntry inDeck = MakeEntry("c1", "Carta", CardRarity.Common, ElementType.None);
            rs.Deck.Add(inDeck);
            CardDeckEntry notInDeck = MakeEntry("c2", "Otra", CardRarity.Common, ElementType.None);

            Assert.IsFalse(rs.RemoveCardFromDeck(notInDeck));
            Assert.IsTrue(rs.RemoveCardFromDeck(inDeck));
            Assert.AreEqual(0, rs.Deck.Count);
        }

        [Test]
        public void TryPurchase_RemoveCard_ReducesDeckAndDeductsGold()
        {
            RunState rs = new RunState { Gold = 100 };
            CardDeckEntry target = MakeEntry("c1", "Carta", CardRarity.Common, ElementType.None);
            rs.Deck.Add(target);
            ShopItem item = new ShopItem(ShopItemKind.RemoveCard, "Eliminar carta", "", 75);

            Assert.IsTrue(ShopNodeController.TryPurchase(rs, item, target));
            Assert.AreEqual(0, rs.Deck.Count);
            Assert.AreEqual(25, rs.Gold);
            Assert.IsTrue(item.Purchased);
        }

        [Test]
        public void TryPurchase_RemoveCard_NoTarget_ReturnsFalse()
        {
            RunState rs = new RunState { Gold = 100 };
            ShopItem item = new ShopItem(ShopItemKind.RemoveCard, "Eliminar carta", "", 75);

            Assert.IsFalse(ShopNodeController.TryPurchase(rs, item, null));
            Assert.AreEqual(100, rs.Gold);
        }

        [Test]
        public void TryPurchase_RemoveCard_TargetNotInDeck_ReturnsFalse()
        {
            // Contraparte de NoTarget: el target existe pero no está en el mazo.
            // RemoveCardFromDeck devuelve false ANTES de descontar oro → no compra.
            RunState rs = new RunState { Gold = 100 };
            CardDeckEntry inDeck = MakeEntry("c1", "Carta", CardRarity.Common, ElementType.None);
            rs.Deck.Add(inDeck);
            CardDeckEntry notInDeck = MakeEntry("c2", "Otra", CardRarity.Common, ElementType.None);
            ShopItem item = new ShopItem(ShopItemKind.RemoveCard, "Eliminar carta", "", 75);

            Assert.IsFalse(ShopNodeController.TryPurchase(rs, item, notInDeck));
            Assert.AreEqual(100, rs.Gold);
            Assert.AreEqual(1, rs.Deck.Count, "El mazo no debe cambiar si el target no estaba.");
            Assert.IsFalse(item.Purchased);
        }

        // ────────────────────────────────────────
        // BuildStock
        // ────────────────────────────────────────

        [Test]
        public void BuildStock_DeterministicBySeed()
        {
            List<CardDeckEntry> cards = new List<CardDeckEntry>
            {
                MakeEntry("a", "A", CardRarity.Common, ElementType.None),
                MakeEntry("b", "B", CardRarity.Common, ElementType.None),
                MakeEntry("c", "C", CardRarity.Common, ElementType.None),
                MakeEntry("d", "D", CardRarity.Common, ElementType.None),
                MakeEntry("e", "E", CardRarity.Common, ElementType.None),
            };
            ShopConfig config = MakeConfig(cards, new List<RelicDefinition>(), cardSlots: 3, relicSlots: 0);
            RunState rs = new RunState();

            List<ShopItem> first = ShopNodeController.BuildStock(config, rs, seed: 42);
            List<ShopItem> second = ShopNodeController.BuildStock(config, rs, seed: 42);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Title, second[i].Title, $"Item {i} debe ser idéntico con la misma seed.");
            }
        }

        [Test]
        public void BuildStock_FiltersCardsByType()
        {
            List<CardDeckEntry> cards = new List<CardDeckEntry>
            {
                MakeEntry("rojo", "Rojo", CardRarity.Common, ElementType.Rojo),
                MakeEntry("azul", "Azul", CardRarity.Common, ElementType.Azul),     // disallowed
                MakeEntry("none", "Neutra", CardRarity.Common, ElementType.None),
                MakeEntry("amar", "Amarilla", CardRarity.Common, ElementType.Amarillo),
            };
            ShopConfig config = MakeConfig(cards, new List<RelicDefinition>(), cardSlots: 10, relicSlots: 0);
            RunState rs = new RunState
            {
                PlayerWorldAType = ElementType.Rojo,
                PlayerWorldBType = ElementType.Amarillo
            };

            List<ShopItem> stock = ShopNodeController.BuildStock(config, rs, seed: 1);

            List<string> cardTitles = new List<string>();
            foreach (ShopItem it in stock)
            {
                if (it.Kind == ShopItemKind.Card) cardTitles.Add(it.Title);
            }

            Assert.Contains("Rojo", cardTitles);
            Assert.Contains("Neutra", cardTitles);
            Assert.Contains("Amarilla", cardTitles);
            Assert.IsFalse(cardTitles.Contains("Azul"), "La carta Azul (tipo no permitido) no debe ofrecerse.");
        }

        [Test]
        public void BuildStock_ExcludesOwnedRelics()
        {
            RelicDefinition owned = MakeRelic("Poseído");
            RelicDefinition fresh = MakeRelic("Nuevo");
            ShopConfig config = MakeConfig(
                new List<CardDeckEntry>(),
                new List<RelicDefinition> { owned, fresh },
                cardSlots: 0, relicSlots: 10);
            RunState rs = new RunState();
            rs.AddRelic(owned);

            List<ShopItem> stock = ShopNodeController.BuildStock(config, rs, seed: 7);

            List<string> relicTitles = new List<string>();
            foreach (ShopItem it in stock)
            {
                if (it.Kind == ShopItemKind.Relic) relicTitles.Add(it.Title);
            }

            Assert.Contains("Nuevo", relicTitles);
            Assert.IsFalse(relicTitles.Contains("Poseído"), "Un Retazo ya poseído no debe ofrecerse.");
        }

        [Test]
        public void BuildStock_IncludesScalingRemoveCardService()
        {
            ShopConfig config = MakeConfig(new List<CardDeckEntry>(), new List<RelicDefinition>(), 0, 0);

            RunState firstShop = new RunState { ShopsCompleted = 0 };
            ShopItem remove1 = FindRemove(ShopNodeController.BuildStock(config, firstShop, 1));
            Assert.IsNotNull(remove1, "Siempre debe existir el servicio eliminar carta.");
            Assert.AreEqual(75, remove1.Price); // base

            RunState thirdShop = new RunState { ShopsCompleted = 2 };
            ShopItem remove3 = FindRemove(ShopNodeController.BuildStock(config, thirdShop, 1));
            Assert.AreEqual(85, remove3.Price); // 75 + 5*2
        }

        private static ShopItem FindRemove(List<ShopItem> stock)
        {
            foreach (ShopItem it in stock)
            {
                if (it.Kind == ShopItemKind.RemoveCard) return it;
            }
            return null;
        }

        // ────────────────────────────────────────
        // Hook OnShopStockBuilt
        // ────────────────────────────────────────

        private class AddItemEffect : IRelicEffect
        {
            public void OnHook(RelicHook hook, RelicHookContext ctx)
            {
                if (hook != RelicHook.OnShopStockBuilt) return;
                if (ctx is ShopStockBuiltHookData data)
                {
                    data.Stock.Add(new ShopItem(ShopItemKind.Card, "Regalo", "Gratis del Retazo", 0));
                }
            }
        }

        [Test]
        public void ShopHook_RelicCanAddItem()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);

            RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
            def.DisplayName = "Mercader";
            def.Effect = new AddItemEffect();
            def.Hooks = new[] { RelicHook.OnShopStockBuilt };
            rs.Relics.Add(new RelicInstance(def, 0));

            List<ShopItem> stock = new List<ShopItem>
            {
                new ShopItem(ShopItemKind.Card, "Base", "", 50)
            };
            disp.Dispatch(RelicHook.OnShopStockBuilt, new ShopStockBuiltHookData(rs, disp, stock));

            Assert.AreEqual(2, stock.Count);
            Assert.AreEqual("Regalo", stock[1].Title);
        }
    }
}
