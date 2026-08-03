### Table of Contents
- [About](#about)
- [Installation](#installation)

---

### About
**Nexora** is a cross-platform, open source desktop audio player.

Features:
- Loads music from your system's default directory (`C:/Users/[user]/Music` on Windows, `~/Music` on Linux)
- Basic audio player controls
- Displays your current listening activity in Discord Rich Presence (not the cover, sadly 😞)

Target platforms currently are:
- Windows
- Linux
- MacOS (hasn't been tested yet, but it should work fine in theory, because all dependencies are platform-independent)

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
