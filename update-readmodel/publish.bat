@echo off
:: *******************************************************************
:: Builds project locally and then uploads final zip file to S3 folder
:: *******************************************************************
dotnet lambda package -o .\bin\Release\netcoreapp2.1\update-readmodel.zip
aws s3 cp .\bin\Release\netcoreapp2.1\update-readmodel.zip s3://howtobuildaneventstore-deployment-artifacts/update-readmodel.zip