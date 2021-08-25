$plugin_path = Join-Path -Path $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('.\') -ChildPath Extensions\VXApp4Playnite
$repo_updater_path = Join-Path -Path $plugin_path -ChildPath RepoUpdater.ps1
$exporter_path = Join-Path -Path $plugin_path -ChildPath VXAppExporter.ps1
$importer_path = Join-Path -Path $plugin_path -ChildPath VXAppImporter.ps1
$utils_path = Join-Path -Path $plugin_path -ChildPath VXAppUtils.ps1
$loader_path = Join-Path -Path $plugin_path -ChildPath "v4p"
Import-Module $repo_updater_path
Import-Module $exporter_path
Import-Module $importer_path
Import-Module $utils_path



function global:UpdateExtension{
    if((DownloadLatestRelease "$plugin_path" "batteryshark" "VXApp4Playnite" "VXApp4Playnite.zip") -eq $false){
        $PlayniteApi.Dialogs.ShowMessage("VXApp4Playnite Extension is Up-to-Date.")
    }else{
        $PlayniteApi.Dialogs.ShowMessage("VXApp4Playnite Extension has been Updated!")
    }
    if((DownloadLatestRelease "$loader_path" "batteryshark" "V4P" "v4p.zip") -eq $false){
        $PlayniteApi.Dialogs.ShowMessage("VXApp4Playnite Loader is Up-to-Date.")
    }else{
        $PlayniteApi.Dialogs.ShowMessage("VXApp4Playnite Loader has been Updated!")
    }
}

