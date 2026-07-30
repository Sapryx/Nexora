### Table of Contents
- [About](#about)
- [Installation](#installation)

---

### About
**Nexora** is a cross-platform, open source desktop audio player.

Features:
- Loads music from your system's default directory (`C:/Users/[user]/Music` on Windows, `~/Music` on Linux)
- Has basic audio player controls
- Displays your current listening activity in Discord Rich Presence
- Left side buttons are just for fun now

Target platforms currently are:
- Windows
- Linux
- MacOS (hasn't been tested yet, but it should work fine in theory, because all dependencies are platform-independent)

![preview.png](preview.png)

---

### Installation
The only option right now is to manually build the thing. You'll need:
- VLC
- .NET 10 SDK
- .NET 10 Runtime (optional)

<details>
<summary>Downloading dependencies</summary>
Arch Linux:

```
yay -S vlc
```

Windows:
```
winget install VideoLAN.VLC
```
</details>

<details>
<summary>Building the project</summary>
Clone the repository:

```
git clone "https://github.com/sapryx/nexora"
```

Open the solution directory:
```
cd Nexora
```

Build the project:

Windows
```
.\Publish.ps1
```

Linux
```
publish.sh
```

The executable file will be located at `Nexora/bin/Release/net10.0/[your OS]/publish/`
</details>
