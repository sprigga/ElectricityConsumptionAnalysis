# 使用 .NET 8.0 SDK 作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制 csproj 文件并还原依赖项
COPY ["PowerAnalysis.csproj", "./"]
RUN dotnet restore "PowerAnalysis.csproj"

# 复制所有源代码
COPY . .

# 构建应用程序
RUN dotnet build "PowerAnalysis.csproj" -c Release -o /app/build

# 发布应用程序
FROM build AS publish
RUN dotnet publish "PowerAnalysis.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 使用 .NET 8.0 ASP.NET 运行时作为最终镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 安装必要的运行时依赖
RUN apt-get update && apt-get install -y \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# 从发布阶段复制应用程序
COPY --from=publish /app/publish .

# 创建数据目录用于 SQLite 数据库
RUN mkdir -p /app/data

# 暴露端口
EXPOSE 80
EXPOSE 443

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# 启动应用程序
ENTRYPOINT ["dotnet", "PowerAnalysis.dll"]
