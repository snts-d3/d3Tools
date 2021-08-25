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

    public class NoDTool : BasePlugin, IInGameTopPainter, IBeforeRenderHandler
    {

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
            // quiver
            int bloodShardCost = 25;
            int itemSize = 2;
            float KADALA_TAB_WEAPONS_X = 0.27f;
            float KADALA_TAB_WEAPONS_Y = 0.17f;
            float KADALA_WEAPONS_QUIVER_X = 0.05f;
            float KADALA_WEAPONS_QUIVER_Y = 0.3f;

            int numGambles = Math.Min(
                // max gambles to clear blood shards
                ((int) Hud.Game.Me.Materials.BloodShard) / bloodShardCost,
                // num items fitting into inventory
                (Hud.Game.Me.InventorySpaceTotal - Hud.Game.InventorySpaceUsed) / itemSize
            );
            if (numGambles < 1) {
                return;
            }

            Hud.Interaction.MouseMove(KADALA_TAB_WEAPONS_X * Hud.Window.Size.Width,  KADALA_TAB_WEAPONS_Y * Hud.Window.Size.Height, 1, 1);
            LeftClick();
            for (int i = 0; i < numGambles; i++) {
                Hud.Interaction.MouseMove(KADALA_WEAPONS_QUIVER_X * Hud.Window.Size.Width, KADALA_WEAPONS_QUIVER_Y * Hud.Window.Size.Height, 1, 1);
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