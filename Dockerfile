# WebMap imaji. Derleme (D:\MyApp\WebMap klasorunde): docker build -t webmap:1.0 .
# Iki asama: SDK sadece derlerken kullanilir, son imajda ASP.NET Core runtime ve publish ciktisi kalir.

# ---- 1. asama: derleme ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Once sadece proje dosyasi: csproj degismedikce NuGet paketleri Docker onbelleginden gelir.
COPY WebMap.csproj .
RUN dotnet restore WebMap.csproj

# Sonra kaynak kod (.dockerignore'dakiler haric).
# Klasorde WebMap.slnx de var; proje adi yazilmazsa publish hangisini derleyecegini secemez.
COPY . .
RUN dotnet publish WebMap.csproj -c Release -o /app --no-restore

# ---- 2. asama: calistirma ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Cerez anahtarlarinin yazilacagi klasor (compose'da volume buraya baglanir).
# Volume, imajda olmayan bir klasore baglaninca sahibi root olur ve 'app' kullanicisi yazamaz;
# klasoru burada acip sahipligini vermek bunu onler.
RUN mkdir /keys && chown $APP_UID /keys

# root yerine imajda hazir bulunan yetkisiz 'app' kullanicisi.
# Port ayari gerekmez: imaj varsayilan olarak 8080'i dinler.
USER $APP_UID
ENTRYPOINT ["dotnet", "WebMap.dll"]
