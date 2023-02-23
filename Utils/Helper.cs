using System.Collections.Generic;
using System.Collections.ObjectModel;
using ABI.Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using StorageFile = Windows.Storage.StorageFile;

namespace Vitalpad.Utils;

public static class Helper
{
    public static ObservableCollection<TabViewItem> Tabs { get; set; }
    public static Dictionary<StorageFile, string> ActiveFiles { get; set; }
    public static Dictionary<TabViewItem, KeyValuePair<StorageFile, string>> Active { get; set; }
}