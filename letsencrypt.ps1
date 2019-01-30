 
# Load ACMESharp module
import-module ACMESharp
 
# Change to the Vault folder
Set-Location C:\ProgramData\ACMESharp\sysVault
 
### VARIABLES ###
 
$alias = "vcc-$(get-date -format yyyyMMddhhmm)"
$certname = "vcc-$(get-date -format yyyyMMddhhmm)"
$pfxfile = "C:\ProgramData\ACMESharp\sysVault\$certname.pfx"
# Configure the FQDN that the certificate needs to be bound to
$domain = "hook.takofukku.io"
 

### PART 1: UPDATE THE IDENTIFIER ###
 
New-ACMEIdentifier -Dns $domain -Alias $alias
Complete-ACMEChallenge $alias -ChallengeType dns-01 -Handler manual -force 
 
(Update-ACMEIdentifier $alias -ChallengeType dns-01).Challenges | Where-Object {$_.Type -eq "dns-01"} | Out-File challenge.txt
$RRtext = Select-String challenge.txt -Pattern "RR " -CaseSensitive | select Line 

$textrecord = [regex]::match($RRText[2].Line, "\[(.+)\]").Groups[1].Value

Write-Host "New Challenge text is $textrecord"

# hop back to our repo
Push-Location $repos\takofukku 

# Here we grab the new TXT DNS Record and push to Route53
$secrets = (gc .\.secrets | ConvertFrom-Json)
$storedcreds = $secrets.storedcredentials
Set-AWSCredentials -storedCredentials $storedcreds

$zoneId = $zone = (Get-R53HostedZones | ? { $_.Name -eq "takofukku.io." } ).Id

$currentDNSValue = Get-R53ResourceRecordSet -hostedzoneID $zoneID -StartRecordName _acme-challenge.hook.takofukku.io. -MaxItem 1 | 
                                                                        select -expand ResourceRecordSets | 
                                                                        select -expand ResourceRecords | select -expand Value

Write-Host "Route 53 record value is $currentDNSValue" 
 
.\manage-r53recordset.ps1 -HostedZoneID $zoneID -Name _acme-challenge.hook -Value "`"$textrecord`"" -Type TXT -TTL 60 -Force -Verbose

$DNS = Resolve-DNSName -Name _acme-challenge.hook.takofukku.io -Type TXT | select -expand strings

while($DNS -ne $textrecord)
{

    Write-Host "DNS not yet confirmed propagated, waiting 5 seconds"
    Start-Sleep -s 5
    $DNS = Resolve-DNSName -Name _acme-challenge.hook.takofukku.io -Type TXT | select -expand strings
}

Pop-Location

Read-Host "DNS should be ready. Hit any key to complete your request"
 
Submit-ACMEChallenge $alias -ChallengeType dns-01
Update-ACMEIdentifier $alias
 
### PART 2: UPDATE THE CERTIFICATE ###
 
# Generate a new certificate
New-ACMECertificate ${alias} -Generate -Alias $certname
 
# Submit the certificate request
Submit-ACMECertificate $certname
 
# Wait until the certificate is available (has a serial number) before moving on
# as API work in async mode so the cert may not be immediately released. 
$serialnumber = $null
$serialnumber = $(update-AcmeCertificate $certname).SerialNumber
 
# Export the new Certificate to a PFX file
Get-ACMECertificate $certname -ExportPkcs12 $pfxfile -CertificatePassword $secrets.certpassword

# now upload the PFX to Azure and Bind it
Set-Location $repos\takofukku

# TODO
