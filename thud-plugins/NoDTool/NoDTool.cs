﻿using System;
using SharpDX;
using System.IO;


namespace Turbo.Plugins.Default
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using SharpDX.Text;
    using System.Windows.Forms;

    public class KadalaTab
    {
        public float screenX { get; }
        public float screenY { get; }

        public KadalaTab(float screenX, float screenY) 
        {
            this.screenX = screenX;
            this.screenY = screenY;
        }
    }

    public class KadalaItem
    {
        public String identifier { get; }
        public float screenX { get; }
        public float screenY { get; }
        public int bloodShardCost { get; }
        public int itemSize { get; }
        public KadalaTab kadalaTab { get; }

        public KadalaItem(String identifier, float screenX, float screenY, int bloodShardCost, int itemSize, KadalaTab kadalaTab)
        {
            this.identifier = identifier;
            this.screenX = screenX;
            this.screenY = screenY;
            this.bloodShardCost = bloodShardCost;
            this.itemSize = itemSize;
            this.kadalaTab = kadalaTab;
        }
    }

    public class NoDTool : BasePlugin, IInGameTopPainter, IBeforeRenderHandler
    {

        private static float VENDOR_ITEMS_LEFT_X = 0.05f;
        private static float VENDOR_ITEMS_RIGHT_X = 0.18f;
        private static float VENDOR_ITEMS_0_Y = 0.2f;
        private static float VENDOR_ITEMS_1_Y = 0.3f;
        private static float VENDOR_ITEMS_2_Y = 0.4f;
        private static float VENDOR_ITEMS_3_Y = 0.5f;
        private static float VENDOR_ITEMS_4_Y = 0.6f;

        private static KadalaTab KADALA_TAB_WEAPONS = new KadalaTab(0.27f, 0.17f);
        private static KadalaTab KADALA_TAB_ARMORY = new KadalaTab(0.27f, 0.32f);
        private static KadalaTab KADALA_TAB_TRINKETS = new KadalaTab(0.27f, 0.47f);

        private static KadalaItem[] KADALA_ITEMS = 
        {
            // WEAPONS
            new KadalaItem("KADALA_ITEM_ONE_HANDED_WEAPON", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_0_Y, 75, 2, KADALA_TAB_WEAPONS),
            new KadalaItem("KADALA_ITEM_TWO_HANDED_WEAPON", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_0_Y, 75, 2, KADALA_TAB_WEAPONS),
            new KadalaItem("KADALA_ITEM_QUIVER", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_1_Y, 25, 2, KADALA_TAB_WEAPONS),
            new KadalaItem("KADALA_ITEM_ORB", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_1_Y, 25, 2, KADALA_TAB_WEAPONS),
            new KadalaItem("KADALA_ITEM_MOJO", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_2_Y, 25, 2, KADALA_TAB_WEAPONS),
            new KadalaItem("KADALA_ITEM_PHYLACTERY", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_2_Y, 25, 2, KADALA_TAB_WEAPONS),
            // ARMORY
            new KadalaItem("KADALA_ITEM_HELM", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_0_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_HANDS", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_0_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_BOOTS", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_1_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_CHEST", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_1_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_BELT", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_2_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_SHOULDERS", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_2_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_PANTS", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_3_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_BRACERS", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_3_Y, 25, 2, KADALA_TAB_ARMORY),
            new KadalaItem("KADALA_ITEM_SHIELD", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_4_Y, 25, 2, KADALA_TAB_ARMORY),
            // TRINKETS
            new KadalaItem("KADALA_ITEM_RING", VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_0_Y, 50, 2, KADALA_TAB_TRINKETS),
            new KadalaItem("KADALA_ITEM_AMULET", VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_0_Y, 100, 2, KADALA_TAB_TRINKETS)
        };

        private static string PATH_TO_PLUGIN_CONFIG = "./TurboHUD/plugins/NoDTool/config";
        private static string CONFIG_FILE = "config.txt";

        private static string CONFIG_KEY_KADALA_GAMBLE_ON_ITEM = "KADALA_GAMBLE_ON_ITEM";

        private IUiElement salvageButton;
        private IUiElement salvageNormalButton;
        private IUiElement salvageMagicButton;
        private IUiElement salvageRareButton;
        private IUiElement salvageDialog;
        private IUiElement salvageTab;
        private IUiElement vendorTab4;

        private IFont watermark;

        private bool isSalvageAncientAndPrimal = true;

        private KadalaItem itemToGambleOn = null;

        public NoDTool()
        {
            Enabled = true;
            InitConfigWatcher();
            ReadConfig();
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            
            IUiElement vendorPage = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage", Hud.Inventory.InventoryMainUiElement, null);
            vendorTab4 = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.tab_4", vendorPage, null);
            salvageDialog = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog", vendorPage, null);
            salvageTab = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.tab_2", vendorPage, null);

            vendorTab4 = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.tab_4");
            salvageDialog = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog");
            salvageTab = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.tab_2");
        }

        public void PaintTopInGame(ClipState clipState)
        {
            watermark = Hud.Render.CreateFont("tahoma", 10, 155, 200, 0, 0, true, false, false);
            watermark.DrawText(watermark.GetTextLayout("NoDTool"), 4, Hud.Window.Size.Height * 0.966f);
        }
        
        public void BeforeRender()
        {
            bool isKadalaVisible = false;
            bool isBlackSmithVisible = vendorTab4.Visible;

            IUiElement KadalaWindow = Hud.Render.GetUiElement("Root.NormalLayer.shop_dialog_mainPage.gold_text");
            IUiElement VendorWindow = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.panel");

            isKadalaVisible = (KadalaWindow == null) ? false : KadalaWindow.Visible;

            if (isBlackSmithVisible) 
            {
                AutoSalvage();
                CloseChatIfOpen();
            }

            if (isKadalaVisible && itemToGambleOn != null) 
            {
                AutoGamble();
            }
        }

        private void InitConfigWatcher() {
            FileSystemWatcher watcher = new FileSystemWatcher(PATH_TO_PLUGIN_CONFIG)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
            watcher.Changed += (sender, e) => 
            {
                if (e.Name == CONFIG_FILE) 
                {
                    ReadConfig();
                }
            };
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfig()
        {
                using (StreamReader reader = new StreamReader(new FileStream(PATH_TO_PLUGIN_CONFIG + "/" + CONFIG_FILE, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    String[] contents = reader.ReadToEnd().Split('\n');
                    String kadalaItemId = ReadConfigEntry(contents, CONFIG_KEY_KADALA_GAMBLE_ON_ITEM);
                    itemToGambleOn = KADALA_ITEMS.Where(item => item.identifier == kadalaItemId).FirstOrDefault();
                }
        }

        private string ReadConfigEntry(String[] contents, String key) {
            String entry = contents.Where(line => line.StartsWith(key)).FirstOrDefault();
            if (entry == null) {
                return null;
            }
            String[] keyPair = entry.Split('=');
            if (keyPair.Length < 2) {
                return null;
            }
            return keyPair[1];
        }

        private void AutoGamble() {
            int numGambles = Math.Min(
                // max gambles to clear blood shards
                ((int) Hud.Game.Me.Materials.BloodShard) / itemToGambleOn.bloodShardCost,
                // num items fitting into inventory
                (Hud.Game.Me.InventorySpaceTotal - Hud.Game.InventorySpaceUsed) / itemToGambleOn.itemSize
            );
            if (numGambles < 1) {
                return;
            }

            Hud.Interaction.MouseMove(itemToGambleOn.kadalaTab.screenX * Hud.Window.Size.Width,  itemToGambleOn.kadalaTab.screenY * Hud.Window.Size.Height, 1, 1);
            LeftClick();
            for (int i = 0; i < numGambles; i++) {
                Hud.Interaction.MouseMove(itemToGambleOn.screenX * Hud.Window.Size.Width, itemToGambleOn.screenY * Hud.Window.Size.Height, 1, 1);
                RightClick();
            }
        }

        private void AutoSalvage() 
        {
            List<IItem> itemsToSalvage = new List<IItem>();
            foreach (var item in Hud.Inventory.ItemsInInventory)
            {
                if (CanSalvage(item)) 
                {
                    if (item.AncientRank != 0 && !isSalvageAncientAndPrimal) 
                    {
                        continue;
                    }
                    itemsToSalvage.Add(item);
                }
            }
            Salvage(itemsToSalvage);
        }

        private bool CanSalvage(IItem item)
        {
            return !(item.SnoItem.MainGroupCode == null ? "" : item.SnoItem.MainGroupCode).Contains("gems") 
                && item.SnoItem.MainGroupCode != "riftkeystone" 
                && item.SnoItem.MainGroupCode != "horadriccache" 
                && item.SnoItem.MainGroupCode != "-" 
                && item.SnoItem.MainGroupCode != "pony" 
                && item.SnoItem.MainGroupCode != "plans" 
                && !item.SnoItem.MainGroupCode.Contains("cosmetic")
                && item.Quantity <= 1
                && !item.VendorBought
                && !IsInArmorySet(item);
        }

        private bool IsInArmorySet(IItem item)
        {
            foreach (var armorySet in Hud.Game.Me.ArmorySets)
            {
                if (armorySet.ContainsItem(item))
                {
                    return true;
                }
            }
            return false;
        }

        private void ClickSalvageOk() {
            // TODO: double press needed?
            Hud.Wait(1);
            Hud.Interaction.PressEnter();
            Hud.Interaction.PressEnter();
        }

        private void LeftClick()
        {
            Hud.Interaction.MouseDown(MouseButtons.Left);
            Hud.Wait(1);
            Hud.Interaction.MouseUp(MouseButtons.Left);
        }

        private void RightClick()
        {
            Hud.Interaction.MouseDown(MouseButtons.Right);
            Hud.Wait(1);
            Hud.Interaction.MouseUp(MouseButtons.Right);
        }

        private void Salvage(List<IItem> items)
        {
            if (items.Count == 0) 
            {
                return;
            }
            Hud.Interaction.ClickUiElement(MouseButtons.Left, salvageTab);
            Hud.Wait(1);
            
            if (salvageButton == null || salvageNormalButton == null || salvageMagicButton == null || salvageRareButton == null) {
                // initialize salvage buttons
                salvageButton = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog.salvage_all_wrapper.salvage_button");
                salvageNormalButton = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog.salvage_all_wrapper.salvage_normal_button");
                salvageMagicButton = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog.salvage_all_wrapper.salvage_magic_button");
                salvageRareButton = Hud.Render.GetUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog.salvage_all_wrapper.salvage_rare_button");
                // return to make sure buttons on this tab are registered
                return;
            }
            SalvageNonLegendaries(items);
            List<IItem> legendariesOrderedByPosition = items
                .Where(item => !item.IsNormal && !item.IsMagic && !item.IsRare)
                .OrderBy(item => item.InventoryY)
                .ThenBy(item => item.InventoryX)
                .ToList();
            SalvageLegendaries(legendariesOrderedByPosition);
        }
        
        private void SalvageNonLegendaries(List<IItem> items) {
            if (items.Any(item => item.IsNormal)) {
                Hud.Interaction.ClickUiElement(MouseButtons.Left, salvageNormalButton);
                ClickSalvageOk();
            }
            if (items.Any(item => item.IsMagic)) {
                Hud.Interaction.ClickUiElement(MouseButtons.Left, salvageMagicButton);
                ClickSalvageOk();
            }
            if (items.Any(item => item.IsRare)) {
                Hud.Interaction.ClickUiElement(MouseButtons.Left, salvageRareButton);
                ClickSalvageOk();
            }
        }

        private void SalvageLegendaries(List<IItem> legendaries) {
            if (!IsSalvageButtonClicked()) {
                Hud.Interaction.ClickUiElement(MouseButtons.Left, salvageButton);
                Hud.Wait(100);
            }
            foreach (var item in legendaries)
            {
                Hud.Interaction.MoveMouseOverInventoryItem(item);
                LeftClick();
                ClickSalvageOk();
            }
        }

        private bool IsSalvageButtonClicked() {
            return salvageButton == null ? false : (salvageButton.AnimState == 19 || salvageButton.AnimState == 20);
        }

        private void CloseChatIfOpen() {
            IUiElement chat = Hud.Render.GetUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline");
            if (Hud.WaitFor(100, 10, 10, () => chat.Visible))
            {
                Hud.Interaction.PressEnter();
            }
        }
    }
}