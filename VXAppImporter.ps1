# Pull Current DB to Check for existing games
$games = $PlayniteApi.Database.Games

function global:get_emuprofile_by_name($emu_name,$profile_name){
    $emu = $PlayniteApi.Database.Emulators | Where { $_.Name -eq $emu_name }
    return $emu.Profiles | Where {$_.Name -eq $profile_name}
}	

function global:process_vxapp($item){
        # We'll skip a directory if it doesn't look like a VXApp or if our vxapp.info doesn't exist
        if(!$item.EndsWith(".vxapp")){continue}
        $appinfo_path = $item + "\" + "vxapp.info"
        if(-not (Test-Path -LiteralPath $appinfo_path)){continue}
        
        # Read our VXApp Info file to get some config specifics
        try{
            $appinfo = Get-Content -LiteralPath $appinfo_path -Encoding UTF8 | ConvertFrom-Json
        }catch{
            $PlayniteApi.Dialogs.ShowMessage("Could not Parse VXApp.Info File at " + $appinfo_path)
            continue
        }

        # Skip importing duplicates
        $is_duplicate= 0
        foreach($g in $games){
            if($appinfo.Name -eq $g.Name){
                $is_duplicate = 1
            }
        }
        if($is_duplicate){continue}

		$game = New-Object "Playnite.SDK.Models.Game" $appinfo.Name
		
        # Set up our predefined asset paths and add them if we have them.
		$icon_path = Get-ChildItem -LiteralPath $item -Filter "icon.*"
        if($icon_path -And (Test-Path -LiteralPath $icon_path.FullName)){
            $game.Icon = $PlayniteApi.Database.AddFile($icon_path.FullName, $game.Id)
        }
		
		$cover_path = Get-ChildItem -LiteralPath $item -Filter "cover.*"
		if($cover_path -And (Test-Path -LiteralPath $cover_path.FullName)){
            $game.CoverImage = $PlayniteApi.Database.AddFile($cover_path.FullName, $game.Id)
        }
		
		$background_path = Get-ChildItem -LiteralPath $item -Filter "background.*" 
		if($background_path -And (Test-Path -LiteralPath $background_path.FullName)){
            $game.BackgroundImage = $PlayniteApi.Database.AddFile($background_path.FullName, $game.Id)
        }

        $platform = $PlayniteApi.Database.Platforms | Where { $_.Name -eq "PC (VXApp)" } 
        $loader = $PlayniteApi.Database.Emulators | Where { $_.Name -eq "V4P" }
        
        # Add our vxapp location and set up the "Play" and "Close" actions.
        $game.InstallDirectory = $item
        $game.IsInstalled = $true
	    $game.PlatformId = $platform.Id 
        $profile_run = get_emuprofile_by_name "V4P" "Run"
        $profile_close = get_emuprofile_by_name "V4P" "Close"
        $profile_suspend = get_emuprofile_by_name "V4P" "Suspend"
        $profile_resume = get_emuprofile_by_name "V4P" "Resume"
        $profile_clear = get_emuprofile_by_name "V4P" "Clear Cache"

		# Add our Metadata
        $game.Description = $appinfo.Description
		
        $appconfig_path = $item + "\" + "vxapp.config"
        if(-not (Test-Path -LiteralPath $appconfig_path)){continue}

        # Read our VXApp Info file to get some config specifics
        try{
            $app_config = Get-Content -LiteralPath $appconfig_path -Encoding UTF8 | ConvertFrom-Json
        }catch{
            $PlayniteApi.Dialogs.ShowMessage("Could not Parse VXApp.Config File at " + $appconfig_path)
            continue
        }

        $playTask = New-Object "Playnite.SDK.Models.GameAction"
        $playTask.Type = "Emulator"
        $playTask.Name = "Play"
        $playTask.EmulatorId = $loader.Id
        $playTask.EmulatorProfileId = $profile_run.Id
        $game.PlayAction =  $playTask
        

        $closeTask = New-Object "Playnite.SDK.Models.GameAction"
        $closeTask.Name = "Close App"
        $closeTask.Type = "Emulator"
        $closeTask.EmulatorId = $loader.Id
        $closeTask.EmulatorProfileId = $profile_close.Id
   
        $suspendTask = New-Object "Playnite.SDK.Models.GameAction"
        $suspendTask.Name = "Suspend App"
        $suspendTask.Type = "Emulator"
        $suspendTask.EmulatorId = $loader.Id
        $suspendTask.EmulatorProfileId = $profile_suspend.Id
 
        $resumeTask = New-Object "Playnite.SDK.Models.GameAction"
        $resumeTask.Name = "Resume App"
        $resumeTask.Type = "Emulator"
        $resumeTask.EmulatorId = $loader.Id
        $resumeTask.EmulatorProfileId = $profile_resume.Id

        $clearTask = New-Object "Playnite.SDK.Models.GameAction"
        $clearTask.Name = "Clear Saved Data"
        $clearTask.Type = "Emulator"
        $clearTask.EmulatorId = $loader.Id
        $clearTask.EmulatorProfileId = $profile_clear.Id

        $game.OtherActions += $closeTask
        $game.OtherActions += $suspendTask
        $game.OtherActions += $resumeTask
        $game.OtherActions += $clearTask

        # Finally, Parse the vxapp.info and read out all other entrypoints to add as actions.
        foreach ($config in $app_config){
            if($config.name -ne "default"){
                $addlplayTask = New-Object "Playnite.SDK.Models.GameAction"
                $addlplayTask.Type = "Emulator"
                $addlplayTask.AdditionalArguments = "config=`""+$config.name+"`""
                $addlplayTask.Name = $config.name
                $addlplayTask.EmulatorId = $loader.Id
                $addlplayTask.EmulatorProfileId = $loader.Profiles[0].Id
                $game.OtherActions.Add($addlplayTask)
            }
        }


        $PlayniteApi.Database.Games.Add($game)
    
}


function global:ImportVXApps{
    # Ask for our VXApp Root or App Root
    $selected_dir = $PlayniteApi.Dialogs.SelectFolder()
	if(!$selected_dir){
		$PlayniteApi.Dialogs.ShowMessage("VXApp Import Cancelled.")
        return
	}
    # Check for the VXApp Emulator Entry
    $VXAppLoader = $PlayniteApi.Database.Emulators | Where { $_.Name -eq "V4P" } | Select-Object -First 1
    if(!$VXAppLoader){
        $PlayniteApi.Dialogs.ShowMessage("Couldn't find V4P loader configuration in Playnite. Make sure you have V4P Loader configured.")
        return
    }
	
	if($selected_dir.EndsWith(".vxapp")){
		process_vxapp($selected_dir)
	}else{
		# Iterate each .vxapp directory and generate entries.
		$items = Get-ChildItem -Path $selected_dir -Directory -Force -ErrorAction SilentlyContinue | SELECT FullName
		foreach ($item in $items){
			process_vxapp($item)
		}
	}

	$PlayniteApi.Dialogs.ShowMessage("VXApp(s) Imported OK!")
}

#$PlayniteApi = (Get-Runspace)[-2].SessionStateProxy.GetVariable("PlayniteApi")

