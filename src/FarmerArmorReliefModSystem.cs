using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace FarmerClass
{
    // Reduces only the hunger penalty contributed by worn armor.
    public class FarmerArmorReliefModSystem : ModSystem
    {
        const float ArmorHungerReduction = 0.4f;
        const string ReliefCategory = "farmerarmorrelief";

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        }

        private void OnPlayerNowPlaying(IServerPlayer player)
        {
            var inv = player.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inv != null) inv.SlotModified += _ => UpdateRelief(player);

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

            entity.Stats.Set("hungerrate", ReliefCategory, -ArmorHungerReduction * armorHunger, false);
        }
    }
}
