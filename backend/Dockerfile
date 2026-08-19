FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LimousineBooking.sln ./
COPY src/LimousineBooking.Api/LimousineBooking.Api.csproj src/LimousineBooking.Api/
COPY src/LimousineBooking.Application/LimousineBooking.Application.csproj src/LimousineBooking.Application/
COPY src/LimousineBooking.Domain/LimousineBooking.Domain.csproj src/LimousineBooking.Domain/
COPY src/LimousineBooking.Infrastructure/LimousineBooking.Infrastructure.csproj src/LimousineBooking.Infrastructure/
COPY src/LimousineBooking.Tests/LimousineBooking.Tests.csproj src/LimousineBooking.Tests/
RUN dotnet restore src/LimousineBooking.Api/LimousineBooking.Api.csproj

COPY . .
RUN dotnet publish src/LimousineBooking.Api/LimousineBooking.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LimousineBooking.Api.dll"]
