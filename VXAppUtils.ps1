#Enter-PSHostProcess -Name Playnite.DesktopApp
#$PlayniteApi = (Get-Runspace)[-2].SessionStateProxy.GetVariable("PlayniteApi")
# Determine if Loader is Installed

$plugin_path = Join-Path -Path $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('.\') -ChildPath Extensions\VXApp4Playnite
$loader_path = Join-Path -Path $plugin_path -ChildPath "v4p"

function global:IsLoaderInstalled{
	    $res = $PlayniteApi.Database.Emulators | Where { $_.Name -eq "V4P" } | Select-Object -First 1
    if(!$res){
        return $false
    }
	return $true
}

function global:GetPlatform{
    return $PlayniteApi.Database.Platforms | Where { $_.Name -eq "PC (VXApp)" } | Select-Object -First 1
}



function global:Install{
    # If the loader is already installed, we won't do anything.
	if(IsLoaderInstalled -eq $true){
		$PlayniteApi.Dialogs.ShowMessage("VXApp4Playnite Loader already Installed.")
		return
	}

    # Check if Platform has been Created, Create if Necessary
    $platform = GetPlatform
    if($platform -eq $null){
        $n_platform = New-Object "Playnite.SDK.Models.Platform" "PC (VXApp)"
        $PlayniteApi.Database.Platforms.Add($n_platform)
        $platform = GetPlatform
    }
    
    $platform_id = $platform.Id
    
	$vxlauncher_path = Join-Path -Path $loader_path -ChildPath VXLauncher.exe
	$vxctrl_path = Join-Path -Path $loader_path -ChildPath VXCtrl.exe
	
	$loader = New-Object "Playnite.SDK.Models.Emulator" "V4P"
    $profile_play = New-Object "Playnite.SDK.Models.EmulatorProfile"
    $profile_play.Name = "Run"
    $profile_play.WorkingDirectory = $loader_path
    $profile_play.Executable = $vxlauncher_path
    $profile_play.Arguments = "`"{InstallDir}`""
    $profile_play.Platforms = $platform.Id
    $profile_play.ImageExtensions = "vxapp"
    $loader.Profiles += $profile_play

    $profile_close = New-Object "Playnite.SDK.Models.EmulatorProfile"
    $profile_close.Name = "Close"
    $profile_close.WorkingDirectory = $loader_path
    $profile_close.Executable = $vxlauncher_path
    $profile_close.Arguments = "`"{InstallDir}`" cmd=CLEANUP"
    $profile_close.Platforms = $platform.Id
    $profile_close.ImageExtensions = "vxapp"
    $loader.Profiles += $profile_close

    $profile_clear = New-Object "Playnite.SDK.Models.EmulatorProfile"
    $profile_clear.Name = "Clear Cache"
    $profile_clear.WorkingDirectory = $loader_path
    $profile_clear.Executable = $vxlauncher_path
    $profile_clear.Arguments = "`"{InstallDir}`" cmd=CLEARCACHE"
    $profile_clear.Platforms = $platform.Id
    $profile_clear.ImageExtensions = "vxapp"
    $loader.Profiles += $profile_clear

    $profile_suspend = New-Object "Playnite.SDK.Models.EmulatorProfile"
    $profile_suspend.Name = "Suspend"
    $profile_suspend.WorkingDirectory = $loader_path
    $profile_suspend.Executable = $vxctrl_path
    $profile_suspend.Arguments = "`"{InstallDir}`" SUSPEND"
    $profile_suspend.Platforms = $platform.Id
    $profile_suspend.ImageExtensions = "vxapp"
    $loader.Profiles += $profile_suspend

    $profile_resume = New-Object "Playnite.SDK.Models.EmulatorProfile"
    $profile_resume.Name = "Resume"
    $profile_resume.WorkingDirectory = $loader_path
    $profile_resume.Executable = $vxctrl_path
    $profile_resume.Arguments = "`"{InstallDir}`" RESUME"
    $profile_resume.Platforms = $platform.Id
    $profile_resume.ImageExtensions = "vxapp"
    $loader.Profiles += $profile_resume

	$PlayniteApi.Database.Emulators.Add($loader)
    $PlayniteApi.Dialogs.ShowMessage("VXApp4Playnite Loader Installed!")
}
