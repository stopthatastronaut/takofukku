

$localUrl = "http://localhost:7071/api/Takofukku?apikey=API-1234567890"
$pushevent = gc .\Takofukku\models\pushevent.json -raw |  ConvertFrom-Json


irm $localUrl -ContentType "application/json" -Body ($pushevent | ConvertTo-Json -depth 5)  -Method POST -verbose 