using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("ba735ee0-6611-435c-9206-aeaf3661a717")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyTitle("PINS AllSky")]
[assembly: AssemblyDescription("Pi HQ camera timelapses, keograms, and startrails for PINS and Touch-N-Stars")]
[assembly: AssemblyCompany("PINS AllSky Contributors")]
[assembly: AssemblyProduct("PINS AllSky")]
[assembly: AssemblyCopyright("Copyright © 2026 PINS AllSky Contributors")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.3.0.1021")]
[assembly: AssemblyMetadata("License", "GPL-3.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.gnu.org/licenses/gpl-3.0.en.html#license-text")]
[assembly: AssemblyMetadata("Tags", "PINS,Touch-N-Stars,AllSky,Timelapse,Keogram,Startrails,Raspberry Pi")]
[assembly: AssemblyMetadata("LongDescription", @"# PINS AllSky

Capture Pi HQ camera frames during an astrophotography session and generate timelapse videos, keograms, and startrail composites.

## Highlights
- Pi camera capture on Raspberry Pi 5 via `rpicam-still`
- Automatic session start and stop driven by the local Advanced API sequence state
- Timelapse video generation with `ffmpeg`
- Keogram and startrail generation from the AllSky project
- Touch-N-Stars frontend plugin for live preview, control, and configuration

## Notes
- Startrails are still offered on equatorial mounts, but the resulting image may not be meaningful when the camera tracks with the mount.
- Keogram and startrail processing are based on the AllSky MIT-licensed tools by the AllSky Team.
")]
[assembly: ComVisible(false)]
