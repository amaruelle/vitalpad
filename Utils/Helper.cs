using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;

namespace Vitalpad.Utils;

public static class Helper
{
    public static ObservableCollection<TabViewItem> Tabs { get; set; }
    public static TabViewItem SelectedTab { get; set; }
}