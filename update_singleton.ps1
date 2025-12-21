$file = 'c:\SR3H_MACRO\Services\MacroFortActivationService.cs'
$content = [IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

$singleton = "        private static readonly Lazy<MacroFortActivationService> _instance = `r`n            new Lazy<MacroFortActivationService>(() => new MacroFortActivationService());`r`n        `r`n        public static MacroFortActivationService Instance => _instance.Value;`r`n`r`n"

$content = $content -replace '(    public class MacroFortActivationService\s*\{)\s*(        private readonly MacroFortSecureCredentialsManager)', "`$1`r`n$singleton`$2"
$content = $content -replace 'public MacroFortActivationService\(\)', 'private MacroFortActivationService()'

[IO.File]::WriteAllText($file, $content, [System.Text.Encoding]::UTF8)
Write-Host "Singleton pattern added successfully"
