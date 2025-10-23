FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TuringMachinesAPI.sln ./
COPY src/TuringMachinesAPI.csproj ./src/

RUN dotnet restore TuringMachinesAPI.sln

COPY . .

RUN dotnet publish src/TuringMachinesAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TuringMachinesAPI.dll"]
