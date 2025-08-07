using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace DotCalculator.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("DoT calculator###SaltedEarther99")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Config;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (Configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var flyTextEnabled = Configuration.FlyTextEnabled;
        if (ImGui.Checkbox("Display DoT damage as FlyText", ref flyTextEnabled))
        {
            Configuration.FlyTextEnabled = flyTextEnabled;
            Configuration.Save();
        }

        var printToChatEnabled = Configuration.PrintToChatEnabled;
        if (ImGui.Checkbox("Print DoT damage in chat", ref printToChatEnabled))
        {
            Configuration.PrintToChatEnabled = printToChatEnabled;
            Configuration.Save();
        }
    }
}
