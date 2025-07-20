using ChosenConcept.APFramework.UI;
using ChosenConcept.APFramework.UI.Element;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Menu;
using ChosenConcept.APFramework.UI.Window;
using Godot;
using System;

[GlobalClass]
public partial class SetupMenu : Node
{
    [Export]
    public CubeCredits Credits;
    [Export]
    public CubeMessageBus MessageBus;
    [Export]
    public LayoutSetup CreditsOverviewLayoutSetup;
    [Export]
    public LayoutSetup actionPanelLayoutSetup;

    private string _logCached;

    public override void _Ready()
    {
        SimpleMenu menu = new SimpleMenu(
            "Credits Overview",
            MenuSetup.defaultSetup,
            WindowSetup.defaultSetup,
            this.CreditsOverviewLayoutSetup);
        AddChild(menu);

        menu.AddText("Credits")
            .SetContent(new FunctionStringLabel(() => this.Credits.Value.ToString()));
        menu.OpenMenu();
        menu.ForceUpdateDisplayContent();
        WindowManager.instance.RegisterMenu(menu);

        SimpleMenu actionPanel = new SimpleMenu(
            "Action Panel",
            MenuSetup.defaultSetup,
            WindowSetup.defaultSetup,
            this.actionPanelLayoutSetup);
        AddChild(actionPanel);

        actionPanel.AddButton("Craft", () =>
        {
            if (this.Credits.Value < 10)
            {
                this.MessageBus.EmitSignal(nameof(CubeMessageBus.MessageReceived), "Not enough credits to craft!");
                return;
            }
            this.Credits.Value -= 10;
            this.MessageBus.EmitSignal(nameof(CubeMessageBus.MessageReceived), "Craft action triggered");
        });
        actionPanel.AddButton("Upgrade", () =>
        {
            if (this.Credits.Value < 20)
            {
                this.MessageBus.EmitSignal(nameof(CubeMessageBus.MessageReceived), "Upgrade action failed: Not enough credits");
                return;
            }
            this.Credits.Value -= 20;
            this.MessageBus.EmitSignal(nameof(CubeMessageBus.MessageReceived), "Upgrade action triggered");
        });
        actionPanel.AddText("Log")
            .SetContent(new FunctionStringLabel(() => this._logCached));
        this.MessageBus.MessageReceived += (message) =>
        {
            this._logCached = message;
        };
        actionPanel.OpenMenu();
        actionPanel.ForceUpdateDisplayContent();
        WindowManager.instance.RegisterMenu(actionPanel);
    }
}
