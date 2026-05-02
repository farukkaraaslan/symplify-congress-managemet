$applicationRoot = "src\projects\back-office\Symplify.BackOffice.Application"

if (-not (Test-Path $applicationRoot)) {
    Write-Host "Application path bulunamadı: $applicationRoot" -ForegroundColor Red
    exit 1
}

$files = Get-ChildItem -Path $applicationRoot -Recurse -Filter "*.cs"

$staticUsingPattern = 'using\s+static\s+(?<full>Symplify\.BackOffice\.Application\.Features\.(?<feature>[^.]+)\.Constants\.(?<claims>[A-Za-z0-9_]+OperationClaims));'

$rolePatternNewArray = 'public\s+string\[\]\s+Roles\s*=>\s*new\[\]\s*\{\s*(?<body>.*?)\s*\}\s*;'
$rolePatternCollection = 'public\s+string\[\]\s+Roles\s*=>\s*\[\s*(?<body>.*?)\s*\]\s*;'

$updatedCount = 0

function Convert-RolesBody {
    param(
        [string] $body,
        [string] $claimsClass
    )

    $items = $body `
        -split "," `
        | ForEach-Object { $_.Trim() } `
        | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    $converted = @()

    foreach ($item in $items) {
        $clean = $item.Trim()

        if ($clean -match '^[A-Za-z_][A-Za-z0-9_]*$') {
            $converted += "$claimsClass.$clean"
        }
        else {
            $converted += $clean
        }
    }

    return "public string[] Roles => new[] { " + ($converted -join ", ") + " };"
}

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    $match = [regex]::Match($content, $staticUsingPattern)

    if (-not $match.Success) {
        continue
    }

    $fullUsing = $match.Groups["full"].Value
    $feature = $match.Groups["feature"].Value
    $claimsClass = $match.Groups["claims"].Value
    $constantsNamespace = "Symplify.BackOffice.Application.Features.$feature.Constants"

    # using static ...OperationClaims; -> using ...Constants;
    $content = [regex]::Replace(
        $content,
        $staticUsingPattern,
        "using $constantsNamespace;"
    )

    # public string[] Roles => new[] { Admin, Write, Update };
    $content = [regex]::Replace(
        $content,
        $rolePatternNewArray,
        {
            param($m)
            Convert-RolesBody $m.Groups["body"].Value $claimsClass
        },
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    # public string[] Roles => [Admin, Write, Update];
    $content = [regex]::Replace(
        $content,
        $rolePatternCollection,
        {
            param($m)
            Convert-RolesBody $m.Groups["body"].Value $claimsClass
        },
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $updatedCount++
        Write-Host "Updated: $($file.FullName)" -ForegroundColor Green
    }
}

Write-Host "Tamamlandı. Güncellenen dosya sayısı: $updatedCount" -ForegroundColor Cyan