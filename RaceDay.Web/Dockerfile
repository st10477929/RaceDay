# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.Web.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Where the RaceDay API is. Override at run time with -e ApiBaseUrl=...
ENV ApiBaseUrl=http://host.docker.internal:5080/
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
