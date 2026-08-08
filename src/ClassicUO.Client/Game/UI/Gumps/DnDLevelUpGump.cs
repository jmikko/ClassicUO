// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.DnD;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class DnDLevelUpGump : Gump
    {
        private const int BUTTON_CONFIRM = 1;

        private const int BUTTON_ASI_INC_BASE = 100;
        private const int BUTTON_ASI_DEC_BASE = 200;
        private const int BUTTON_CLASS_BASE = 300;
        private const int BUTTON_FEAT_BASE = 500;
        private const int BUTTON_SPELL_BASE = 1000;

        private readonly int _pendingLevels;
        private readonly int _pendingASI;
        private readonly int _pendingSpellsKnown;
        private readonly List<(string name, string parent)> _classes;
        private readonly List<string> _feats;
        private readonly List<DnDSpellEntry> _spells;

        private int _asiSpent;
        private readonly int[] _abilityIncreases = new int[6];
        private readonly Label[] _abilityIncreaseLabels = new Label[6];
        private Label _asiPointsLabel;

        private string _chosenClass = string.Empty;
        private string _chosenFeat = string.Empty;

        private int _spellsSelected;
        private readonly HashSet<int> _selectedSpellIds = new HashSet<int>();
        private Label _spellsPointsLabel;
        private readonly Dictionary<int, NiceButton> _spellButtons = new Dictionary<int, NiceButton>();
        private readonly Dictionary<int, NiceButton> _classButtons = new Dictionary<int, NiceButton>();
        private readonly Dictionary<int, NiceButton> _featButtons = new Dictionary<int, NiceButton>();

        private static readonly string[] _scoreNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        public DnDLevelUpGump(World world, int pendingLevels, int pendingASI, int pendingSpellsKnown, List<(string name, string parent)> classes, List<string> feats, List<DnDSpellEntry> spells) : base(world, 0, 0)
        {
            _pendingLevels = pendingLevels;
            _pendingASI = pendingASI;
            _pendingSpellsKnown = pendingSpellsKnown;
            _classes = classes;
            _feats = feats;
            _spells = spells;

            CanMove = true;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;

            Width = 600;
            Height = 700;

            Add(new ResizePic(0x0A28) { Width = Width, Height = Height });
            Add(new Label("D&D Level Up Choices", true, 0x0035, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 14, Y = 14 });

            int currentY = 40;

            if (_pendingLevels > 0)
            {
                Add(new Label($"Class / Subclass (Pending Levels: {_pendingLevels})", true, 0x0481, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 14, Y = currentY });
                currentY += 24;

                int row = 0;
                int col = 0;
                for (int i = 0; i < classes.Count; i++)
                {
                    string displayName = string.IsNullOrEmpty(classes[i].parent) ? classes[i].name : $"{classes[i].name} ({classes[i].parent})";
                    NiceButton btn = new NiceButton(14 + (col * 190), currentY + (row * 24), 180, 20, ButtonAction.Activate, displayName, 0);
                    btn.ButtonParameter = BUTTON_CLASS_BASE + i;
                    btn.IsSelected = false;
                    Add(btn);
                    _classButtons[btn.ButtonParameter] = btn;

                    col++;
                    if (col >= 3)
                    {
                        col = 0;
                        row++;
                    }
                }
                currentY += (row + 1) * 24 + 10;
            }

            if (_pendingASI > 0)
            {
                _asiPointsLabel = new Label($"Ability Score Increases (Pending: {_pendingASI}) - Choose ASI OR Feats", true, 0x0481, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 14, Y = currentY };
                Add(_asiPointsLabel);
                currentY += 24;

                int asiStartY = currentY;
                for (int i = 0; i < 6; i++)
                {
                    Add(new Label(_scoreNames[i], true, 0x03E3, 100, 0xFF, FontStyle.BlackBorder) { X = 14, Y = currentY });
                    
                    NiceButton decBtn = new NiceButton(80, currentY, 20, 20, ButtonAction.Activate, "-", 0);
                    decBtn.ButtonParameter = BUTTON_ASI_DEC_BASE + i;
                    Add(decBtn);

                    _abilityIncreaseLabels[i] = new Label("0", true, 0xFFFF, 20, 0xFF, FontStyle.BlackBorder) { X = 110, Y = currentY };
                    Add(_abilityIncreaseLabels[i]);

                    NiceButton incBtn = new NiceButton(130, currentY, 20, 20, ButtonAction.Activate, "+", 0);
                    incBtn.ButtonParameter = BUTTON_ASI_INC_BASE + i;
                    Add(incBtn);

                    currentY += 24;
                }

                currentY = asiStartY;
                if (feats.Count > 0)
                {
                    Add(new Label("Available Feats:", true, 0x0481, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 200, Y = currentY });
                    currentY += 24;
                    for (int i = 0; i < feats.Count; i++)
                    {
                        NiceButton btn = new NiceButton(200, currentY, 150, 20, ButtonAction.Activate, feats[i], 0);
                        btn.ButtonParameter = BUTTON_FEAT_BASE + i;
                        btn.IsSelected = false;
                        Add(btn);
                        _featButtons[btn.ButtonParameter] = btn;
                        currentY += 24;
                    }
                }
                currentY = Math.Max(asiStartY + 144, currentY) + 10;
            }

            if (_pendingSpellsKnown > 0)
            {
                _spellsPointsLabel = new Label($"Spells Known (Pending: {_pendingSpellsKnown})", true, 0x0481, Width - 20, 0xFF, FontStyle.BlackBorder) { X = 14, Y = currentY };
                Add(_spellsPointsLabel);
                currentY += 24;

                int row = 0;
                int col = 0;
                for (int i = 0; i < spells.Count; i++)
                {
                    var spell = spells[i];
                    NiceButton spellBtn = new NiceButton(14 + (col * 180), currentY + (row * 24), 170, 20, ButtonAction.Activate, $"{spell.Name} (Lvl {spell.Level})", 1000 + i);
                    spellBtn.ButtonParameter = BUTTON_SPELL_BASE + i;
                    spellBtn.IsSelected = false;
                    Add(spellBtn);
                    _spellButtons[spellBtn.ButtonParameter] = spellBtn;

                    col++;
                    if (col >= 3)
                    {
                        col = 0;
                        row++;
                    }
                }
                currentY += (row + 1) * 24 + 10;
            }

            NiceButton confirmBtn = new NiceButton(Width / 2 - 40, Height - 30, 80, 22, ButtonAction.Activate, "Confirm", 0);
            confirmBtn.ButtonParameter = BUTTON_CONFIRM;
            Add(confirmBtn);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == BUTTON_CONFIRM)
            {
                List<int> selectedSpellIds = new List<int>();
                foreach (int spellId in _selectedSpellIds)
                {
                    selectedSpellIds.Add(spellId);
                }

                NetClient.Socket.Send_DnDLevelUpSubmit(World, _chosenClass, _chosenFeat, _abilityIncreases, selectedSpellIds);
                Dispose();
                return;
            }

            if (buttonID >= BUTTON_CLASS_BASE && buttonID < BUTTON_CLASS_BASE + _classes.Count)
            {
                int classIdx = buttonID - BUTTON_CLASS_BASE;
                var cls = _classes[classIdx];
                
                foreach (var kv in _classButtons)
                {
                    kv.Value.IsSelected = (kv.Key == buttonID && !kv.Value.IsSelected);
                }
                _chosenClass = _classButtons[buttonID].IsSelected ? cls.name : string.Empty;
            }
            else if (buttonID >= BUTTON_FEAT_BASE && buttonID < BUTTON_FEAT_BASE + _feats.Count)
            {
                int featIdx = buttonID - BUTTON_FEAT_BASE;
                var feat = _feats[featIdx];

                // Choosing a feat costs ASI
                foreach (var kv in _featButtons)
                {
                    kv.Value.IsSelected = (kv.Key == buttonID && !kv.Value.IsSelected);
                }
                
                if (_featButtons[buttonID].IsSelected)
                {
                    _chosenFeat = feat;
                    _asiSpent = 2; // Assuming feats cost 2 points (full ASI)
                    for(int i=0; i<6; i++) _abilityIncreases[i] = 0; // Clear stat boosts
                    UpdateASILabels();
                }
                else
                {
                    _chosenFeat = string.Empty;
                    _asiSpent = 0;
                    UpdateASILabels();
                }
            }
            else if (buttonID >= BUTTON_ASI_INC_BASE && buttonID < BUTTON_ASI_INC_BASE + 6)
            {
                if (!string.IsNullOrEmpty(_chosenFeat)) return; // Can't choose stat boost if feat is chosen
                
                int statIdx = buttonID - BUTTON_ASI_INC_BASE;
                if (_asiSpent < _pendingASI && _abilityIncreases[statIdx] < 2)
                {
                    _abilityIncreases[statIdx]++;
                    _asiSpent++;
                    UpdateASILabels();
                }
            }
            else if (buttonID >= BUTTON_ASI_DEC_BASE && buttonID < BUTTON_ASI_DEC_BASE + 6)
            {
                if (!string.IsNullOrEmpty(_chosenFeat)) return;

                int statIdx = buttonID - BUTTON_ASI_DEC_BASE;
                if (_abilityIncreases[statIdx] > 0)
                {
                    _abilityIncreases[statIdx]--;
                    _asiSpent--;
                    UpdateASILabels();
                }
            }
            else if (buttonID >= BUTTON_SPELL_BASE && buttonID < BUTTON_SPELL_BASE + _spells.Count)
            {
                int spellIdx = buttonID - BUTTON_SPELL_BASE;
                var spell = _spells[spellIdx];
                var btn = _spellButtons[buttonID];

                if (_selectedSpellIds.Contains(spell.Id))
                {
                    _selectedSpellIds.Remove(spell.Id);
                    _spellsSelected--;
                    btn.IsSelected = false;
                }
                else if (_spellsSelected < _pendingSpellsKnown)
                {
                    _selectedSpellIds.Add(spell.Id);
                    _spellsSelected++;
                    btn.IsSelected = true;
                }
                else
                {
                    btn.IsSelected = false; // enforce max
                }
                UpdateSpellLabels();
            }
        }

        private void UpdateASILabels()
        {
            if (_asiPointsLabel != null)
            {
                _asiPointsLabel.Text = $"Ability Score Increases (Pending: {_pendingASI - _asiSpent}) - Choose ASI OR Feats";
            }
            for (int i = 0; i < 6; i++)
            {
                if (_abilityIncreaseLabels[i] != null)
                {
                    _abilityIncreaseLabels[i].Text = $"+{_abilityIncreases[i]}";
                }
            }
        }

        private void UpdateSpellLabels()
        {
            if (_spellsPointsLabel != null)
            {
                _spellsPointsLabel.Text = $"Spells Known (Pending: {_pendingSpellsKnown - _spellsSelected})";
            }
        }
    }
}
