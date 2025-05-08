Use .NET 6.0 SDK for building

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build WORKDIR /app

Copy csproj and restore dependencies

COPY *.csproj ./ RUN dotnet restore

Copy the rest of the code and build

COPY . ./ RUN dotnet publish -c Release -o out

Create runtime image

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime WORKDIR /app COPY --from=build /app/out ./

Expose port for WebSocket

EXPOSE 8080

Set environment variables

ENV ASPNETCORE_URLS=http://+:8080

Run the application

ENTRYPOINT ["dotnet", "GlitcherServer.dll"]
