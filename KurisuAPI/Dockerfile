FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY KurisuCore/ KurisuCore/
COPY KurisuAPI/ KurisuAPI/

WORKDIR /src/KurisuAPI
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "KurisuAPI.dll"]