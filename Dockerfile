FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PaymentAPI/PaymentAPI.csproj PaymentAPI/
RUN dotnet restore PaymentAPI/PaymentAPI.csproj
COPY PaymentAPI/ PaymentAPI/
RUN dotnet publish PaymentAPI/PaymentAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentAPI.dll"]
