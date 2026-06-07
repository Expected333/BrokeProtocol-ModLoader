using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using ModLoader.Helpers;
using ModLoader.Utility;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModLoader.UI
{
    /// <summary>
    /// Menu de gestion des mods : onglet "Installés" (local) + onglet "Disponibles" (registry distant).
    /// Implémenté comme overlay UI Toolkit pour éviter un VisualTreeAsset prefab.
    /// </summary>
    public static class ModsMenu
    {
        private const string RootName = "ModLoader_ModsMenuRoot";
        private static VisualElement _overlay;

        private enum Tab { Installed, Browse }
        private static Tab _currentTab = Tab.Installed;

        // Cache fetch
        private static RegistryIndex _cachedIndex;
        private static string _cachedError;
        private static bool _isFetching;
        private static readonly HashSet<string> _downloading = new HashSet<string>();

        public static bool IsOpen => _overlay != null && _overlay.parent != null;

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public static void Open()
        {
            var root = UIHelper.GetUIRoot();
            if (root == null)
            {
                ConsoleBase.WriteLine("[ModLoader] UI root introuvable, impossible d'ouvrir ModsMenu");
                return;
            }
            if (IsOpen) return;

            _overlay = BuildOverlay();
            root.Add(_overlay);
            ShowTab(_currentTab);
        }

        public static void Close()
        {
            _overlay?.RemoveFromHierarchy();
            _overlay = null;
        }

        // =================================================================
        //  SHELL UI
        // =================================================================

        private static VisualElement BuildOverlay()
        {
            var overlay = new VisualElement { name = RootName };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.pickingMode = PickingMode.Position;

            var panel = new VisualElement { name = "ModLoader_ModsPanel" };
            panel.style.width = 760;
            panel.style.maxHeight = Length.Percent(88);
            panel.style.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 0.98f);
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = 10;
            panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 10;
            panel.style.paddingLeft = panel.style.paddingRight = 24;
            panel.style.paddingTop = panel.style.paddingBottom = 20;
            ApplyBorder(panel, new Color(0.18f, 0.55f, 0.95f, 1f), 2);

            panel.Add(BuildHeader());
            panel.Add(BuildTabBar());

            var content = new VisualElement { name = "ModLoader_TabContent" };
            content.style.flexGrow = 1;
            content.style.marginTop = 12;
            panel.Add(content);

            var footer = new Label { name = "ModLoader_StatusLabel", text = "" };
            footer.style.marginTop = 12;
            footer.style.color = new Color(0.7f, 0.85f, 1f, 1f);
            footer.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(footer);

            overlay.Add(panel);
            return overlay;
        }

        private static VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 8;

            var title = new Label { text = "GESTION DES MODS" };
            title.style.fontSize = 22;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;

            var close = new Button { text = "X" };
            close.clicked += Close;
            close.style.width = 36;
            close.style.height = 32;
            close.style.color = Color.white;
            close.style.backgroundColor = new Color(0.5f, 0.15f, 0.15f, 1f);

            header.Add(title);
            header.Add(close);
            return header;
        }

        private static VisualElement BuildTabBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.borderBottomWidth = 2;
            bar.style.borderBottomColor = new Color(0.2f, 0.2f, 0.24f, 1f);
            bar.style.marginTop = 4;

            bar.Add(MakeTabButton("Installés", Tab.Installed));
            bar.Add(MakeTabButton("Disponibles", Tab.Browse));
            return bar;
        }

        private static Button MakeTabButton(string text, Tab tab)
        {
            bool active = _currentTab == tab;
            var b = new Button { text = text, name = "Tab_" + tab };
            b.style.color = Color.white;
            b.style.backgroundColor = active
                ? new Color(0.18f, 0.55f, 0.95f, 1f)
                : new Color(0.15f, 0.15f, 0.18f, 1f);
            b.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            b.style.paddingLeft = b.style.paddingRight = 18;
            b.style.paddingTop = b.style.paddingBottom = 8;
            b.style.marginRight = 4;
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = 6;
            b.clicked += () => ShowTab(tab);
            return b;
        }

        private static void ShowTab(Tab tab)
        {
            _currentTab = tab;
            if (_overlay == null) return;

            // refresh tab bar styles
            var tabBar = _overlay.Q<VisualElement>().Q(className: null); // not needed; rebuild approach instead
            var panel = _overlay.Q<VisualElement>("ModLoader_ModsPanel");
            // Rebuild tab bar (simpler than tracking styles)
            var oldBar = panel.ElementAt(1);
            panel.RemoveAt(1);
            panel.Insert(1, BuildTabBar());

            var content = _overlay.Q<VisualElement>("ModLoader_TabContent");
            content.Clear();

            if (tab == Tab.Installed)
            {
                content.Add(BuildInstalledTab());
            }
            else
            {
                content.Add(BuildBrowseTab());
                if (_cachedIndex == null && !_isFetching)
                {
                    FetchRegistry();
                }
            }

            SetStatus(string.Empty);
        }

        // =================================================================
        //  TAB : INSTALLED
        // =================================================================

        private static VisualElement BuildInstalledTab()
        {
            var root = new VisualElement();

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.marginBottom = 8;

            var install = MakeActionButton("Installer un .dll local...", new Color(0.18f, 0.55f, 0.95f, 1f));
            install.clicked += OnInstallLocalClicked;
            toolbar.Add(install);

            var openFolder = MakeActionButton("Ouvrir le dossier Mods", new Color(0.3f, 0.3f, 0.34f, 1f));
            openFolder.clicked += OnOpenFolderClicked;
            toolbar.Add(openFolder);

            var refresh = MakeActionButton("Actualiser", new Color(0.3f, 0.3f, 0.34f, 1f));
            refresh.clicked += () => ShowTab(Tab.Installed);
            toolbar.Add(refresh);

            root.Add(toolbar);

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "ModLoader_InstalledList" };
            scroll.style.flexGrow = 1;
            scroll.style.maxHeight = 540;

            var entries = ModInspector.Scan();
            if (entries.Count == 0)
            {
                var empty = new Label { text = "Aucun mod détecté dans " + ModInspector.ModsDirectory };
                empty.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                empty.style.unityFontStyleAndWeight = FontStyle.Italic;
                empty.style.marginTop = 16;
                empty.style.whiteSpace = WhiteSpace.Normal;
                scroll.Add(empty);
            }
            else
            {
                foreach (var e in entries) scroll.Add(BuildInstalledCard(e));
            }

            root.Add(scroll);
            return root;
        }

        private static VisualElement BuildInstalledCard(ModEntry entry)
        {
            var card = MakeCard(ColorForState(entry.State));

            var info = new VisualElement();
            info.style.flexGrow = 1;
            info.style.flexShrink = 1;

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;

            var name = new Label { text = entry.Name ?? entry.FileName };
            name.style.color = Color.white;
            name.style.fontSize = 15;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;

            titleRow.Add(name);
            titleRow.Add(MakeBadge(BadgeText(entry), ColorForState(entry.State)));

            string meta = string.Empty;
            if (!string.IsNullOrEmpty(entry.Version)) meta += "v" + entry.Version;
            if (!string.IsNullOrEmpty(entry.Author))
            {
                if (meta.Length > 0) meta += " · ";
                meta += "par " + entry.Author;
            }
            if (meta.Length == 0) meta = entry.FileName;

            var sub = new Label { text = meta };
            sub.style.color = new Color(0.7f, 0.7f, 0.72f, 1f);
            sub.style.fontSize = 11;
            sub.style.marginTop = 2;

            info.Add(titleRow);
            info.Add(sub);

            if (!string.IsNullOrEmpty(entry.Error))
            {
                var err = new Label { text = "⚠ " + entry.Error };
                err.style.color = new Color(1f, 0.5f, 0.5f, 1f);
                err.style.fontSize = 10;
                err.style.marginTop = 2;
                err.style.whiteSpace = WhiteSpace.Normal;
                info.Add(err);
            }

            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.flexShrink = 0;

            if (entry.State != ModState.Failed)
            {
                string toggleLabel = entry.State == ModState.Active ? "Désactiver" : "Activer";
                Color toggleColor = entry.State == ModState.Active
                    ? new Color(0.7f, 0.5f, 0.15f, 1f)
                    : new Color(0.2f, 0.6f, 0.2f, 1f);
                var toggle = MakeActionButton(toggleLabel, toggleColor);
                toggle.clicked += () => OnToggleClicked(entry);
                actions.Add(toggle);
            }

            var del = MakeActionButton("Supprimer", new Color(0.6f, 0.18f, 0.18f, 1f));
            del.clicked += () => OnDeleteClicked(entry);
            actions.Add(del);

            card.Add(info);
            card.Add(actions);
            return card;
        }

        // =================================================================
        //  TAB : BROWSE (registry)
        // =================================================================

        private static VisualElement BuildBrowseTab()
        {
            var root = new VisualElement();

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.marginBottom = 8;
            toolbar.style.alignItems = Align.Center;

            var refresh = MakeActionButton("Recharger l'index", new Color(0.18f, 0.55f, 0.95f, 1f));
            refresh.clicked += () => FetchRegistry(force: true);
            toolbar.Add(refresh);

            var sourceLabel = new Label { text = "Source : " + ModRegistry.ResolveRegistryUrl() };
            sourceLabel.style.color = new Color(0.6f, 0.6f, 0.64f, 1f);
            sourceLabel.style.fontSize = 10;
            sourceLabel.style.marginLeft = 12;
            sourceLabel.style.flexGrow = 1;
            sourceLabel.style.whiteSpace = WhiteSpace.NoWrap;
            sourceLabel.style.overflow = Overflow.Hidden;
            toolbar.Add(sourceLabel);

            root.Add(toolbar);

            var listScroll = new ScrollView(ScrollViewMode.Vertical) { name = "ModLoader_BrowseList" };
            listScroll.style.flexGrow = 1;
            listScroll.style.maxHeight = 540;

            if (_isFetching)
            {
                listScroll.Add(MakeNoticeLabel("Chargement de l'index distant..."));
            }
            else if (_cachedError != null)
            {
                listScroll.Add(MakeNoticeLabel("Erreur : " + _cachedError, isError: true));
            }
            else if (_cachedIndex == null)
            {
                listScroll.Add(MakeNoticeLabel("Cliquez sur 'Recharger l'index' pour récupérer la liste des mods."));
            }
            else if (_cachedIndex.Mods.Count == 0)
            {
                listScroll.Add(MakeNoticeLabel("Aucun mod publié pour l'instant."));
            }
            else
            {
                var installed = ModInspector.Scan();
                foreach (var m in _cachedIndex.Mods)
                {
                    listScroll.Add(BuildRemoteCard(m, installed));
                }
            }

            root.Add(listScroll);
            return root;
        }

        private static VisualElement BuildRemoteCard(RemoteMod mod, List<ModEntry> installed)
        {
            ModEntry localMatch = null;
            foreach (var i in installed)
            {
                if (string.Equals(i.Name, mod.Name, StringComparison.OrdinalIgnoreCase)) { localMatch = i; break; }
            }

            bool downloading = _downloading.Contains(mod.Name);
            bool installedSameVersion = localMatch != null &&
                string.Equals(localMatch.Version, mod.Version, StringComparison.OrdinalIgnoreCase);
            bool updateAvailable = localMatch != null && !installedSameVersion;

            Color borderColor =
                downloading ? new Color(0.18f, 0.55f, 0.95f, 1f) :
                installedSameVersion ? new Color(0.2f, 0.6f, 0.2f, 1f) :
                updateAvailable ? new Color(0.85f, 0.6f, 0.15f, 1f) :
                new Color(0.3f, 0.3f, 0.34f, 1f);

            var card = MakeCard(borderColor);

            var info = new VisualElement();
            info.style.flexGrow = 1;
            info.style.flexShrink = 1;
            info.style.flexBasis = 0;

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.flexWrap = Wrap.Wrap;

            var name = new Label { text = mod.Name };
            name.style.color = Color.white;
            name.style.fontSize = 15;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(name);

            var version = new Label { text = "v" + mod.Version };
            version.style.color = new Color(0.7f, 0.85f, 1f, 1f);
            version.style.fontSize = 12;
            version.style.marginLeft = 8;
            titleRow.Add(version);

            if (installedSameVersion)
                titleRow.Add(MakeBadge("INSTALLÉ", new Color(0.2f, 0.6f, 0.2f, 1f)));
            else if (updateAvailable)
                titleRow.Add(MakeBadge("MAJ DISPO (" + localMatch.Version + " → " + mod.Version + ")", new Color(0.85f, 0.6f, 0.15f, 1f)));

            info.Add(titleRow);

            var sub = new Label { text = "par " + mod.Author + " · " + FormatSize(mod.SizeBytes) + " · ★ " + mod.Stars };
            sub.style.color = new Color(0.7f, 0.7f, 0.72f, 1f);
            sub.style.fontSize = 11;
            sub.style.marginTop = 2;
            info.Add(sub);

            if (!string.IsNullOrEmpty(mod.Description))
            {
                var desc = new Label { text = mod.Description };
                desc.style.color = new Color(0.85f, 0.85f, 0.87f, 1f);
                desc.style.fontSize = 11;
                desc.style.marginTop = 4;
                desc.style.whiteSpace = WhiteSpace.Normal;
                info.Add(desc);
            }

            if (mod.Tags != null && mod.Tags.Count > 0)
            {
                var tagRow = new VisualElement();
                tagRow.style.flexDirection = FlexDirection.Row;
                tagRow.style.flexWrap = Wrap.Wrap;
                tagRow.style.marginTop = 4;
                foreach (var t in mod.Tags)
                {
                    var tagChip = new Label { text = "#" + t };
                    tagChip.style.color = new Color(0.65f, 0.75f, 0.95f, 1f);
                    tagChip.style.fontSize = 10;
                    tagChip.style.marginRight = 8;
                    tagRow.Add(tagChip);
                }
                info.Add(tagRow);
            }

            // Actions
            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Column;
            actions.style.flexShrink = 0;
            actions.style.alignItems = Align.FlexEnd;

            Button mainBtn;
            if (downloading)
            {
                mainBtn = MakeActionButton("Téléchargement...", new Color(0.3f, 0.3f, 0.34f, 1f));
                mainBtn.SetEnabled(false);
            }
            else if (installedSameVersion)
            {
                mainBtn = MakeActionButton("Installé ✓", new Color(0.2f, 0.6f, 0.2f, 1f));
                mainBtn.SetEnabled(false);
            }
            else if (updateAvailable)
            {
                mainBtn = MakeActionButton("Mettre à jour", new Color(0.85f, 0.6f, 0.15f, 1f));
                mainBtn.clicked += () => OnInstallRemoteClicked(mod);
            }
            else
            {
                mainBtn = MakeActionButton("Installer", new Color(0.18f, 0.55f, 0.95f, 1f));
                mainBtn.clicked += () => OnInstallRemoteClicked(mod);
            }
            actions.Add(mainBtn);

            if (!string.IsNullOrEmpty(mod.Repo))
            {
                var repoBtn = MakeActionButton("Repo", new Color(0.3f, 0.3f, 0.34f, 1f));
                repoBtn.clicked += () =>
                {
                    try { Process.Start(new ProcessStartInfo { FileName = mod.Repo, UseShellExecute = true }); }
                    catch (Exception ex) { SetStatus("Erreur ouverture URL : " + ex.Message); }
                };
                actions.Add(repoBtn);
            }

            card.Add(info);
            card.Add(actions);
            return card;
        }

        // =================================================================
        //  ACTIONS
        // =================================================================

        private static void OnToggleClicked(ModEntry entry)
        {
            ModInspector.Toggle(entry, out string msg);
            SetStatus(msg);
            ShowTab(Tab.Installed);
        }

        private static void OnDeleteClicked(ModEntry entry)
        {
            ModInspector.Delete(entry, out string msg);
            SetStatus(msg);
            ShowTab(Tab.Installed);
        }

        private static void OnOpenFolderClicked()
        {
            try
            {
                ModInspector.EnsureDirectory();
                Process.Start(new ProcessStartInfo { FileName = ModInspector.ModsDirectory, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SetStatus("Impossible d'ouvrir le dossier : " + ex.Message);
            }
        }

        private static void OnInstallLocalClicked()
        {
            try
            {
                string picked = PickDllFile();
                if (string.IsNullOrEmpty(picked))
                {
                    SetStatus("Installation annulée.");
                    return;
                }
                ModInspector.Install(picked, out string msg);
                SetStatus(msg);
                ShowTab(Tab.Installed);
            }
            catch (Exception ex)
            {
                SetStatus("Erreur installation : " + ex.Message);
            }
        }

        private static void OnInstallRemoteClicked(RemoteMod mod)
        {
            var cl = MonoBehaviourSingleton<ClManager>.Instance;
            if (cl == null)
            {
                SetStatus("ClManager indisponible (coroutine impossible).");
                return;
            }

            _downloading.Add(mod.Name);
            ShowTab(Tab.Browse);
            SetStatus("Téléchargement de " + mod.Name + "...");

            cl.StartCoroutine(ModDownloader.Install(
                mod,
                progress => SetStatus("Téléchargement de " + mod.Name + " : " + Mathf.RoundToInt(progress * 100f) + "%"),
                result =>
                {
                    _downloading.Remove(mod.Name);
                    if (result.Success)
                    {
                        SetStatus("✓ " + mod.Name + " v" + mod.Version + " installé. Redémarrage requis pour activer.");
                    }
                    else
                    {
                        SetStatus("✗ Échec : " + result.Error);
                    }
                    if (_currentTab == Tab.Browse) ShowTab(Tab.Browse);
                }
            ));
        }

        private static void FetchRegistry(bool force = false)
        {
            if (_isFetching) return;
            if (!force && _cachedIndex != null) return;

            var cl = MonoBehaviourSingleton<ClManager>.Instance;
            if (cl == null)
            {
                _cachedError = "ClManager indisponible.";
                if (_currentTab == Tab.Browse) ShowTab(Tab.Browse);
                return;
            }

            _isFetching = true;
            _cachedError = null;
            if (_currentTab == Tab.Browse) ShowTab(Tab.Browse);

            cl.StartCoroutine(ModRegistry.Fetch(result =>
            {
                _isFetching = false;
                if (result.Success)
                {
                    _cachedIndex = result.Index;
                    _cachedError = null;
                    SetStatus("Index chargé : " + result.Index.Mods.Count + " mod(s) disponibles.");
                }
                else
                {
                    _cachedError = result.Error;
                    SetStatus("Échec chargement registry : " + result.Error);
                }
                if (_currentTab == Tab.Browse) ShowTab(Tab.Browse);
            }));
        }

        private static void SetStatus(string msg)
        {
            if (_overlay == null) return;
            var label = _overlay.Q<Label>("ModLoader_StatusLabel");
            if (label != null) label.text = msg ?? string.Empty;
        }

        // =================================================================
        //  HELPERS UI
        // =================================================================

        private static VisualElement MakeCard(Color borderColor)
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.justifyContent = Justify.SpaceBetween;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 1f);
            card.style.marginBottom = 8;
            card.style.paddingLeft = card.style.paddingRight = 14;
            card.style.paddingTop = card.style.paddingBottom = 10;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 6;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 6;
            ApplyBorder(card, borderColor, 1);
            return card;
        }

        private static Label MakeBadge(string text, Color bg)
        {
            var badge = new Label { text = text };
            badge.style.color = Color.white;
            badge.style.backgroundColor = bg;
            badge.style.marginLeft = 10;
            badge.style.paddingLeft = badge.style.paddingRight = 8;
            badge.style.paddingTop = badge.style.paddingBottom = 2;
            badge.style.fontSize = 10;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius = 4;
            badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 4;
            return badge;
        }

        private static Button MakeActionButton(string text, Color bg)
        {
            var b = new Button { text = text };
            b.style.color = Color.white;
            b.style.backgroundColor = bg;
            b.style.paddingLeft = b.style.paddingRight = 14;
            b.style.paddingTop = b.style.paddingBottom = 8;
            b.style.marginRight = 6;
            b.style.marginTop = 2;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            return b;
        }

        private static Label MakeNoticeLabel(string text, bool isError = false)
        {
            var l = new Label { text = text };
            l.style.color = isError
                ? new Color(1f, 0.5f, 0.5f, 1f)
                : new Color(0.8f, 0.8f, 0.8f, 1f);
            l.style.unityFontStyleAndWeight = FontStyle.Italic;
            l.style.marginTop = 16;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        private static string BadgeText(ModEntry entry)
        {
            switch (entry.State)
            {
                case ModState.Active: return entry.LoadedInSession ? "ACTIF" : "ACTIF (non chargé)";
                case ModState.Disabled: return "DÉSACTIVÉ";
                case ModState.Failed: return "ERREUR";
                default: return "?";
            }
        }

        private static Color ColorForState(ModState s)
        {
            switch (s)
            {
                case ModState.Active: return new Color(0.2f, 0.6f, 0.2f, 1f);
                case ModState.Disabled: return new Color(0.55f, 0.55f, 0.55f, 1f);
                case ModState.Failed: return new Color(0.7f, 0.2f, 0.2f, 1f);
                default: return Color.gray;
            }
        }

        private static void ApplyBorder(VisualElement el, Color color, float width)
        {
            el.style.borderLeftColor = color;
            el.style.borderRightColor = color;
            el.style.borderTopColor = color;
            el.style.borderBottomColor = color;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "? Ko";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.#") + " Ko";
            return (bytes / 1024.0 / 1024.0).ToString("0.##") + " Mo";
        }

        // =================================================================
        //  Win32 OpenFileDialog
        // =================================================================

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_NOCHANGEDIR = 0x00000008;

        private static string PickDllFile()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.WindowsEditor)
            {
                SetStatus("Sélecteur de fichier dispo uniquement sur Windows. Utilisez 'Ouvrir le dossier Mods'.");
                return null;
            }

            var ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = "Mod DLL\0*.dll\0Tous les fichiers\0*.*\0\0";
            ofn.file = new string(new char[260]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[260]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = ModInspector.ModsDirectory;
            ofn.title = "Sélectionner un mod (.dll) à installer";
            ofn.defExt = "dll";
            ofn.flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

            return GetOpenFileName(ofn) ? ofn.file : null;
        }
    }
}
