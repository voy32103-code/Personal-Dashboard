# 1. Giai đoạn Build (Dùng ảnh SDK để biên dịch code)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file dự án vào trước để restore (tối ưu cache)
COPY ["MyPortfolio.Web/MyPortfolio.Web.csproj", "MyPortfolio.Web/"]
COPY ["MyPortfolio.Core/MyPortfolio.Core.csproj", "MyPortfolio.Core/"]
COPY ["MyPortfolio.Infrastructure/MyPortfolio.Infrastructure.csproj", "MyPortfolio.Infrastructure/"]

# Tải các thư viện về
RUN dotnet restore "MyPortfolio.Web/MyPortfolio.Web.csproj"

# Copy toàn bộ code còn lại vào
COPY . .
WORKDIR "/src/MyPortfolio.Web"
# Build ra bản Release
RUN dotnet build "MyPortfolio.Web.csproj" -c Release -o /app/build

# 2. Giai đoạn Publish (Đóng gói gọn nhẹ)
FROM build AS publish
RUN dotnet publish "MyPortfolio.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Giai đoạn Chạy (Dùng ảnh runtime nhẹ hơn để chạy web)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Tạo thư mục uploads để tránh lỗi thiếu folder
RUN mkdir -p wwwroot/uploads

ENTRYPOINT ["dotnet", "MyPortfolio.Web.dll"]