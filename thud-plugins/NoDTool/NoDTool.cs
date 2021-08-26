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
        public float screenX { get; }
        public float screenY { get; }
        public int bloodShardCost { get; }
        public int itemSize { get; }
        public KadalaTab kadalaTab { get; }

        public KadalaItem(float screenX, float screenY, int bloodShardCost, int itemSize, KadalaTab kadalaTab)
        {
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
        private static float VENDOR_ITEMS_6_Y = 0.7f;

        private static KadalaTab KADALA_TAB_WEAPONS = new KadalaTab(0.27f, 0.17f);
        private static KadalaTab KADALA_TAB_ARMORY = new KadalaTab(0.27f, 0.32f);

        private static KadalaItem KADALA_ITEM_QUIVER = new KadalaItem(VENDOR_ITEMS_LEFT_X, VENDOR_ITEMS_1_Y, 25, 2, KADALA_TAB_WEAPONS);
        private static KadalaItem KADALA_ITEM_TWO_HANDED_WEAPON = new KadalaItem(VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_0_Y, 75, 2, KADALA_TAB_WEAPONS);
        private static KadalaItem KADALA_ITEM_HANDS = new KadalaItem(VENDOR_ITEMS_RIGHT_X, VENDOR_ITEMS_0_Y, 25, 2, KADALA_TAB_ARMORY);

        private IUiElement salvageButton;
        private IUiElement salvageNormalButton;
        private IUiElement salvageMagicButton;
        private IUiElement salvageRareButton;
        private IUiElement salvageDialog;
        private IUiElement salvageTab;
        private IUiElement vendorTab4;
        private HashSet<string> itemCache;

        private IFont watermark;

        private bool isSalvageAncientAndPrimal = true;

        private KadalaItem itemToGambleOn = KADALA_ITEM_HANDS;

        public NoDTool()
        {
            Enabled = true;
            itemCache = new HashSet<string>();
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

            if (isKadalaVisible) 
            {
                AutoGamble();
            }
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