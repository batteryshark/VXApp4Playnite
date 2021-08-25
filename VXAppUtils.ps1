$plugin_path = Join-Path -Path $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('.\') -ChildPath Extensions\VXApp
$repo_updater_path = Join-Path -Path $plugin_path -ChildPath RepoUpdater.ps1
$exporter_path = Join-Path -Path $plugin_path -ChildPath VXAppExporter.ps1
$emulator_path = Join-Path -Path $plugin_path -ChildPath "emulator\vxfe"
Import-Module $repo_updater_path
Import-Module $exporter_path



function global:InstallUpdates{
    if((DownloadLatestRelease "$plugin_path" "batteryshark" "VXAppUtils" "vxau.zip") -eq $false){
        $PlayniteApi.Dialogs.ShowMessage("VXAppUtils Plugin is Up-to-Date.")
    }else{
        $PlayniteApi.Dialogs.ShowMessage("VXAppUtils Plugin has been Updated!")
    }
    if((DownloadLatestRelease "$emulator_path" "batteryshark" "VXAppFE" "vxfe.zip") -eq $false){
        $PlayniteApi.Dialogs.ShowMessage("VXApp Emulator is Up-to-Date.")
    }else{
        $PlayniteApi.Dialogs.ShowMessage("VXApp Emulator has been Updated!")
    }
}

