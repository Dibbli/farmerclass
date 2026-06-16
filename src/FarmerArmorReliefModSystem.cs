using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace FarmerClass
{
    // The farmer is "used to carrying stuff all day", so heavy armor barely starves them.
    // Vanilla only has a global `hungerrate` stat (armor adds to it: plate +0.24, chain +0.075...),
    // so we cancel part of the ARMOR-SOURCED hunger only, leaving base hunger untouched.
    // The movement half of this fantasy is done in JSON via the `armorWalkSpeedAffectedness` trait stat.
    public class FarmerArmorReliefModSystem : ModSystem
    {
        // Fraction of armor-sourced hunger removed for a farmer.
        // 0.4 -> full plate's +72% armor hunger drops to ~+43%. Tune freely.
        const float ArmorHungerReduction = 0.4f;

        // Our own stat key; vanilla/other systems never touch this category.
        const string ReliefCategory = "farmerarmorrelief";

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        }

        private void OnPlayerNowPlaying(IServerPlayer player)
        {
            var inv = player.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inv != null)
            {
                // Recompute whenever worn gear changes (armor put on / taken off).
                inv.SlotModified += _ => UpdateRelief(player);
            }

            // Recompute when the player (re)selects their class.
            player.Entity?.WatchedAttributes.RegisterModifiedListener("characterClass", () => UpdateRelief(player));

            UpdateRelief(player);
        }

        private void UpdateRelief(IServerPlayer player)
        {
            var entity = player.Entity;
            if (entity == null) return;

            if (entity.WatchedAttributes.GetString("characterClass") != "farmer")
            {
                entity.Stats.Remove("hungerrate", ReliefCategory);
                return;
            }

            float armorHunger = 0f;
            var inv = player.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inv != null)
            {
                foreach (var slot in inv)
                {
                    var supplier = slot?.Itemstack?.Collectible?.GetCollectibleInterface<IWearableStatsSupplier>();
                    if (supplier == null || !supplier.IsArmorType(slot)) continue;

                    var mods = supplier.GetStatModifiers(slot);
                    if (mods != null) armorHunger += mods.hungerrate;
                }
            }

            // Negative offset cancels part of the armor's hunger contribution. Base hunger is unaffected.
            entity.Stats.Set("hungerrate", ReliefCategory, -ArmorHungerReduction * armorHunger, false);
        }
    }
}
