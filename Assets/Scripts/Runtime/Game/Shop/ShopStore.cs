using System;
using System.Collections.Generic;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Game.Shop
{
    /// <summary>
    /// 商店购买与库存逻辑：实际扣鱼干、发放奖励、记录购买状态。
    /// 装扮的解锁与装备状态、消耗道具的库存、待消耗的本局加成全部走 <see cref="PlayerSave"/>。
    /// </summary>
    public static class ShopStore
    {
        /// <summary>每日特惠：每天选取 3 个条目，统一打 7 折，凌晨刷新。</summary>
        private const float DailyDealDiscount = 0.7f;
        private const int DailyDealCount = 3;

        public sealed class DailyDeal
        {
            public ShopItemDefinition Item;
            public int OriginalPrice;
            public int DiscountedPrice;
            public int DiscountPercent => Mathf.Max(1, Mathf.RoundToInt((1f - DiscountedPrice / (float)Mathf.Max(1, OriginalPrice)) * 100f));
        }

        /// <summary>距离次日本地 0 点的毫秒数，用于商店每日刷新。</summary>
        public static long MillisUntilNextRefresh()
        {
            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1);
            var span = tomorrow - now;
            return span.TotalMilliseconds > 0 ? (long)span.TotalMilliseconds : 0L;
        }

        /// <summary>按当天日期生成稳定的「每日特惠」清单。</summary>
        public static List<DailyDeal> GetDailyDeals()
        {
            var pool = new List<ShopItemDefinition>();
            foreach (var d in ShopCatalog.All)
            {
                // 礼盒已经一次性，限时不再促销；装扮也不放进每日轮换（避免与装扮 Tab 重复语义）
                if (d.Category == ShopCategory.Bundles) continue;
                if (d.Category == ShopCategory.Cosmetics) continue;
                // 鱼干包仅在限时特惠出现
                pool.Add(d);
            }

            // 用「年-序数日」做稳定哈希，跨日刷新
            var dayOfYear = DateTime.Now.DayOfYear;
            var year = DateTime.Now.Year;
            var seed = unchecked(dayOfYear * 9176 + year * 31);
            var picked = StablePick(pool, seed, Mathf.Min(DailyDealCount, pool.Count));

            var deals = new List<DailyDeal>(picked.Count);
            foreach (var def in picked)
            {
                var basePrice = def.BasePrice <= 0 ? 200 : def.BasePrice;
                var price = Mathf.Max(5, Mathf.RoundToInt(basePrice * DailyDealDiscount / 5f) * 5);
                deals.Add(new DailyDeal
                {
                    Item = def,
                    OriginalPrice = basePrice,
                    DiscountedPrice = price,
                });
            }
            return deals;
        }

        /// <summary>查询条目当前是否「已购入/已解锁」（用于装扮、礼盒）。</summary>
        public static bool IsOwned(ShopItemDefinition def)
        {
            if (def == null) return false;
            return def.Kind switch
            {
                ShopItemKind.CosmeticScarf  => PlayerSave.IsScarfUnlocked(def.Param),
                ShopItemKind.CosmeticHat    => PlayerSave.IsHatUnlocked(def.Param),
                ShopItemKind.BundleStarter
                    or ShopItemKind.BundlePolar
                    or ShopItemKind.BundleAurora => PlayerSave.IsBundleClaimed(def.Id),
                _ => false,
            };
        }

        /// <summary>条目是否已被装备（仅装扮有意义）。</summary>
        public static bool IsEquipped(ShopItemDefinition def)
        {
            if (def == null) return false;
            return def.Kind switch
            {
                ShopItemKind.CosmeticScarf => PlayerSave.SelectedScarfId == def.Param,
                ShopItemKind.CosmeticHat   => PlayerSave.SelectedHatId == def.Param,
                _ => false,
            };
        }

        /// <summary>该条目当前显示价格（含每日特惠折扣）。返回 null 表示「不可标价」（如已拥有的装扮）。</summary>
        public static int? ResolvePrice(ShopItemDefinition def, DailyDeal deal = null)
        {
            if (def == null) return null;
            if (deal != null && deal.Item != null && deal.Item.Id == def.Id) return deal.DiscountedPrice;
            return def.BasePrice > 0 ? def.BasePrice : (int?)null;
        }

        /// <summary>装扮：切换为指定外观（必须先解锁）。</summary>
        public static bool TryEquip(ShopItemDefinition def)
        {
            if (def == null) return false;
            if (def.Kind == ShopItemKind.CosmeticScarf)
            {
                if (!PlayerSave.IsScarfUnlocked(def.Param)) return false;
                PlayerSave.SelectedScarfId = def.Param;
                PlayerSave.Flush();
                return true;
            }
            if (def.Kind == ShopItemKind.CosmeticHat)
            {
                if (!PlayerSave.IsHatUnlocked(def.Param)) return false;
                PlayerSave.SelectedHatId = def.Param;
                PlayerSave.Flush();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 执行购买流程：扣鱼干 + 发放奖励。返回 true 表示购买成功；通过 <paramref name="reason"/>
        /// 输出失败原因，调用方可直接 Toast 提示。
        /// </summary>
        public static bool TryPurchase(ShopItemDefinition def, int price, out string reason)
        {
            reason = null;
            if (def == null) { reason = "条目不存在"; return false; }
            if (price <= 0) { reason = "该条目暂未上架"; return false; }

            // 礼盒只能购买一次
            if (IsBundle(def.Kind) && PlayerSave.IsBundleClaimed(def.Id))
            {
                reason = "该礼盒只能购买一次";
                return false;
            }
            // 已拥有的装扮无需再次购买
            if (def.Kind == ShopItemKind.CosmeticScarf && PlayerSave.IsScarfUnlocked(def.Param))
            {
                reason = "已解锁该围巾";
                return false;
            }
            if (def.Kind == ShopItemKind.CosmeticHat && PlayerSave.IsHatUnlocked(def.Param))
            {
                reason = "已解锁该帽子";
                return false;
            }

            if (PlayerSave.TotalFishSnacks < price)
            {
                reason = "鱼干不足";
                return false;
            }
            if (!PlayerSave.TrySpendFishSnacks(price))
            {
                reason = "扣款失败";
                return false;
            }

            Dispense(def);
            PlayerSave.Flush();
            return true;
        }

        public static int InventoryCount(ShopItemKind kind) =>
            kind switch
            {
                ShopItemKind.BoosterDoubleFish => PlayerSave.BoosterDoubleFish,
                ShopItemKind.BoosterScoreBoost => PlayerSave.BoosterScoreBoost,
                ShopItemKind.BoosterLuckyStart => PlayerSave.BoosterLuckyStart,
                _ => 0,
            };

        /// <summary>开局准备：从库存中抽取一张消耗券并写入"待结算"通道。</summary>
        public static void ConsumeOneOfEachForRunStart()
        {
            RecoverPendingBoostersToInventory();

            // 自动从库存抽出每种 1 张作为"本局可用"。无库存的种类不影响。
            if (PlayerSave.BoosterDoubleFish > 0)
            {
                PlayerSave.BoosterDoubleFish -= 1;
                PlayerSave.PendingDoubleFish = Mathf.Max(PlayerSave.PendingDoubleFish, 1);
            }
            if (PlayerSave.BoosterScoreBoost > 0)
            {
                PlayerSave.BoosterScoreBoost -= 1;
                PlayerSave.PendingScoreBoost = Mathf.Max(PlayerSave.PendingScoreBoost, 1);
            }
            if (PlayerSave.BoosterLuckyStart > 0)
            {
                PlayerSave.BoosterLuckyStart -= 1;
                PlayerSave.PendingLuckyStart = Mathf.Max(PlayerSave.PendingLuckyStart, 1);
            }
            PlayerSave.Flush();
        }

        /// <summary>
        /// 兜底修正：若上局异常中断（闪退/强退）导致 pending 遗留，先归还库存再开启新局，
        /// 避免多次点击开局时重复扣券。
        /// </summary>
        private static void RecoverPendingBoostersToInventory()
        {
            if (PlayerSave.PendingDoubleFish > 0)
            {
                PlayerSave.BoosterDoubleFish += PlayerSave.PendingDoubleFish;
                PlayerSave.PendingDoubleFish = 0;
            }
            if (PlayerSave.PendingScoreBoost > 0)
            {
                PlayerSave.BoosterScoreBoost += PlayerSave.PendingScoreBoost;
                PlayerSave.PendingScoreBoost = 0;
            }
            if (PlayerSave.PendingLuckyStart > 0)
            {
                PlayerSave.BoosterLuckyStart += PlayerSave.PendingLuckyStart;
                PlayerSave.PendingLuckyStart = 0;
            }
        }

        /// <summary>结算时由 <see cref="RunOutcomeRouter"/> 调用：清空本局待消耗道具。</summary>
        public static (float fishMul, float scoreMul) ConsumePendingForOutcome()
        {
            var fishMul = 1f;
            var scoreMul = 1f;
            if (PlayerSave.PendingDoubleFish > 0)
            {
                fishMul *= 2f;
                PlayerSave.PendingDoubleFish = 0;
            }
            if (PlayerSave.PendingScoreBoost > 0)
            {
                scoreMul *= 1.5f;
                PlayerSave.PendingScoreBoost = 0;
            }
            // LuckyStart 占位：暂不影响结算
            if (PlayerSave.PendingLuckyStart > 0)
            {
                PlayerSave.PendingLuckyStart = 0;
            }
            PlayerSave.Flush();
            return (fishMul, scoreMul);
        }

        public static bool HasPendingBoostersForNextRun =>
            PlayerSave.PendingDoubleFish > 0
            || PlayerSave.PendingScoreBoost > 0
            || PlayerSave.PendingLuckyStart > 0;

        // ── 私有辅助 ──────────────────────────────────────────────────────────

        private static bool IsBundle(ShopItemKind kind) =>
            kind == ShopItemKind.BundleStarter
            || kind == ShopItemKind.BundlePolar
            || kind == ShopItemKind.BundleAurora;

        private static void Dispense(ShopItemDefinition def)
        {
            switch (def.Kind)
            {
                case ShopItemKind.BoosterDoubleFish:
                    PlayerSave.BoosterDoubleFish = PlayerSave.BoosterDoubleFish + 1;
                    break;
                case ShopItemKind.BoosterScoreBoost:
                    PlayerSave.BoosterScoreBoost = PlayerSave.BoosterScoreBoost + 1;
                    break;
                case ShopItemKind.BoosterLuckyStart:
                    PlayerSave.BoosterLuckyStart = PlayerSave.BoosterLuckyStart + 1;
                    break;
                case ShopItemKind.FishPackSmall:
                    PlayerSave.AddFishSnacks(200);
                    break;
                case ShopItemKind.FishPackMedium:
                    PlayerSave.AddFishSnacks(600);
                    break;
                case ShopItemKind.CosmeticScarf:
                    PlayerSave.UnlockScarf(def.Param);
                    PlayerSave.SelectedScarfId = def.Param;
                    break;
                case ShopItemKind.CosmeticHat:
                    PlayerSave.UnlockHat(def.Param);
                    PlayerSave.SelectedHatId = def.Param;
                    break;
                case ShopItemKind.BundleStarter:
                    PlayerSave.AddFishSnacks(200);
                    PlayerSave.BoosterDoubleFish = PlayerSave.BoosterDoubleFish + 1;
                    PlayerSave.BoosterScoreBoost = PlayerSave.BoosterScoreBoost + 1;
                    PlayerSave.MarkBundleClaimed(def.Id);
                    break;
                case ShopItemKind.BundlePolar:
                    PlayerSave.AddFishSnacks(600);
                    PlayerSave.BoosterDoubleFish = PlayerSave.BoosterDoubleFish + 2;
                    PlayerSave.BoosterScoreBoost = PlayerSave.BoosterScoreBoost + 2;
                    PlayerSave.UnlockScarf("scarf_aurora");
                    PlayerSave.MarkBundleClaimed(def.Id);
                    break;
                case ShopItemKind.BundleAurora:
                    PlayerSave.AddFishSnacks(1500);
                    PlayerSave.BoosterDoubleFish = PlayerSave.BoosterDoubleFish + 3;
                    PlayerSave.BoosterScoreBoost = PlayerSave.BoosterScoreBoost + 3;
                    PlayerSave.BoosterLuckyStart = PlayerSave.BoosterLuckyStart + 2;
                    PlayerSave.UnlockScarf("scarf_pearl");
                    PlayerSave.MarkBundleClaimed(def.Id);
                    break;
            }
        }

        /// <summary>稳定的"按种子从池里挑 n 个"（不放回）。同一种子下多次调用结果一致。</summary>
        private static List<ShopItemDefinition> StablePick(List<ShopItemDefinition> pool, int seed, int count)
        {
            var n = pool.Count;
            count = Mathf.Clamp(count, 0, n);
            var indices = new int[n];
            for (var i = 0; i < n; i++) indices[i] = i;

            // Linear congruential，跨平台稳定
            uint state = (uint)(seed == 0 ? 1 : seed);
            for (var i = n - 1; i > 0; i--)
            {
                state = state * 1664525u + 1013904223u;
                var j = (int)(state % (uint)(i + 1));
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            var result = new List<ShopItemDefinition>(count);
            for (var i = 0; i < count; i++) result.Add(pool[indices[i]]);
            return result;
        }
    }
}
