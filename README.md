### Table of Contents
- [About](#about)
- [Installation](#installation)

---

### About
**Nexora** is an open-source cross-platform desktop audio player.

Features:
- Loads music from your system's default directory (`C:/Users/[user]/Music` on Windows, `~/Music` on Linux)
- Basic audio player controls
- Displays your current listening activity in Discord Rich Presence (not the cover, sadly 😞)

Target platforms currently are:
- Windows
- Linux

<img width="1920" height="1030" alt="image" src="https://github.com/user-attachments/assets/d271fb95-d45b-462b-b0f8-de5e33c0c9f6" />

---

### Installation
#### Option 1: Prebuilt binary
Binaries for the latest release:
- [Windows x64](https://github.com/Sapryx/Nexora/releases/latest/download/Nexora-win-x64.zip)
- [Linux x64](https://github.com/Sapryx/Nexora/releases/latest/download/Nexora-linux-x64.tar.gz)

You can view all releases [here](https://github.com/Sapryx/Nexora/releases).

#### Option 2: Package manager
AUR:
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
