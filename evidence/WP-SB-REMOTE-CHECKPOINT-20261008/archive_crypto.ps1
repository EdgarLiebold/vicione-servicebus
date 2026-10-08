param(
    [Parameter(Mandatory)][ValidateSet('Encrypt', 'Decrypt')][string]$Mode,
    [Parameter(Mandatory)][string]$InputFile,
    [Parameter(Mandatory)][string]$OutputFile,
    [Parameter(Mandatory)][string]$KeyFile,
    [Parameter(Mandatory)][string]$Aad
)
$ErrorActionPreference = 'Stop'
if (-not [Security.Cryptography.AesGcm]::IsSupported) { throw 'AES-GCM is unsupported.' }
if ([IO.File]::Exists($OutputFile)) { throw 'Output must not exist.' }
$key = [IO.File]::ReadAllBytes($KeyFile)
if ($key.Length -ne 32) { throw 'Expected a 256-bit key.' }
$magic = [Text.Encoding]::ASCII.GetBytes("VICIONE-SERVICEBUS-AES256GCM-V1`n")
$associated = [Text.Encoding]::UTF8.GetBytes(([Text.Encoding]::ASCII.GetString($magic)) + $Aad)
$aes = [Security.Cryptography.AesGcm]::new($key, 16)
try {
    $data = [IO.File]::ReadAllBytes($InputFile)
    if ($Mode -eq 'Encrypt') {
        if ($data.Length -gt 41943040) { throw 'Plaintext chunk exceeds 40 MiB.' }
        $nonce = [Security.Cryptography.RandomNumberGenerator]::GetBytes(12)
        $tag = [byte[]]::new(16)
        $cipher = [byte[]]::new($data.Length)
        $aes.Encrypt($nonce, $data, $cipher, $tag, $associated)
        $stream = [IO.File]::Open($OutputFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
        try {
            $stream.Write($magic, 0, $magic.Length)
            $stream.Write($nonce, 0, $nonce.Length)
            $stream.Write($tag, 0, $tag.Length)
            $stream.Write($cipher, 0, $cipher.Length)
        } finally { $stream.Dispose() }
    } else {
        $overhead = $magic.Length + 12 + 16
        if ($data.Length -lt $overhead -or $data.Length -gt (41943040 + $overhead)) { throw 'Invalid encrypted length.' }
        for ($i = 0; $i -lt $magic.Length; $i++) {
            if ($data[$i] -ne $magic[$i]) { throw 'Invalid encrypted header.' }
        }
        $nonce = [byte[]]::new(12)
        $tag = [byte[]]::new(16)
        $cipher = [byte[]]::new($data.Length - $overhead)
        [Array]::Copy($data, $magic.Length, $nonce, 0, 12)
        [Array]::Copy($data, $magic.Length + 12, $tag, 0, 16)
        [Array]::Copy($data, $overhead, $cipher, 0, $cipher.Length)
        $plain = [byte[]]::new($cipher.Length)
        $aes.Decrypt($nonce, $cipher, $tag, $plain, $associated)
        $stream = [IO.File]::Open($OutputFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
        try { $stream.Write($plain, 0, $plain.Length) } finally { $stream.Dispose() }
    }
} finally {
    $aes.Dispose()
    [Array]::Clear($key, 0, $key.Length)
}
