$urlGet = "https://crm.i-ca.vn/RAFrontEnd/Login.jsp"
$urlPost = "https://crm.i-ca.vn/RAFrontEnd/LoginCommon"

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# 1. GET request to extract CsrfToken
$responseGet = Invoke-WebRequest -Uri $urlGet -WebSession $session
$html = $responseGet.Content

# Use regex to extract CsrfToken
$csrfToken = ""
if ($html -match 'id="CsrfToken"\s+value="([^"]+)"') {
    $csrfToken = $matches[1]
}

Write-Host "CsrfToken: $csrfToken"

# 2. POST request to login
$body = @{
    idParam = "loginpage"
    sUserName = "bachkhoa"
    sPwd = "Toimanhnhat@10"
    sCaptcha = ""
    svn = "1"
    CsrfToken = $csrfToken
}

$responsePost = Invoke-WebRequest -Uri $urlPost -Method Post -Body $body -WebSession $session

Write-Host "Login response: $($responsePost.Content)"

# Extract JSESSIONID and XSRF-TOKEN
foreach ($cookie in $session.Cookies.GetCookies($urlPost)) {
    Write-Host "Cookie: $($cookie.Name)=$($cookie.Value)"
}
