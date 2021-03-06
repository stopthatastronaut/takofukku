# is the host running? try func host start

try
{
    Invoke-WebRequest http://localhost:7071/
}
catch
{
    & func host start
}

# fire up ngrok here
$proc = start-process "ngrok" -ArgumentList @('http','7071') -PassThru

# get key from .secret
$secrets = gc .\.secrets | ConvertFrom-Json

$localUrl = "http://localhost:7071/api/Takofukku?apikey=" + $secrets.octopusapikey
$remoteUrl = "https://hook.takofukku.io/api/Takofukku?code="+$secrets.azuresecret+"&apikey="+ $secrets.octopusapikey
$pushevent = gc .\Takofukku\models\pushevent.json -raw |  ConvertFrom-Json


Invoke-RestMethod $localurl -ContentType "application/json" -Body ($pushevent | ConvertTo-Json -depth 5)  -Method POST -verbose 



iwr https://raw.githubusercontent.com/stopthatastronaut/TakoFukku/master/takofile


Read-Host -Prompt "Press a key when manual testing is complete"

# kill ngrok
$proc | Stop-Process