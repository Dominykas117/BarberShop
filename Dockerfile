# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
# For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:35792ea4ad1db051981f62b313f1be3b46b1f45cadbaa3c288cd0d3056eefb83 AS build-env
WORKDIR /App

# Copy everything
#COPY . ./  #Skiriasi video ir githube codas. cia is githubo.
#COPY C:/Users/domin/OneDrive/Desktop/KTU_IV_metai/Bakalauras/Barbershop/BarberShop/source/DemoRest2024/DemoRest2024Live
COPY source/DemoRest2024/DemoRest2024Live
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:6c4df091e4e531bb93bdbfe7e7f0998e7ced344f54426b7e874116a3dc3233ff
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "DemoRest2024Live.dll"]

## This stage is used when running from VS in fast mode (Default for Debug configuration)
#FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-1809 AS base
#WORKDIR /app
#EXPOSE 8080
#
#
## This stage is used to build the service project
#FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-1809 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["DemoRest2024Live/DemoRest2024Live.csproj", "DemoRest2024Live/"]
#RUN dotnet restore "./DemoRest2024Live/DemoRest2024Live.csproj"
#COPY . .
#WORKDIR "/src/DemoRest2024Live"
#RUN dotnet build "./DemoRest2024Live.csproj" -c %BUILD_CONFIGURATION% -o /app/build
#
## This stage is used to publish the service project to be copied to the final stage
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./DemoRest2024Live.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false
#
## This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "DemoRest2024Live.dll"]