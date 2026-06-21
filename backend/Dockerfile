FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /sln

COPY ./*.sln ./

COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done

COPY . .
WORKDIR /sln/src/Loteria.Console
RUN dotnet publish --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=America/Cuiaba

COPY --from=build /app .

ENTRYPOINT ["dotnet", "./Loteria.Console.dll"]