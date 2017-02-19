Invoke-WebRequest -UseBasicParsing `
https://functionplayground.azurewebsites.net/api/InternetUpLogger?code=SBCl2hu2Cli1xSWmzRjCe5KPomx5bdF8zSaGaFpThIH6c6tdXkmHlw== `
 -ContentType "application/json" -Method POST -Body `
 "{ 'MachineName':'Beastly'}"