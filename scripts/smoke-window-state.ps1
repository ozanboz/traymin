param([Parameter(Mandatory = $true)][string]$ProcessName)

Add-Type -Namespace SmokeWin -Name Api -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
[DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
'@

$found = $false
foreach ($p in Get-Process -Name $ProcessName -ErrorAction SilentlyContinue) {
  if ($p.MainWindowHandle -eq 0) {
    Write-Output ("pid={0} mainWindowHandle=0 (window hidden or unavailable)" -f $p.Id)
    $found = $true
    continue
  }
  $visible = [SmokeWin.Api]::IsWindowVisible($p.MainWindowHandle)
  Write-Output ("pid={0} hwnd=0x{1:X} visible={2}" -f $p.Id, [int64]$p.MainWindowHandle, $visible)
  $found = $true
}
if (-not $found) { Write-Output "$ProcessName is not running" }
