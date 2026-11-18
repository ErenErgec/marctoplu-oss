FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/MarcToken.Api/MarcToken.Api.csproj backend/MarcToken.Api/
RUN dotnet restore backend/MarcToken.Api/MarcToken.Api.csproj

COPY backend/MarcToken.Api backend/MarcToken.Api
RUN dotnet publish backend/MarcToken.Api/MarcToken.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends python3 python3-pip && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

COPY engine/requirements.txt /tmp/requirements.txt
RUN pip3 install --no-cache-dir --break-system-packages -r /tmp/requirements.txt

COPY engine /engine

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MarcToken.Api.dll"]
