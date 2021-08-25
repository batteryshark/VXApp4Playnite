function GetLocalTag([string] $path){
    $tag_name = Get-Content -LiteralPath $path
    if($tag_name -eq $null){
        return ""
    }
    return $tag_name
}

function SetTagName([string] $tag, [string] $path){
    $tag | Out-File -Force -LiteralPath $path
}

function GetRemoteTag([string] $user, [string] $repo){
    return (Invoke-WebRequest "https://api.github.com/repos/$user/$repo/releases" | ConvertFrom-Json)[0].tag_name
}

# Check for a new Version of the Release to Pull
function VersionCheck($local_tag_path, $user, $repo){
    if(Test-Path -LiteralPath $local_tag_path){
        $local_tag = GetLocalTag $local_tag_path
        $remote_tag = GetRemoteTag $user $repo
        Write-Output("Local: $local_tag Remote: $remote_tag")
        return $local_tag -ne $remote_tag
    }
    return $true
}

function DownloadLatestRelease([string] $target_path, [string] $user, [string] $repo, [string] $zipfile_name){
    New-Item -ItemType Directory -Force -Path $target_path
    # Check if there's an update
    $local_tag_path = Join-Path -Path $target_path -ChildPath "tag"
    $update_available = VersionCheck $local_tag_path $user $repo
    if(!$update_available){return $false}
    
    #Wipe out Our Directory
    #Remove-Item -LiteralPath $target_path -Recurse
    #New-Item -ItemType Directory -Force -Path $target_path

    # Get Recent Tag (Again)
    $remote_tag = GetRemoteTag $user $repo

    # Download Zip
    $zipfile_path = Join-Path -Path $target_path -ChildPath $zipfile_name
    Invoke-WebRequest "https://github.com/$user/$repo/releases/download/$remote_tag/$zipfile_name" -Out $zipfile_path
    Expand-Archive $zipfile_path -Force -DestinationPath $target_path
    Remove-Item -LiteralPath $zipfile_path
    SetTagName $remote_tag $local_tag_path
    return $true
}



