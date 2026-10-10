### Table of Contents
- [About](#about)
- [Installation](#installation)
- [Usage](#usage)
- [Development](#development)

---

### About
**Nexora** is an open source cross-platform desktop audio player.

Features:
- Loads music from your system's default directory (`C:/Users/[user]/Music` on Windows, `~/Music` on Linux)
- Basic audio player controls
- Intelligent search system
- Modern UI/UX
- Keyboard shortcuts for playback
- Displays your current listening activity in Discord Rich Presence (not the cover, sadly 😞)

Target platforms currently are:
- Windows
- Linux

<img width="1920" height="1030" alt="image" src="https://github.com/user-attachments/assets/19901dca-8641-4d66-a4b8-410a1e6b0c1b" />

---

### Installation
The only option right now is to manually build the thing.

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

.NET isn't reliably up to date in the distro repos yet, so add Microsoft's official package feed first, then install everything:
```
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 clang zlib1g-dev vlc
```
(For Debian instead of Ubuntu, swap the URL for `https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb`, or `.../13/...` on Trixie.)
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

<details>
<summary>macOS</summary>

Dependencies:
- .NET 10 SDK
- Xcode Command Line Tools (provides `clang` and the linker Native AOT needs)
- VLC

Install dependencies via [Homebrew](https://brew.sh):
```
xcode-select --install
brew install --cask dotnet-sdk vlc
```

> Note: some .NET SDK versions require the full Xcode app (not just the Command Line Tools) for AOT publishing. If the build fails with an `xcodebuild requires Xcode` error, install Xcode from the App Store, then run `sudo xcode-select --switch /Applications/Xcode.app`.

Build the application:
```
chmod +x scripts/publish.sh
./scripts/publish.sh osx-arm64
```
Use `osx-x64` instead if you're on an Intel Mac.
</details>

---

### Usage

Keyboard shortcuts (ignored while typing in the search bar):

| Key | Action |
|-----|--------|
| `Space` | Play / pause |
| `←` | Rewind 5 seconds |
| `→` | Fast-forward 5 seconds |

Command-line options:
- `--log-level trace|debug|info|warn|error|crit` — minimum log level (`info` by default)

App data lives in `%APPDATA%/Nexora` on Windows and `~/.config/Nexora` on Linux.
The app stores logs for the last 10 sessions.
- `logs/session-<date>_<time>.log` — one log per launch, the 10 newest are kept.
- `volume.txt` — the last volume level (50 on first launch); delete it to reset.

---

### Development

Run the tests:
```
./scripts/test.sh
```
(`.\scripts\Test.ps1` on Windows.)

Run with a code coverage report (requires [ReportGenerator](https://github.com/danielpalme/ReportGenerator)):
```
.\scripts\RunTestsWithCoverage.ps1
```
