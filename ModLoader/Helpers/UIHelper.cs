using BrokeProtocol.Client.UI;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModLoader.Helpers
{
    /// <summary>
    /// Helper class for UI operations
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Get the ClManager instance
        /// </summary>
        public static ClManager GetClManager()
        {
            return MonoBehaviourSingleton<ClManager>.Instance;
        }

        /// <summary>
        /// Get the main menu instance
        /// </summary>
        public static MainMenu GetMainMenu()
        {
            ClManager clManager = GetClManager();
            if (clManager != null && clManager.menus.TryGetValue("Default", out Menu menu))
            {
                return menu as MainMenu;
            }
            return null;
        }

        /// <summary>
        /// Get the uiClone from a Panel using reflection (since it's protected)
        /// Use this in Harmony patches to access the UI container
        /// </summary>
        public static TemplateContainer GetUIClone(Panel panel)
        {
            if (panel == null) return null;

            var field = typeof(Panel).GetField("uiClone", BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(panel) as TemplateContainer;
        }

        /// <summary>
        /// Get the HUD instance
        /// </summary>
        public static HUD GetHUD()
        {
            ClManager clManager = GetClManager();
            return clManager?.hud;
        }

        /// <summary>
        /// Get a specific menu by ID
        /// </summary>
        public static Menu GetMenu(string menuId)
        {
            ClManager clManager = GetClManager();
            if (clManager != null && clManager.menus.TryGetValue(menuId, out Menu menu))
            {
                return menu;
            }
            return null;
        }

        /// <summary>
        /// Get all currently open menus
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, Menu> GetAllMenus()
        {
            ClManager clManager = GetClManager();
            return clManager?.menus;
        }

        /// <summary>
        /// Create a button with text and click handler
        /// </summary>
        public static Button CreateButton(string text, System.Action onClick = null)
        {
            Button button = new Button();
            button.text = text;

            if (onClick != null)
            {
                button.clicked += onClick;
            }

            return button;
        }

        /// <summary>
        /// Create a styled button with custom colors
        /// </summary>
        public static Button CreateStyledButton(string text, Color backgroundColor, Color textColor, System.Action onClick = null)
        {
            Button button = CreateButton(text, onClick);
            button.style.backgroundColor = backgroundColor;
            button.style.color = textColor;
            return button;
        }

        /// <summary>
        /// Create a label with text
        /// </summary>
        public static Label CreateLabel(string text)
        {
            Label label = new Label(text);
            return label;
        }

        /// <summary>
        /// Create a styled label with custom color and size
        /// </summary>
        public static Label CreateStyledLabel(string text, Color color, int fontSize = 14)
        {
            Label label = CreateLabel(text);
            label.style.color = color;
            label.style.fontSize = fontSize;
            return label;
        }

        /// <summary>
        /// Show a game message to the player
        /// </summary>
        public static void ShowMessage(string message)
        {
            ClManager clManager = GetClManager();
            clManager?.ShowGameMessage(message);
        }

        /// <summary>
        /// Get the UI Document root
        /// </summary>
        public static VisualElement GetUIRoot()
        {
            return MonoBehaviourSingleton<SceneManager>.Instance?.uiDocument?.rootVisualElement;
        }

        /// <summary>
        /// Find a visual element by name in the UI tree
        /// </summary>
        public static T FindElement<T>(string name) where T : VisualElement
        {
            VisualElement root = GetUIRoot();
            return root?.Q<T>(name);
        }

        /// <summary>
        /// Check if any menu is currently open
        /// </summary>
        public static bool IsAnyMenuOpen()
        {
            ClManager clManager = GetClManager();
            return clManager != null && clManager.menus.Count > 0;
        }
    }
}

