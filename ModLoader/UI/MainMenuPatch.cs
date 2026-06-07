using System;
using System.Collections.Generic;
using BrokeProtocol.Client.UI;
using HarmonyLib;
using ModLoader.Helpers;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModLoader.UI
{
    /// <summary>
    /// Injecte un bouton "MODS" dans le menu principal en se branchant
    /// après MainMenu.Initialize.
    ///
    /// Le bouton hérite des classes USS d'un bouton existant pour matcher
    /// le look natif du menu.
    /// </summary>
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Initialize))]
    public static class MainMenu_AddModsButton
    {
        private const string ButtonName = "ModsButton";

        public static void Postfix(MainMenu __instance)
        {
            try
            {
                var uiClone = UIHelper.GetUIClone(__instance);
                if (uiClone == null)
                {
                    ConsoleBase.WriteLine("[ModLoader] MainMenu uiClone introuvable, bouton MODS non injecté");
                    return;
                }

                if (uiClone.Q<Button>(ButtonName) != null) return; // déjà injecté

                // Référence visuelle : on copie le style d'un bouton existant.
                // On essaie plusieurs noms au cas où le UXML change.
                Button reference = uiClone.Q<Button>("Workshop")
                                 ?? uiClone.Q<Button>("Offline")
                                 ?? uiClone.Q<Button>("Online");

                if (reference == null)
                {
                    ConsoleBase.WriteLine("[ModLoader] Aucun bouton de référence trouvé dans MainMenu, injection annulée");
                    return;
                }

                var modsBtn = new Button
                {
                    name = ButtonName,
                    text = "MODS"
                };

                // Copier les classes USS pour matcher le style natif
                foreach (var cls in reference.GetClasses())
                {
                    modsBtn.AddToClassList(cls);
                }

                modsBtn.clicked += OnModsClicked;

                // Insérer juste après le bouton de référence
                var parent = reference.parent;
                int index = parent.IndexOf(reference);
                parent.Insert(index + 1, modsBtn);

                ConsoleBase.WriteLine("[ModLoader] Bouton MODS injecté dans MainMenu");
            }
            catch (Exception ex)
            {
                ConsoleBase.WriteLine("[ModLoader] Échec injection bouton MODS : " + ex);
            }
        }

        private static void OnModsClicked()
        {
            try
            {
                ModsMenu.Toggle();
            }
            catch (Exception ex)
            {
                ConsoleBase.WriteLine("[ModLoader] Erreur ouverture ModsMenu : " + ex);
            }
        }
    }
}
