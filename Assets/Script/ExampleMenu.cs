using ChosenConcept.APFramework.UI;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Menu;
using ChosenConcept.APFramework.UI.Window;
using Godot;

public partial class ExampleMenu : SimpleMenu
{
    public ExampleMenu() : base("Example Menu", MenuSetup.defaultSetup, WindowSetup.defaultSetup, LayoutSetup.defaultLayout)
    {
    }

    // protected override void InitializeMenu()
    // {
    //     LayoutAlignment layout = InitNewLayout();

    //     AddButton("Multiple window example (Horizontal)", layout, OpenSubMenu<MultipleWindowHorizontal>);
    //     AddButton("Multiple window example (Vertical)", layout, OpenSubMenu<MultipleWindowVertical>);
    //     AddButton("Chinese 中文顯示", layout, OpenSubMenu<ChineseDisplay>);
    //     AddButton("Code Initialized SimpleMenus", layout, () =>
    //     {
    //         CloseMenu(false);
    //         GetViewport().GetNode<SimpleMenuInstance>("SimpleMenuInstance").OpenMenu();
    //     });
    //     AddButton("Quit", layout, Quit);
    // }

    void Quit()
    {
        GetTree().Quit();
    }
}