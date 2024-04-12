FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app
EXPOSE 8080

#COPY CSPROJ AND RESTORE AS DISTINCT LAYERS
COPY *.csproj ./
RUN dotnet restore 

#COPY EVERYTHING ELSE AND BUILD
COPY . ./
RUN dotnet publish -c Release -o out

#BUILD RUNTIME IMAGE
FROM mcr.microsoft.com/dotnet/aspnet:7.0.10
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "API.dll" ]
