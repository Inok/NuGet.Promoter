﻿Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Generate .env file

$EnvFilePath = Join-Path $PSScriptRoot '.env'

Write-Host "Generate $EnvFilePath" -ForegroundColor Green

$EnvHeader = @"
# The file is generated at $( Get-Date ) using $( $MyInvocation.MyCommand )
# Do not commit/push the file or change it manually
"@

$EnvVars = @"
BAGETTER_API_KEY=$( -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 20 | % {[char]$_}) )
"@

$EnvFileContent = $EnvHeader + [Environment]::NewLine + $EnvVars

Set-Content -Path $EnvFilePath -Value $EnvFileContent -ErrorAction Stop