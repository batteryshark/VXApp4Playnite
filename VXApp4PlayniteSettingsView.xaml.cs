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

namespace VXApp4Playnite
{
    public partial class VXAppPluginSettingsView : UserControl
    {
        VXApp4Playnite plugin;
        public VXAppPluginSettingsView(VXApp4Playnite np)
        {
            this.plugin = np;
            InitializeComponent();
        }

        private void btn_pick_app_path_Click(object sender, RoutedEventArgs e)
        {
            tpath_app.Text = this.plugin.PlayniteApi.Dialogs.SelectFolder();
            if(tpath_app.Text == "")
            {
                tpath_app.Text = this.plugin.settings.Settings.fallback_app_path;
                
            }
        }

        private void btn_pick_tmp_path_Click(object sender, RoutedEventArgs e)
        {
            tpath_tmp.Text = this.plugin.PlayniteApi.Dialogs.SelectFolder();
            if (tpath_tmp.Text == "")
            {
                tpath_tmp.Text = this.plugin.settings.Settings.fallback_tmp_path;
               

            }
        
        }

        private void btn_pick_save_path_Click(object sender, RoutedEventArgs e)
        {
            tpath_save.Text = this.plugin.PlayniteApi.Dialogs.SelectFolder();
            if (tpath_save.Text == "")
            {
                tpath_save.Text = this.plugin.settings.Settings.fallback_save_path;
                


            }
        }
    }
}