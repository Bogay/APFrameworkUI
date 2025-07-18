using System.Collections.Generic;
using System.Diagnostics;
using ChosenConcept.APFramework.UI;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Menu;
using ChosenConcept.APFramework.UI.Window;
using Godot;

[GlobalClass]
public partial class SimpleMenuInstance : Node
{
    [Export] Godot.Collections.Array<LayoutSetup> _layoutSetups = new();
    Godot.Collections.Array<SimpleMenu> _simpleMenus = new();
    bool _active;

    public override void _Ready()
    {
        int i = 0;
        foreach (LayoutSetup layout in _layoutSetups)
        {
            MenuSetup setup = MenuSetup.defaultSetup;
            Debug.Assert(layout != null, "LayoutSetup cannot be null");
            setup.SetAllowCloseMenuWithCancelAction(true);
            SimpleMenu menu = new(i.ToString(), setup, WindowSetup.defaultSetup, layout);
            _simpleMenus.Add(menu);
            AddChild(menu);
            menu.AddText("Close all menu to quit");
            menu.AddSingleSelection("Test", obj => { })
                .SetChoiceByValue(new List<string> { "1", "2", "3" });
            menu.AddSlider("slider")
                .SetChoiceByValue(new[] { "0", "1", "2", "3" })
                .SetAction(x => GD.Print(x));
            menu.AddButton("close", () => menu.CloseMenu());
            WindowManager.instance.RegisterMenu(menu);
            i++;
        }

        foreach (SimpleMenu menu in _simpleMenus)
        {
            menu.OpenMenu();
            menu.ForceUpdateDisplayContent();
        }
    }

    public override void _Process(double delta)
    {
        if (!_active)
            return;
        bool any = false;
        foreach (SimpleMenu x in _simpleMenus)
        {
            if (x.isDisplayActive)
            {
                any = true;
                break;
            }
        }
        if (!any)
        {
            _active = false;
            WindowManager.instance.GetMenu<ExampleMenu>().OpenMenu(true);
        }
    }

    public void OpenMenu()
    {
        _active = true;
        foreach (SimpleMenu menu in _simpleMenus)
        {
            menu.OpenMenu(true);
        }
    }
}