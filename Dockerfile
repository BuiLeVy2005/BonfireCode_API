# Sử dụng image .NET SDK để build dự án
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy toàn bộ mã nguồn vào trong container
COPY . ./
# Tải các thư viện cần thiết
RUN dotnet restore
# Đóng gói dự án (Release)
RUN dotnet publish -c Release -o out

# Sử dụng image ASP.NET Runtime để chạy ứng dụng (nhẹ hơn)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .

# Mở cổng 8080 cho Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Khởi chạy dự án
ENTRYPOINT ["dotnet", "CodeShareAPI.dll"]
