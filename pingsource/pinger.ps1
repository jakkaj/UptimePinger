Invoke-WebRequest -UseBasicParsing `
<yourfunctionurl> `
 -ContentType "application/json" -Method POST -Body `
 "{ 'MachineName':'Beastly'}"