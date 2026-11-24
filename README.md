# VXApp4Playnite

VXApp4Playnite (v4p) is a Playnite plugin that lists `.vxapp` folders as a game
library and launches them through the VX tools.

**Status: a source snapshot from 2021, when it was built and used on Windows. It has not been rebuilt since, and two
pieces are not in the tree: the updater's download step and the plugin's
`extension.yaml`. See [Status and limits](#status-and-limits).**

This is the frontend piece from
[Part Six of the Masterpiece write-up](https://batteryshark.github.io/2021/10/25/masterpiece-slapping-on-a-frontend-and-wrapping-it-up-part-six.html).
The screenshots are over there.

## Why it exists

The vxapp format packs a PC game and its dependencies into one portable
folder, and the VX tools run it inside an isolation layer. That gets you an app
you can launch from a command line. The goal was something that feels like a
console.

A frontend needs a library manager, graphics, controller support and metadata
handling, which is most of a game engine. [Playnite](https://playnite.link/)
already has all of that, plus a plugin API. So instead of writing a frontend, I
wrote the part that teaches Playnite what a `.vxapp` is. It replaced an earlier
script extension, VXAppImporter, which was a single import function.

## What it does

- Reads one or more "repositories", folders that hold `.vxapp` folders, and
  keeps the Playnite library in step with them. With Realtime Refresh on, it
  rescans once a minute while nothing is running. There is a Refresh Library
  menu item as well.
- Install and uninstall: copy an app from its repository to a local directory
  for faster loading or offline play, and remove it again. Installed apps get
  an `OnDevice` tag.
- Suspend, resume and close a running app.
- Clear an app's saved data, or open its save directory.
- A backsplash: a fullscreen window showing the game's background image while
  the app loads.
- Push and pull metadata (name, description, scores, genres, cover, background
  and so on) between Playnite and the app folder, so it travels with the app.
- Self-update of the plugin and the tools it drives: only partly here. See
  below.

## How it works

Every folder ending in `.vxapp` inside a repository path becomes a Playnite
game on a `PC (VXApp)` platform, as long as it has a `vxapp.config` (a JSON
list of launch configurations). The Play action runs
`{PlayniteDir}\v4p\tools\VXLauncher.exe "{InstallDir}" config="<first config>"`,
and every configuration also gets its own named action. If the folder has a
`vxapp.info`, the name, description, scores, genres and the rest come from
there, and files named `background` and `cover` become the artwork.

The other controls are game actions that point at
`playnite://vxctrl/<verb>/<game id>`, which the plugin registers a handler for:
`install`, `uninstall`, `suspend`, `resume`, `close`, `clearcache` and
`opensave`. Suspend, resume and close write `SUSPEND`, `RESUME` or `SHUTDOWN`
to a named pipe called `VX_<app code>`, where VXLauncher's watchdog is
listening. The app code is nine characters (`XXX-XXX-XXX`) derived from the
SHA-1 of the folder name, so both sides arrive at the same pipe name without
talking first. Force Close skips the pipe and runs VXLauncher with
`cmd=CLEANUP`.

## What is in here

| Path | What it is |
|---|---|
| `VXApp4Playnite.cs` | The plugin: library refresh, the `vxctrl` handler, menus, backsplash hooks |
| `PlayniteUtils.cs` | Import, install and uninstall, `vxapp.info` import and export |
| `Utils.cs` | App code, pipe client, file helpers |
| `VXApp4PlayniteSettings.cs`, `VXApp4PlayniteSettingsView.xaml` | Settings and the settings page |
| `BackSplash.*` | The WinForms splash window |
| `V4PUpdater/` | A second, tiny plugin that swaps a downloaded update into place |

## Build

It is a .NET Framework 4.6.2 class library (WPF and WinForms), so you need
Windows with Visual Studio 2019 or the Build Tools, the 4.6.2 targeting pack,
and `nuget.exe`.

```bat
nuget restore VXApp4Playnite.sln
msbuild VXApp4Playnite.sln /p:Configuration=Release
msbuild V4PUpdater\V4PUpdater.csproj /p:Configuration=Release
```

Both projects write to `extension\`. V4PUpdater is not part of the solution, so
it builds on its own line.

These are the intended steps, not a tested recipe. The last build was in 2021,
against a checked-in copy of `Playnite.SDK.dll` 6.1.0.0, while `packages.config`
still pinned PlayniteSDK 5.5.0. For this release the checked-in binaries were
removed, the pin moved to 6.1.0 and the references now point into the NuGet
`packages\` folder. Nobody has built it since that change.

## Install

There is no `extension.yaml` in the source, and Playnite won't load a plugin
without one. Something like this should do it. It is untested:

```yaml
Id: VXApp4Playnite_b9e83df4-c46b-4877-9291-ba742c9eb142
Name: VXApp4Playnite
Author: BatteryShark
Version: 1.0
Module: VXApp4Playnite.dll
Type: GenericPlugin
```

The plugin builds its own paths from Playnite's application folder, so it
expects a portable Playnite install and this folder name:

```text
{PlayniteDir}\Extensions\VXApp4Playnite_b9e83df4-c46b-4877-9291-ba742c9eb142\
```

Put the manifest there along with the build output from `extension\`. Leave
out `Playnite.SDK.dll`, which Playnite supplies, and `V4PUpdater.dll`, which
only exists for the update swap. The main plugin deletes it on start anyway.

The plugin doesn't launch anything by itself. It expects the VX tools under
`{PlayniteDir}\v4p\tools`: a VXTools.NET build (`VXLauncher.exe` and friends),
Smoothie.NET, and the pdx libraries in `tools\pdxplugins`. The updater was
supposed to fetch those. In this snapshot you place them by hand.

Then open the plugin's settings and fill in:

- **Paths to App Repos**: one or more folders, separated by semicolons.
- **Local App, Tmp and Save directories**: default to `app`, `tmp` and `save`
  under `{PlayniteDir}\v4p`.
- **Backsplash**, **Realtime Refresh** (needs a restart) and **Debug Mode**.

## What it changes on your machine

Worth knowing before you load it:

- It sets `VXPATH_APP`, `VXPATH_TMP` and `VXPATH_SAVE` as user environment
  variables, every time Playnite starts and every time you save settings. Debug
  Mode sets `PDXDBG=1` the same way, and turning it off removes the variable.
  These are how the VX tools find their directories. They are user-level, so
  they stay behind after Playnite closes and after the plugin is removed.
- It creates `app`, `tmp` and `save` under `{PlayniteDir}\v4p`.
- It adds a `PC (VXApp)` platform and an `OnDevice` tag to the Playnite
  database, and removes library entries whose `.vxapp` folder has gone away.
- Push Repository Metadata writes `vxapp.info`, `background` and `cover` into
  the app's folder in the repository.

## Status and limits

- Built and used on Windows in 2021; not rebuilt since, and not rebuilt for this release. It targeted Playnite 9
  (SDK 6.1.0). Newer Playnite versions are untried.
- The updater's download step is not in this snapshot. The write-up lists
  self-updating as a feature, and the last compiled build had it: a class that
  asked GitHub for each source's latest release and downloaded it. Its source
  didn't make it into this tree. What is here is everything around it: the
  default list of release sources in the settings
  (`batteryshark/pdx`, `VXTools.NET`, `Smoothie.NET` and this repo), the
  cleanup after an update, and V4PUpdater, which copies `v4p\update` over the
  plugin folder and closes Playnite. V4PUpdater carries the same plugin Id as
  the main plugin, which is how it finds the folder to replace. The About box
  lists a version tag per source, and without the downloader those stay blank.
- No `extension.yaml`, as above.
- The library refresh reads `PlatformIds[0]` and `Roms[0]` on every game in the
  database without checking for null, so it expects a library where every entry
  has both. In practice that means a Playnite install kept for vxapps.
- The install copy skips any file it fails to copy and says nothing. The source
  marks it `HACK`.
- Windows only. The plugin is an AnyCPU managed DLL and goes wherever Playnite 9
  and .NET Framework 4.6.2 go; the source doesn't record which Windows versions
  it ran on. The x86 and x64 handling of the apps themselves belongs to the VX
  tools.
- No tests.

## Related pieces

Each of these is its own repository under `github.com/batteryshark`:

- **VXTools.NET**: the managed launcher, watchdog and bootstrap this plugin
  drives.
- **Smoothie.NET** and **libsmoothie**: the layered filesystem.
- **pdx** (Paradox): the libraries loaded into the app's processes.
- **vxtools**: the packaging scripts and the `.vxapp` format spec, including
  `vxapp.config` and `vxapp.info`.
- **vxlauncher** and **VXBootStrap**: the native launcher and injector.

## Credits and licence

- [Playnite](https://github.com/JosefNemec/Playnite) and its SDK are by Josef
  Nemec, MIT licensed.
- [Newtonsoft.Json](https://www.newtonsoft.com/json) 10.0.3, MIT licensed.
- `System.IO.Compression.ZipFile` 4.3.0 from Microsoft, under the .NET library
  licence that ships in its package.

All three are restored from NuGet and none of them is included here. This
repository has no licence file of its own yet.
