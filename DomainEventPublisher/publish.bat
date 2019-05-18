@echo off
:: *******************************************************************
:: Builds project locally and then uploads final zip file to S3 folder
:: *******************************************************************
dotnet lambda package -o .\bin\Release\netcoreapp2.1\domain-event-publisher.zip
aws s3 cp .\bin\Release\netcoreapp2.1\domain-event-publisher.zip s3://howtobuildaneventstore-deployment-artifacts/domain-event-publisher.zip