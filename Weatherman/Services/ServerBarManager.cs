using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.Sheets;

namespace Weatherman.Services;
public unsafe class ServerBarManager : IDisposable
{
    IDtrBarEntry Entry;
    private ServerBarManager()
    {
        Reload();
    }

    public void Reload()
    {
        Svc.Framework.Update -= OnUpdate;
        Entry?.Remove();
        Entry = null;
        if(C.DTRBarEnable)
        {
            Svc.Framework.Update += OnUpdate;
            Entry = Svc.DtrBar.Get("Weatherman", "");
            Entry.Shown = true;
            Entry.OnClick = OnClick;
            UpdateText();
        }
    }

    public void OnClick()
    {
        if(C.DTRBarClickToggle)
        {
            P.PausePlugin = !P.PausePlugin;
        }
        else
        {
            P.ConfigGui.configOpen = true;
        }
    }

    public void UpdateText()
    {
        var text = Svc.Data.GetExcelSheet<Weather>().GetRowOrDefault(C.DTRBarRealAlways? *P.memoryManager.TrueWeather:P.memoryManager.GetDisplayedWeather())?.Name.ExtractText() ?? "Unknown";
        if(P.memoryManager.IsWeatherCustom()) 
        {
            Entry.Text = new SeStringBuilder().AddUiForeground(text, 31).Build();
        }
        else
        {
            Entry.Text = text;
        }
    }

    (int Displayed, int True) LastState = default;
    void OnUpdate(object f)
    {
        var newState = (P.memoryManager.GetDisplayedWeather(), *P.memoryManager.TrueWeather);
        if(newState != LastState)
        {
            PluginLog.Debug($"State updated");
            LastState = newState;
            UpdateText();
        }
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
        Entry?.Remove();
        Entry = null;
    }
}
