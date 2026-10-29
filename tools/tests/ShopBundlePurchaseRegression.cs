using System;
using System.Collections.Generic;
using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;
using UnityEngine;

namespace PenguinRun.Tests
{
    public static class ShopBundlePurchaseRegression
    {
        private static readonly List<string> Failures = new();
        public static int CaseCount { get; private set; }

        private sealed class Bundle
        {
            public readonly string Id, Scarf;
            public readonly ShopItemKind Kind;
            public readonly int Price, Fish, DoubleFish, Score, Lucky;
            public Bundle(string id, ShopItemKind kind, int price, int fish, int doubleFish, int score, int lucky, string scarf = null)
            { Id = id; Kind = kind; Price = price; Fish = fish; DoubleFish = doubleFish; Score = score; Lucky = lucky; Scarf = scarf; }
            public ShopItemDefinition Item => ShopCatalog.Find(Id);
        }

        // Independent expectations describe the current shipped promises, not Dispense internals.
        private static readonly Bundle[] Bundles =
        {
            new("bundle_starter", ShopItemKind.BundleStarter, 380, 200, 1, 1, 0),
            new("bundle_polar", ShopItemKind.BundlePolar, 980, 600, 2, 2, 0, "scarf_aurora"),
            new("bundle_aurora", ShopItemKind.BundleAurora, 1980, 1500, 3, 3, 3, "scarf_pearl"),
        };

        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); }

        private static void Equal<T>(T actual, T expected, string message)
        { Check(EqualityComparer<T>.Default.Equals(actual, expected), $"{message}: expected {expected}, got {actual}"); }

        private static void Case(string name, Action test)
        {
            CaseCount++;
            PlayerPrefs.Reset();
            try { test(); }
            catch (Exception e) { Failures.Add(name + ": " + e.GetBaseException().Message); }
        }

        private static void Buy(Bundle bundle)
        {
            Check(ShopStore.TryPurchase(bundle.Item, bundle.Item.BasePrice, out var reason), "Purchase failed: " + reason);
            Equal(reason, null, "Successful purchase must not have a failure reason");
        }

        private static void Rewards(Bundle b, int wallet, int doubleFish = 0, int score = 0, int lucky = 0)
        {
            Equal(PlayerSave.TotalFishSnacks, wallet - b.Price + b.Fish, "Wallet debit plus bundle fish");
            Equal(PlayerSave.BoosterDoubleFish, doubleFish + b.DoubleFish, "Double-fish vouchers");
            Equal(PlayerSave.BoosterScoreBoost, score + b.Score, "Score vouchers");
            Equal(PlayerSave.BoosterLuckyStart, lucky + b.Lucky, "Lucky-start vouchers");
            Check(PlayerSave.IsBundleClaimed(b.Id) && ShopStore.IsOwned(b.Item), "Bundle ownership not recorded");
            if (b.Scarf != null) Check(PlayerSave.IsScarfUnlocked(b.Scarf), "Promised scarf is not unlocked");
        }

        private static void NoWrites(Action action)
        {
            var before = PlayerPrefs.Snapshot();
            var writes = PlayerPrefs.Writes;
            var saves = PlayerPrefs.Saves;
            action();
            Equal(PlayerPrefs.Writes, writes, "Rejected/read-only operation wrote storage");
            Equal(PlayerPrefs.Saves, saves, "Rejected/read-only operation flushed storage");
            var after = PlayerPrefs.Snapshot();
            Equal(after.Count, before.Count, "Storage key count changed");
            foreach (var pair in before)
                Check(after.TryGetValue(pair.Key, out var value) && Equals(pair.Value, value), "Storage changed: " + pair.Key);
        }

        public static string[] Run()
        {
            Failures.Clear(); CaseCount = 0;
            try
            {
                foreach (var bundle in Bundles)
                {
                    var b = bundle;
                    Case(b.Id + " retains its catalog identity and cost", () =>
                    {
                        Check(b.Item != null, "Catalog entry missing");
                        Equal(b.Item.Kind, b.Kind, "Bundle kind");
                        Equal(b.Item.Category, ShopCategory.Bundles, "Bundle category");
                        Equal(b.Item.BasePrice, b.Price, "Catalog price");
                        Equal(ShopStore.ResolvePrice(b.Item), (int?)b.Price, "Displayed price");
                    });
                    Case(b.Id + " delivers promised rewards and flushes once", () =>
                    {
                        PlayerSave.AddFishSnacks(10000);
                        var saves = PlayerPrefs.Saves;
                        Buy(b); Rewards(b, 10000);
                        Equal(PlayerPrefs.Saves, saves + 1, "Purchase flush count");
                        Equal(PlayerSave.SelectedScarfId, PlayerSave.DefaultScarfId, "Bundle unexpectedly auto-equipped a scarf");
                        Equal(PlayerSave.UnlockedScarves.Count, b.Scarf == null ? 1 : 2, "Unrelated scarf unlocked");
                        Equal(PlayerSave.UnlockedHats.Count, 1, "Unrelated hat unlocked");
                    });
                    Case(b.Id + " adds to existing inventory without consuming pending vouchers", () =>
                    {
                        PlayerSave.AddFishSnacks(10000);
                        PlayerSave.BoosterDoubleFish = 4; PlayerSave.BoosterScoreBoost = 5; PlayerSave.BoosterLuckyStart = 6;
                        PlayerSave.PendingDoubleFish = 1; PlayerSave.PendingScoreBoost = 1; PlayerSave.PendingLuckyStart = 1;
                        Buy(b); Rewards(b, 10000, 4, 5, 6);
                        Equal(PlayerSave.PendingDoubleFish, 1, "Pending double fish changed");
                        Equal(PlayerSave.PendingScoreBoost, 1, "Pending score changed");
                        Equal(PlayerSave.PendingLuckyStart, 1, "Pending lucky start changed");
                    });
                    Case(b.Id + " cannot be purchased twice", () =>
                    {
                        PlayerSave.AddFishSnacks(10000); Buy(b);
                        NoWrites(() =>
                        {
                            Check(!ShopStore.TryPurchase(b.Item, b.Price, out var reason), "Repeated purchase succeeded");
                            Check(!string.IsNullOrWhiteSpace(reason), "Repeated purchase has no explanation");
                        });
                        Rewards(b, 10000);
                    });
                    Case(b.Id + " ownership and rewards survive storage reload", () =>
                    {
                        PlayerSave.AddFishSnacks(10000); Buy(b); PlayerPrefs.Reload();
                        Rewards(b, 10000);
                        NoWrites(() => Check(!ShopStore.TryPurchase(b.Item, b.Price, out _), "Repeated purchase succeeded after reload"));
                    });
                    Case(b.Id + " insufficient funds cannot borrow its cashback", () =>
                    {
                        PlayerSave.AddFishSnacks(b.Price - 1);
                        NoWrites(() => Check(!ShopStore.TryPurchase(b.Item, b.Price, out _), "Underfunded purchase succeeded"));
                        Equal(PlayerSave.TotalFishSnacks, b.Price - 1, "Failed purchase changed balance");
                        Check(!ShopStore.IsOwned(b.Item), "Failed purchase claimed the bundle");
                    });
                    Case(b.Id + " accepts the exact price", () =>
                    {
                        PlayerSave.AddFishSnacks(b.Price); Buy(b); Rewards(b, b.Price);
                    });
                    Case(b.Id + " does not claim the other bundles", () =>
                    {
                        PlayerSave.AddFishSnacks(10000); Buy(b);
                        foreach (var other in Bundles)
                            Equal(ShopStore.IsOwned(other.Item), other.Id == b.Id, "Cross-bundle ownership " + other.Id);
                    });
                    Case(b.Id + " rejects zero and negative prices without changing storage", () =>
                    {
                        PlayerSave.AddFishSnacks(10000);
                        NoWrites(() =>
                        {
                            Check(!ShopStore.TryPurchase(b.Item, 0, out _), "Zero-price purchase succeeded");
                            Check(!ShopStore.TryPurchase(b.Item, -1, out _), "Negative-price purchase succeeded");
                        });
                    });
                    if (b.Scarf != null)
                        Case(b.Id + " still grants the bundle when its scarf is already owned", () =>
                        {
                            PlayerSave.AddFishSnacks(10000); PlayerSave.UnlockScarf(b.Scarf);
                            PlayerSave.UnlockScarf("scarf_red"); PlayerSave.SelectedScarfId = "scarf_red";
                            Buy(b); Rewards(b, 10000);
                            Equal(PlayerSave.UnlockedScarves.Count, 3, "Duplicate scarf entry");
                            Equal(PlayerSave.SelectedScarfId, "scarf_red", "Bundle changed equipped scarf");
                        });
                }
                Case("Polar description explicitly names only the two granted voucher types", () =>
                {
                    var text = ShopCatalog.Find("bundle_polar").Description;
                    Check(text.Contains("双倍鱼干券 ×2") && text.Contains("得分加成券 ×2"), "Polar text does not identify both voucher rewards");
                    Check(!text.Contains("各类券") && !text.Contains("幸运"), "Polar text implies a lucky-start reward it does not grant");
                    Check(text.Contains(CosmeticPalette.FindScarf("scarf_aurora").Title), "Polar text promises the wrong scarf");
                });
                Case("Aurora description matches all voucher types and pearl scarf", () =>
                {
                    var text = ShopCatalog.Find("bundle_aurora").Description;
                    Check(text.Contains("全套消耗券 ×3"), "Aurora does not promise all three voucher types");
                    Check(text.Contains(CosmeticPalette.FindScarf("scarf_pearl").Title), "Aurora text promises the wrong scarf");
                });
                Case("invalid item is rejected without storage writes", () =>
                    NoWrites(() => Check(!ShopStore.TryPurchase(null, 380, out _), "Null item accepted")));
                Case("bundle catalog and ownership queries are read only", () =>
                    NoWrites(() =>
                    {
                        foreach (var b in Bundles)
                        { ShopStore.IsOwned(b.Item); ShopStore.ResolvePrice(b.Item); ShopCatalog.Find(b.Id); }
                    }));
                return Failures.ToArray();
            }
            finally { PlayerPrefs.Reset(); }
        }
    }
}
