# Get List of All Loaded VXApp Games in your Database
function global:get_vxapp_games{
    $vxapp_emu = $PlayniteApi.Database.Emulators | Where { $_.Name -eq "VXApp" } | Select-Object -First 1
    if(!$vxapp_emu){
        $PlayniteApi.Dialogs.ShowMessage("Couldn't find VXApp emulator configuration in Playnite. Make sure you have VXApp emulator configured.")
        return
    }
    
    return $PlayniteApi.Database.Games | Where {$_.PlayAction.EmulatorId -eq $vxapp_emu.Id}
}

function global:export_meta($game){
    # Export our VXApp Info Metadata
    $vxapp_info_path = Join-Path -Path $game.InstallDirectory -ChildPath "vxapp.info"
    $developer = ""
    $publisher = ""
    $series = ""
    $features = ""
    if($game.Developers){
        $developer = $game.Developers[0].Name
    }
    if($game.Publishers){
        $publisher = $game.Publishers[0].Name
    }
    if($game.Series){
        $series = $game.Series[0].Name
    }
    if($game.features){
        $features = $game.features[0].Name
    }

    $appinfo = @{
        Name = $game.Name
        Description = $game.Description
        Region = $game.Region
        ReleaseYear = $game.ReleaseYear
        Series = $series
        Developer = $developer
        Publisher = $publisher
        Features = $features

    }

    $appinfo | ConvertTo-Json -Depth 4 | Out-File -LiteralPath $vxapp_info_path

    # Export Cover Image
	if($game.CoverImage){
		$cover_image_src = $PlayniteApi.Database.GetFullFilePath($game.CoverImage)
		if(Test-Path -LiteralPath $cover_image_src){
			$image_dest = Join-Path -Path $game.InstallDirectory -ChildPath "cover"
			$image_dest += [System.IO.Path]::GetExtension($cover_image_src)
			Copy-Item -Force -LiteralPath $cover_image_src -Destination $image_dest
		}
	}
    
	# Export Icon
	if($game.Icon){
		$icon_image_src = $PlayniteApi.Database.GetFullFilePath($game.Icon)
		if(Test-Path -LiteralPath $icon_image_src){
			$image_dest = Join-Path -Path $game.InstallDirectory -ChildPath "icon"
			$image_dest += [System.IO.Path]::GetExtension($icon_image_src)
			Copy-Item -Force -LiteralPath $icon_image_src -Destination $image_dest
		}			
	}
    
	# Export Background Image
	if($game.BackgroundImage){
		$background_image_src = $PlayniteApi.Database.GetFullFilePath($game.BackgroundImage)
		if(Test-Path -LiteralPath $background_image_src){
			$image_dest = Join-Path -Path $game.InstallDirectory -ChildPath "background"
			$image_dest += [System.IO.Path]::GetExtension($background_image_src)
			Copy-Item -Force -LiteralPath $background_image_src -Destination $image_dest
		}
	}
}

function global:VXAppExport{
    if($PlayniteApi.MainView.SelectedGames.Count -lt 1){return}
	$PlayniteApi.Dialogs.ShowMessage("If you want to just export metadata to your VXApps, cancel the following directory selection.")
    # Ask for our VXApp Root
    $apps_folder = $PlayniteApi.Dialogs.SelectFolder()
	
    $vxapp_games = $PlayniteApi.MainView.SelectedGames

    foreach($game in $vxapp_games){
        Write-Output("Processing: " + $game.Name)
        export_meta($game)
		if($apps_folder){
			$output_path = Join-Path -Path $apps_folder -ChildPath (Split-Path -Leaf $game.InstallDirectory)
			Copy-Item -Force -Recurse -LiteralPath $game.InstallDirectory -Destination $output_path
		}
		
    }
	if($apps_folder){
		$PlayniteApi.Dialogs.ShowMessage("VXApp(s) Exported OK!")
	}else{
		$PlayniteApi.Dialogs.ShowMessage("VXApp(s) Metadata Exported OK!")
	}
	
}