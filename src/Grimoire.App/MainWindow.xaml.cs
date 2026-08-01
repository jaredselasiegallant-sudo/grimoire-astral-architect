using System.IO;
using Microsoft.UI.Xaml;

namespace Grimoire.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 800));
    }
}
