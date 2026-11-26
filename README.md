### Table of Contents

- [About](#about)
- [Installation](#installation)
- [Usage](#usage)
- [Development](#development)

---

### About

**Nexora** is an open source cross-platform desktop audio player.

Features:

- Loads music from your system's default directory (`C:/Users/<user>/Music` on Windows, `~/Music` on Linux)
- Basic audio player controls
- Library in grid (default) or list view, with search ranked by match quality
- Playback queue
- Automatic loudness normalization (-14 LUFS)
- Track metadata editor (title and artists)
- Rotatable 3D cover viewer (why? no reason)
- Customizable animations: override durations and easings in `animations.json` in the app data directory
  (`%APPDATA%/Nexora` on Windows, `~/.config/Nexora` on Linux)
- Displays your current listening activity in Discord Rich Presence (no cover art for now)

Target platforms currently are:

- Windows
- Linux

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/d1f8744b-4358-43d5-83dd-32a9f30ea411" />

---

### Installation

#### Option 1: Prebuilt binary

Binaries for the latest release:

- [Windows x64](https://github.com/Sapryx/Nexora/releases/latest/download/Nexora-win-x64.zip)
- [Linux x64](https://github.com/Sapryx/Nexora/releases/latest/download/Nexora-linux-x64.tar.gz)

You can view all releases [here](https://github.com/Sapryx/Nexora/releases).

#### Option 2: Package manager

winget (Windows):

```
winget install Sapryx.Nexora
```

AUR (Arch Linux):

```
yay -S nexora
```

#### Option 3: Manual build

Clone the repository:

```
git clone "https://github.com/sapryx/nexora"
```

Open the solution directory:

```
cd Nexora
```

<details>
<summary>Windows</summary>

Dependencies:

- .NET 10 SDK
- VLC

Install dependencies:

```
winget install Microsoft.DotNet.SDK.10 VideoLAN.VLC
```

Build the application:

```
.\scripts\Publish.ps1 -Rid win-x64
```

</details>

<details>
<summary>Linux</summary>

Dependencies:

- .NET 10 SDK
- clang
- zlib
- VLC

Install dependencies:

<details>
<summary>Arch Linux (pacman)</summary>

```
sudo pacman -S dotnet-sdk clang zlib vlc
```

</details>

<details>
<summary>Debian / Ubuntu (apt)</summary>

.NET isn't reliably up to date in the distro repos yet, so add Microsoft's official package feed first, then install
everything:

```
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 clang zlib1g-dev vlc
```

(For Debian instead of Ubuntu, swap the URL for
`https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb`, or `.../13/...` on Trixie.)
</details>

<details>
<summary>Fedora (dnf)</summary>

.NET and the build tools are in Fedora's own repos, but VLC isn't. It requires RPM Fusion:

```
sudo dnf install -y dotnet-sdk-10.0 clang zlib-devel
sudo dnf install -y https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
sudo dnf install -y vlc
```

</details>

Build the application:

```
chmod +x scripts/publish.sh
./scripts/publish.sh linux-x64
```

</details>

---

### Usage

Keyboard shortcuts (ignored while typing in a text field or while a popup or the settings tab is open):

| Key     | Action                 |
|---------|------------------------|
| `Space` | Play / pause           |
| `←`     | Rewind 5 seconds       |
| `→`     | Fast-forward 5 seconds |

Command-line options:

- `--log-level trace|debug|info|warn|error|crit` — minimum log level (`info` by default)

App data lives in `%APPDATA%/Nexora` on Windows and `~/.config/Nexora` on Linux:

- `logs/session-<date>_<time>.log` — one log per launch, the 10 newest are kept
- `volume.txt` — the last volume level (50 on first launch); delete it to reset
- `animations.json` — optional animation overrides

---

### Development

Run the tests:

```
./scripts/test.sh
```

Slow tests run only when the code they cover has uncommitted changes; pass `--all` to run every test, for example
before a release. Other arguments go to `dotnet test`.

Profile the app with `perf` and open the recording in [Hotspot](https://github.com/KDAB/hotspot) (Arch Linux; needs `perf` and `hotspot`):

```
./scripts/profile.sh [--dwarf] [output file]
```

It builds a Release (JIT) build, records it until the app is closed or Ctrl+C is pressed, and installs debug symbols
for stripped system libraries from Arch's debuginfod server. The recording goes to `~/nexora.perf.data` by default.

Native libraries such as SkiaSharp are built without frame pointers, so their call stacks stop at the first native
frame. Pass `--dwarf` to unwind them from debug info instead; the recording grows by tens of MB per second, so keep it
short.
