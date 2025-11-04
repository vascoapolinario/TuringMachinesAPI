FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY TuringMachinesAPI.sln ./
COPY src/Project/TuringMachinesAPI.csproj ./src/Project/

RUN dotnet restore src/Project/TuringMachinesAPI.csproj

COPY . .

RUN dotnet publish src/Project/TuringMachinesAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TuringMachinesAPI.dll"]