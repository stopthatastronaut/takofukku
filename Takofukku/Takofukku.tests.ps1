# get key from .secret
$secrets = gc .\.secrets | ConvertFrom-Json

$localUrl = "http://localhost:7071/api/Takofukku?apikey=" + $secrets.octopusapikey
$remoteUrl = "https://hook.takofukku.io/api/Takofukku?code="+$secrets.azuresecret+"&apikey="+ $secrets.octopusapikey
$pushevent = gc .\Takofukku\models\pushevent.json -raw |  ConvertFrom-Json


irm $localurl -ContentType "application/json" -Body ($pushevent | ConvertTo-Json -depth 5)  -Method POST -verbose 



iwr https://raw.githubusercontent.com/stopthatastronaut/TakoFukku/master/takofile