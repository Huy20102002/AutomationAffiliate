import os

def build_installer():
    os.rename('payload.zip', 'Installer/payload.zip')
    
    csproj = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <AssemblyName>ShopeeVideoUploader_Setup</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="payload.zip" />
  </ItemGroup>
</Project>"""
    
    with open('Installer/Installer.csproj', 'w', encoding='utf-8') as f:
        f.write(csproj)
        
    program_cs = """using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;

namespace Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ShopeeVideoUploader Setup";
            Console.WriteLine("DANG CAI DAT SHOPEE VIDEO UPLOADER...");
            Console.WriteLine("=============================================");
            
            string targetDir = @"C:\\ShopeeVideoUploader";
            string exePath = Path.Combine(targetDir, "ShopeeVideoUploader.exe");
            
            try
            {
                if (Directory.Exists(targetDir))
                {
                    Console.WriteLine("Da xoa ban cu...");
                    // Try to kill if running
                    var processes = Process.GetProcessesByName("ShopeeVideoUploader");
                    foreach (var p in processes) { try { p.Kill(); } catch { } }
                    System.Threading.Thread.Sleep(1000);
                    
                    try { Directory.Delete(targetDir, true); } catch { }
                }
                
                Directory.CreateDirectory(targetDir);
                
                Console.WriteLine("Dang giai nen du lieu...");
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Installer.payload.zip"))
                {
                    if (stream == null) throw new Exception("Khong tim thay payload.zip");
                    using (var archive = new ZipArchive(stream))
                    {
                        archive.ExtractToDirectory(targetDir, true);
                    }
                }
                
                Console.WriteLine("Dang tao Shortcut ra Desktop...");
                string ps = $"$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut(\\"$HOME\\\\Desktop\\\\Shopee Video Uploader.lnk\\"); $Shortcut.TargetPath = \\"{exePath}\\"; $Shortcut.WorkingDirectory = \\"{targetDir}\\"; $Shortcut.Save()";
                
                var pInfo = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \\"{ps}\\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(pInfo)?.WaitForExit();
                
                Console.WriteLine("Cai dat thanh cong! Dang mo phan mem...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("LOI: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}
"""
    with open('Installer/Program.cs', 'w', encoding='utf-8') as f:
        f.write(program_cs)
        
    print("Prepared Installer files")

if __name__ == '__main__':
    build_installer()
