FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["F1.Web.csproj", "."]
RUN dotnet restore "F1.Web.csproj"
COPY . .
RUN dotnet publish "F1.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY nginx.conf /etc/nginx/templates/default.conf.template

# Explicitly copy appsettings.json if it's not in wwwroot by default (though publish usually handles this)
# COPY --from=build /app/publish/wwwroot/appsettings.json .

EXPOSE 80
