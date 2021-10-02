FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /app

COPY ./RaidsBot/*.csproj /app/RaidsBot/
COPY ./*.sln /app/
RUN dotnet restore

COPY . /app/
RUN dotnet publish -c Release --no-self-contained --no-restore -o /publish

FROM mcr.microsoft.com/dotnet/runtime:3.1
WORKDIR /app
COPY --from=build /publish /app
CMD ["dotnet", "RaidsBot.dll"]