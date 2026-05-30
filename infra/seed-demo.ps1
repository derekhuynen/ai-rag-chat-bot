<#
.SYNOPSIS
  Seeds a deployed AI RAG Chat Bot with the fictional Contoso demo documents.

.DESCRIPTION
  Logs in as the admin user, then uploads every .md/.txt file in the demo
  documents folder through the same admin upload endpoint the UI uses
  (POST /management/documents/upload). The backend chunks, summarizes, embeds,
  and indexes each file into Azure AI Search synchronously, so when this script
  reports "processed" the document is already queryable by the chat.

  Uses .NET HttpClient for the multipart upload so it works on both Windows
  PowerShell 5.1 and PowerShell 7+ (Invoke-RestMethod -Form is 7+ only).

.PARAMETER ApiBaseUrl
  The Functions API base URL, e.g. https://ragchat-func.azurewebsites.net/api
  (the deploy.ps1 output "API base URL"). Trailing slash is fine.

.PARAMETER AdminEmail
  Admin email. Defaults to admin@example.com (the deploy default).

.PARAMETER AdminPassword
  Admin password as a SecureString. This is the password printed by deploy.ps1
  (or the one you passed it). Required.

.PARAMETER DocumentsPath
  Folder of .md/.txt files to upload. Defaults to ../demo/documents.

.EXAMPLE
  ./seed-demo.ps1 -ApiBaseUrl https://ragchat-func.azurewebsites.net/api `
                  -AdminPassword (Read-Host "Admin password" -AsSecureString)

.EXAMPLE
  # Local dev (func start on :7071)
  ./seed-demo.ps1 -ApiBaseUrl http://localhost:7071/api `
                  -AdminPassword (ConvertTo-SecureString 'ChangeThisAdminPassword123!' -AsPlainText -Force)
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)][string]$ApiBaseUrl,
  [string]$AdminEmail = 'admin@example.com',
  [Parameter(Mandatory = $true)][securestring]$AdminPassword,
  [string]$DocumentsPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'demo/documents')
)

$ErrorActionPreference = 'Stop'

function ConvertFrom-SecureToPlain([securestring]$s) {
  [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
}

# Normalize the base URL (drop any trailing slash).
$ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')

if (-not (Test-Path $DocumentsPath)) { throw "Documents path not found: $DocumentsPath" }
$files = Get-ChildItem -Path $DocumentsPath -File | Where-Object { $_.Extension -in '.md', '.txt' } | Sort-Object Name
if (-not $files) { throw "No .md or .txt files found in $DocumentsPath" }

Write-Host "Seeding $($files.Count) document(s) from $DocumentsPath" -ForegroundColor Cyan
Write-Host "API: $ApiBaseUrl" -ForegroundColor Gray

# --- 1) Log in as admin -----------------------------------------------------
Write-Host "Logging in as $AdminEmail..." -ForegroundColor Cyan
$loginBody = @{ email = $AdminEmail; password = (ConvertFrom-SecureToPlain $AdminPassword) } | ConvertTo-Json
try {
  $login = Invoke-RestMethod -Uri "$ApiBaseUrl/auth/login" -Method Post -Body $loginBody -ContentType 'application/json'
}
catch {
  throw "Login failed for $AdminEmail. Check the API URL and admin credentials. $($_.Exception.Message)"
}
$token = $login.token
if ([string]::IsNullOrWhiteSpace($token)) { throw "Login returned no token." }
if ($login.user.role -ne 'Admin') {
  throw "User '$AdminEmail' is not an Admin (role: $($login.user.role)). Uploads require an admin account."
}
Write-Host "Authenticated as admin." -ForegroundColor Green

# --- 2) Upload each document via multipart/form-data ------------------------
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromMinutes(10)  # processing (summarize+embed) is synchronous
$client.DefaultRequestHeaders.Authorization =
  New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $token)

$ok = 0
$failed = 0
foreach ($file in $files) {
  Write-Host "Uploading $($file.Name)..." -NoNewline
  $form = New-Object System.Net.Http.MultipartFormDataContent
  try {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $fileContent = New-Object System.Net.Http.ByteArrayContent (, $bytes)
    $fileContent.Headers.ContentType =
      [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse('text/markdown')
    # Field name must be "file" to match the UploadDocument function.
    $form.Add($fileContent, 'file', $file.Name)

    $resp = $client.PostAsync("$ApiBaseUrl/management/documents/upload", $form).GetAwaiter().GetResult()
    $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    if ($resp.IsSuccessStatusCode) {
      $status = (ConvertFrom-Json $body).status
      if ($status -eq 'processed') {
        Write-Host " ok ($status)" -ForegroundColor Green
        $ok++
      }
      else {
        Write-Host " uploaded but status='$status'" -ForegroundColor Yellow
        Write-Host "    $body" -ForegroundColor DarkYellow
        $ok++
      }
    }
    else {
      Write-Host " FAILED ($([int]$resp.StatusCode))" -ForegroundColor Red
      Write-Host "    $body" -ForegroundColor DarkRed
      $failed++
    }
  }
  catch {
    Write-Host " ERROR" -ForegroundColor Red
    Write-Host "    $($_.Exception.Message)" -ForegroundColor DarkRed
    $failed++
  }
  finally {
    $form.Dispose()
  }
}

$client.Dispose()

Write-Host ""
Write-Host "=== Seeding complete: $ok succeeded, $failed failed ===" -ForegroundColor $(if ($failed) { 'Yellow' } else { 'Green' })
if ($failed) { exit 1 }
