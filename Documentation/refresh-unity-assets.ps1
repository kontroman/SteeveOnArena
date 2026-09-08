# Ask the running editor to import assets without changing desktop focus.
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class MineArenaAssetRefresh {
 [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr h,int p);
 [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr h,uint id,System.Text.StringBuilder s,int n,uint f);
 [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr h,int p);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
}
'@
$projectEditor = Get-Process Unity | Where-Object { $_.MainWindowTitle -like 'MineArena -*' } | Select-Object -First 1
if ($null -eq $projectEditor) { throw 'MineArena editor is not running.' }
$editorMenu = [MineArenaAssetRefresh]::GetMenu($projectEditor.MainWindowHandle)
for ($i=0; $i -lt [MineArenaAssetRefresh]::GetMenuItemCount($editorMenu); $i++) {
 $label = New-Object System.Text.StringBuilder 256
 [void][MineArenaAssetRefresh]::GetMenuString($editorMenu,$i,$label,256,1024)
 if ($label.ToString() -ne 'Assets') { continue }
 $assetMenu = [MineArenaAssetRefresh]::GetSubMenu($editorMenu,$i)
 for ($j=0; $j -lt [MineArenaAssetRefresh]::GetMenuItemCount($assetMenu); $j++) {
  $entry = New-Object System.Text.StringBuilder 256
  [void][MineArenaAssetRefresh]::GetMenuString($assetMenu,$j,$entry,256,1024)
  if ($entry.ToString() -match '^Refresh') {
   $commandId = [MineArenaAssetRefresh]::GetMenuItemID($assetMenu,$j)
   [void][MineArenaAssetRefresh]::PostMessage($projectEditor.MainWindowHandle,273,[IntPtr]$commandId,[IntPtr]::Zero)
   return
  }
 }
}
throw 'Refresh command was not found.'
