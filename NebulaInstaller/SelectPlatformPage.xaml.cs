using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NebulaInstaller
{
    /// <summary>
    /// SelectPlatformPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectPlatformPage : Page
    {
        string vanillaDirectoryPath;
        public SelectPlatformPage(string vanillaDirectoryPath)
        {
            InitializeComponent();
            this.vanillaDirectoryPath = vanillaDirectoryPath;

            if (AUInstalling.GuessPlatform(vanillaDirectoryPath) == GamePlatform.Epic)
                EpicRadioButton.IsChecked = true;
            else
                SteamRadioButton.IsChecked = true;
        }

        private void ClickNext(object sender, RoutedEventArgs e)
        {
            GamePlatform platform = EpicRadioButton.IsChecked == true ? GamePlatform.Epic : GamePlatform.Steam;
            MainWindow.Instance.OpenPage(new SelectInstallToPage(vanillaDirectoryPath, platform));
        }

        private void ClickPrev(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenPage(new SelectVanillaPage());
        }
    }
}
