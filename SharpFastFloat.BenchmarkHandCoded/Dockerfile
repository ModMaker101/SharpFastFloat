FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN dotnet build "SharpFastFloat.BenchmarkHandCoded.csproj" -c Release
RUN dotnet publish "SharpFastFloat.BenchmarkHandCoded.csproj" -c Release

COPY bin/Release/net9.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "SharpFastFloat.BenchmarkHandCoded.dll"]
