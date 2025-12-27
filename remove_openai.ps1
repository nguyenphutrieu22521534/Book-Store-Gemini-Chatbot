$content = Get-Content appsettings.json -Raw
$content = $content -replace '"OpenAI":\s*{\s*"ApiKey":\s*"[^"]*"\s*},\s*', ''
$content | Set-Content appsettings.json
