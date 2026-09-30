# SimCore sebagai container Linux (CI/integrasi).
# Build dari folder SyncNetSdkPro:  docker build -f tools/SyncNetPro.SimCore/Dockerfile -t syncnet/simcore .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish tools/SyncNetPro.SimCore -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /work
COPY --from=build /app /app
EXPOSE 5080
# Mount folder berisi simcore.json + scenarios ke /work
ENTRYPOINT ["dotnet", "/app/SyncNetPro.SimCore.dll"]
CMD ["up", "--config", "simcore.json", "--scenarios", "scenarios", "--urls", "http://0.0.0.0:5080"]
