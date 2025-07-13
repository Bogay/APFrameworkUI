using System.Collections.Generic;
using ChosenConcept.APFramework.UI;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Menu;
using ChosenConcept.APFramework.UI.Window;
using Godot;

public partial class SimpleMenuInstance : Node
{
    [Export] List<LayoutSetup> _layoutSetups = new();
    [Export] List<SimpleMenu> _simpleMenus = new();
    bool _active;

    public override void _Ready()
    {
        int i = 0;
        foreach (LayoutSetup layout in _layoutSetups)
        {
            MenuSetup setup = MenuSetup.defaultSetup;
            setup.SetAllowCloseMenuWithCancelAction(true);
            SimpleMenu menu = new(i.ToString(), setup, WindowSetup.defaultSetup, layout);
            _simpleMenus.Add(menu);
            menu.AddText("Close all menu to quit");
            menu.AddSingleSelection<int>("Test", obj => { })
                .SetChoiceByValue(new List<int> { 1, 2, 3 });
            menu.AddSlider<int>("slider")
                .SetChoiceByValue(new[] { 0, 1, 2, 3 })
                .SetAction(x => GD.Print(x));
            menu.AddButton("close", () => menu.CloseMenu());
            WindowManager.instance.RegisterMenu(menu);
            i++;
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