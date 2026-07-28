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
    /// SelectInstallToPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectInstallToPage : Page
    {
        string vanillaDirectoryPath;
        GamePlatform platform;
        public SelectInstallToPage(string vanillaDirectoryPath, GamePlatform platform)
        {
            InitializeComponent();
            this.vanillaDirectoryPath = vanillaDirectoryPath;
            this.platform = platform;
        }

        private void ClickInstall(object sender, RoutedEventArgs e)
        {
            string? installTo = AUInstalling.GetModDirectoryPathFromVanilla(vanillaDirectoryPath);

            if(installTo != null)
            {
                MainWindow.Instance.OpenPage(new InstallingPage(vanillaDirectoryPath, installTo, platform));
            }
            else
            {
                MainWindow.Instance.OpenPage(new FailedPage());
            }
        }

        private void ClickCustom(object sender, RoutedEventArgs e)
        {
            string? installTo = AUInstalling.GetCopyToDirectoryPath();

            if (installTo != null)
            {
                MainWindow.Instance.OpenPage(new InstallingPage(vanillaDirectoryPath, installTo, platform));
            }
            else
            {
                MainWindow.Instance.OpenPage(new FailedPage());
            }
        }

        private void ClickPrev(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenPage(new SelectPlatformPage(vanillaDirectoryPath));
        }
    }
}
