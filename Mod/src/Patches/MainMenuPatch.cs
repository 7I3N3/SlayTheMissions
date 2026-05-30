using System;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

using SlayTheMissions.Core;

namespace SlayTheMissions.Patches;

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
public static class MainMenuPatch
{
    private static CheckBox _normalModeCheckbox;
    private static CheckBox _competitionModeCheckbox;

    private static VBoxContainer _competitionPanel;

    private static LineEdit _codeInput;
    private static LineEdit _nameInput;

    private static Button _confirmButton;
    private static Button _leaveButton;

    private static NButton _singleplayerButton;
    private static NButton _multiplayerButton;

    private static Label _statusLabel;

    private static bool _sessionRestoreAttemped = false;

    public static event Action<bool> ModeToggled;

    [HarmonyPostfix]
    public static void Postfix(NMainMenu __instance)
    {
        AddModeSelectionUI(__instance);

        CompetitionManager.Joined += OnJoined;
        CompetitionManager.Left += OnLeft;
        CompetitionManager.Kicked += OnKicked;

        ApplyInitialState();
    }

    private static void AddModeSelectionUI(NMainMenu mainMenu)
    {
        var singleplayerField = AccessTools.Field(typeof(NMainMenu), "_singleplayerButton");
        var multiplayerField = AccessTools.Field(typeof(NMainMenu), "_multiplayerButton");
        _singleplayerButton = singleplayerField.GetValue(mainMenu) as NButton;
        _multiplayerButton = multiplayerField.GetValue(mainMenu) as NButton;

        var container = new VBoxContainer
        {
            Name = "SlayTheMissionsPanel",
            Position = new(20, 400),
            CustomMinimumSize = new(300, 0),
        };
        mainMenu.AddChild(container);

        var modeLabel = new Label
        {
            Text = "게임 모드 선택",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        container.AddChild(modeLabel);

        _normalModeCheckbox = new CheckBox
        {
            Text = " 일반 모드",
            ButtonPressed = true,
            Name = "NormalModeCheck",
        };
        container.AddChild(_normalModeCheckbox);

        _competitionModeCheckbox = new CheckBox
        {
            Text=  " 대회 모드" ,
            ButtonPressed = false,
            Name = "CompetitionMode",
        };
        container.AddChild(_competitionModeCheckbox);

        _competitionPanel = new VBoxContainer
        {
            Name = "CompetitionPanel",
            Visible = false,
        };
        container.AddChild(_competitionPanel);

        var nameLabel = new Label
        {
            Text = "닉네임",
        };
        _competitionPanel.AddChild(nameLabel);

        _nameInput = new LineEdit
        {
            Text = SlayTheMissionsMode.PlayerName,
            PlaceholderText = "닉네임을 입력하세요...",
            Name = "CompetitionNameInput",
            MaxLength = 20,
        };
        _competitionPanel.AddChild(_nameInput);

        var codeLabel = new Label
        {
            Text = "코드 입력",
        };
        _competitionPanel.AddChild(codeLabel);

        _codeInput = new LineEdit
        {
            PlaceholderText = "코드를 입력하세요...",
            Name = "CompetitionCodeInput",
            MaxLength = 8,
        };
        _competitionPanel.AddChild(_codeInput);

        _confirmButton = new Button
        {
            Text = "참가",
            Name = "CompetitionJoinButton"
        };
        _competitionPanel.AddChild(_confirmButton);

        _leaveButton = new Button
        {
            Text = "나가기",
            Name = "CompetitionLeaveButton",
            Visible = false,
        };
        _competitionPanel.AddChild(_leaveButton);

        _statusLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            Name = "CompetitionStatusLabel",
            Visible = false,
        };
        _competitionPanel.AddChild(_statusLabel);

        _normalModeCheckbox.Toggled += OnNormalModeToggled;
        _competitionModeCheckbox.Toggled += OnCompetitionModeToggled;

        _nameInput.TextChanged += OnNameChanged;

        _confirmButton.Pressed += OnJoinPressed;
        _leaveButton.Pressed += OnLeavePressed;
    }

    private static void OnJoined()
    {
        Callable.From(() =>
        {
            UpdateJoinedUI();
        }).CallDeferred();
    }

    private static void OnLeft()
    {
        Callable.From(() =>
        {
            ShowJoinedUI();
        }).CallDeferred();
    }

    private static void OnKicked(string reason)
    {
        Callable.From(() =>
        {
            SlayTheMissionsMode.Reset();

            _normalModeCheckbox.ButtonPressed = true;
            _competitionModeCheckbox.ButtonPressed = false;
            _competitionPanel.Visible = false;
            _leaveButton.Visible = false;
            _statusLabel.Visible = false;
            _codeInput.Text = "";
            _confirmButton.Text = "참가";
            _confirmButton.Disabled = false;
            _confirmButton.Visible = true;
            _nameInput.Editable = true;

            SetGameModeButtonsEnabled(true);
            ShowPopup(_confirmButton.GetParent(), $"강퇴괴었습니다.\n\n사유 : {reason}");

        }).CallDeferred();
    }

    private static void OnNormalModeToggled(bool toggledOn)
    {
        if (toggledOn)
        {
            if (SlayTheMissionsMode.HasJoinedCompetition)
            {
                _normalModeCheckbox.ButtonPressed = false;
                _competitionModeCheckbox.ButtonPressed = true;
                ShowPopup(_normalModeCheckbox.GetParent(), "대회에 참가중입니다.\n먼저 '나가기' 버튼을 눌러주세요.");
                return;
            }

            _competitionModeCheckbox.ButtonPressed = false;
            _competitionPanel.Visible = false;
            SlayTheMissionsMode.IsCompetitionMode = false;
            SetGameModeButtonsEnabled(true);
            ModeToggled?.Invoke(true);
        }
        else
        {
            _competitionModeCheckbox.ButtonPressed = true;
        }
    }
    private static void OnCompetitionModeToggled(bool toggledOn)
    {
        if (toggledOn)
        {
            if (SlayTheMissionsMode.HasJoinedCompetition)
            {
                _normalModeCheckbox.ButtonPressed = false;
                _competitionPanel.Visible = true;
                UpdateJoinedUI();
                return;
            }

            _competitionModeCheckbox.ButtonPressed = false;
            _competitionPanel.Visible = true;
            SlayTheMissionsMode.IsCompetitionMode = true;
            SetGameModeButtonsEnabled(false);
            ShowJoinedUI();
            ModeToggled?.Invoke(false);
        }
        else
        {
            if (!SlayTheMissionsMode.HasJoinedCompetition)
            {
                _normalModeCheckbox.ButtonPressed = true;
                _competitionPanel.Visible = false;
            }
        }
    }

    private static void OnJoinPressed()
    {
        string code = _codeInput.Text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            ShowPopup(_confirmButton.GetParent(), "코드를 입력해주세요.");
            return;
        }

        _confirmButton.Disabled = true;
        _confirmButton.Text = "참가중...";

        _ = JoinAsync(code);
    }

    private static async Task JoinAsync(string code)
    {
        bool success = await CompetitionManager.JoinAsync(code);

        Callable.From(() =>
        {
            _confirmButton.Disabled = false;
            _confirmButton.Text = "참가";

            if (!success)
            {
                ShowPopup(_confirmButton.GetParent(), "대회 참가 실패");
            }
        }).CallDeferred();
    }

    private static void OnLeavePressed()
    {
        _ = CompetitionManager.LeaveAsync();
    }

    private static void OnNameChanged(string text)
    {
        string cleaned = SlayTheMissionsMode.SanitizeName(text);
        if (cleaned != text)
        {
            _nameInput.Text = cleaned;
            _nameInput.CaretColumn = cleaned.Length;
        }

        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            SlayTheMissionsMode.SaveName(cleaned);
        }
    }

    private static void UpdateJoinedUI()
    {
        _confirmButton.Visible = false;
        _leaveButton.Visible = true;

        _statusLabel.Visible = true;

        _statusLabel.Text = $"참가중\n코드 : {SlayTheMissionsMode.CompetitionCode}";

        _codeInput.Editable = false;
        _nameInput.Editable = false;

        SetGameModeButtonsEnabled(false);
    }

    private static void ShowJoinedUI()
    {
        _confirmButton.Visible = true;
        _leaveButton.Visible = false;

        _statusLabel.Visible = false;

        _codeInput.Editable = true;
        _nameInput.Editable = true;

        SetGameModeButtonsEnabled(true);
    }

    private static void ApplyInitialState()
    {
        _competitionPanel.Visible = false;
        ShowJoinedUI();
    }

    private static void SetGameModeButtonsEnabled(bool enabled)
    {
        _singleplayerButton?.SetEnabled(enabled);
        _multiplayerButton?.SetEnabled(enabled);
    }

    private static void ShowPopup(Node parent, string msg)
    {
        if (parent == null) return;

        var existing = parent.GetNodeOrNull<AcceptDialog>("SlayTheMissionsPopup");
        existing?.QueueFree();

        var dialog = new AcceptDialog
        {
            Name = "SlayTheMissionsPopup",
            Title = "SlayTheMissions",
            DialogText = msg,
            Size = new(400, 200),
            OkButtonText = "확인",
        };

        parent.AddChild(dialog);
        dialog.PopupCentered();
    }
}