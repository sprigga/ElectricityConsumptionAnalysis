# PowerAnalysis 部署指南

本指南说明如何使用 Docker 和 Docker Compose 部署 PowerAnalysis 应用程序。

## 目录结构

```
PowerAnalysis/
├── Dockerfile                    # Docker 镜像构建文件
├── .dockerignore                # Docker 构建忽略文件
├── docker-compose.yml           # 开发环境 Docker Compose 配置
├── docker-compose.prod.yml      # 生产环境 Docker Compose 配置
├── nginx/
│   └── nginx.conf              # Nginx 反向代理配置
└── .github/
    └── workflows/
        └── ci-cd.yml           # GitHub Actions CI/CD 管道
```

## 快速开始

### 1. 本地开发环境

#### 前置要求
- Docker 20.10 或更高版本
- Docker Compose 2.0 或更高版本

#### 启动应用

```bash
# 构建并启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

应用将在以下地址可访问：
- 应用程序: http://localhost:8080
- HTTPS (如果配置): https://localhost:8443

#### 启动包含 Nginx 的完整环境

```bash
# 使用 profile 启动 Nginx
docker-compose --profile with-nginx up -d
```

#### 启动包含自动备份的环境

```bash
# 使用 profile 启动数据库备份服务
docker-compose --profile with-backup up -d
```

### 2. 生产环境部署

#### 前置要求
- Linux 服务器 (Ubuntu 20.04+ 或 CentOS 8+ 推荐)
- Docker 和 Docker Compose 已安装
- 域名和 SSL 证书 (用于 HTTPS)

#### 部署步骤

1. **克隆代码或拉取镜像**

```bash
# 方式 1: 从源码构建
git clone <repository-url>
cd PowerAnalysis

# 方式 2: 使用预构建镜像
# 确保 docker-compose.prod.yml 中的镜像地址正确
```

2. **配置环境变量**

创建 `.env` 文件：

```bash
# Docker 镜像配置
DOCKER_REGISTRY=ghcr.io
DOCKER_USERNAME=your-username
TAG=latest

# 数据库配置
CONNECTIONSTRINGS__DEFAULTCONNECTION=Data Source=/app/data/PowerAnalysis.db

# 应用程序配置
ASPNETCORE_ENVIRONMENT=Production
```

3. **准备 SSL 证书** (用于 HTTPS)

```bash
# 创建 SSL 目录
mkdir -p nginx/ssl

# 将证书文件放入目录
# nginx/ssl/cert.pem
# nginx/ssl/key.pem

# 或使用 Let's Encrypt
# sudo certbot certonly --standalone -d your-domain.com
```

4. **创建必要的目录**

```bash
mkdir -p data
mkdir -p backups
mkdir -p Data/reference
```

5. **启动生产环境**

```bash
# 使用生产配置启动
docker-compose -f docker-compose.prod.yml up -d

# 查看日志
docker-compose -f docker-compose.prod.yml logs -f

# 检查健康状态
docker-compose -f docker-compose.prod.yml ps
```

## CI/CD 配置

### GitHub Actions 自动化部署

项目包含完整的 GitHub Actions CI/CD 管道，配置文件位于 `.github/workflows/ci-cd.yml`。

#### 工作流程

1. **构建和测试**: 自动构建 .NET 应用并运行测试
2. **Docker 构建**: 构建 Docker 镜像并推送到容器仓库
3. **自动部署**:
   - `develop` 分支推送到开发环境
   - `main` 分支或 tag 推送到生产环境
4. **安全扫描**: 使用 Trivy 扫描容器镜像的安全漏洞

#### 配置 GitHub Secrets

在 GitHub 仓库设置中添加以下 Secrets：

**开发环境:**
- `DEV_HOST`: 开发服务器地址
- `DEV_USERNAME`: SSH 用户名
- `DEV_SSH_KEY`: SSH 私钥
- `DEV_PORT`: SSH 端口 (可选，默认 22)

**生产环境:**
- `PROD_HOST`: 生产服务器地址
- `PROD_USERNAME`: SSH 用户名
- `PROD_SSH_KEY`: SSH 私钥
- `PROD_PORT`: SSH 端口 (可选，默认 22)
- `PROD_URL`: 生产环境 URL (用于健康检查)

#### 服务器端准备

在目标服务器上：

```bash
# 1. 安装 Docker 和 Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
sudo usermod -aG docker $USER

# 2. 创建部署目录
sudo mkdir -p /opt/poweranalysis
sudo chown $USER:$USER /opt/poweranalysis
cd /opt/poweranalysis

# 3. 复制 docker-compose 配置文件
# 将 docker-compose.yml 或 docker-compose.prod.yml 复制到此目录

# 4. 配置 SSH 密钥认证
# 将 CI/CD 的公钥添加到 ~/.ssh/authorized_keys
```

## 常用操作

### 查看日志

```bash
# 查看所有服务日志
docker-compose logs

# 查看特定服务日志
docker-compose logs web

# 实时跟踪日志
docker-compose logs -f web
```

### 更新应用

```bash
# 拉取最新镜像
docker-compose pull

# 重启服务
docker-compose up -d

# 或使用单个命令
docker-compose pull && docker-compose up -d
```

### 数据库管理

#### 备份数据库

```bash
# 手动备份
docker-compose exec web cp /app/data/PowerAnalysis.db /app/data/backup_$(date +%Y%m%d).db

# 或从宿主机
cp data/PowerAnalysis.db backups/PowerAnalysis_$(date +%Y%m%d_%H%M%S).db
```

#### 恢复数据库

```bash
# 停止应用
docker-compose down

# 恢复数据库文件
cp backups/PowerAnalysis_20240101.db data/PowerAnalysis.db

# 重启应用
docker-compose up -d
```

### 扩展和调优

#### 资源限制

在 `docker-compose.prod.yml` 中调整资源限制：

```yaml
deploy:
  resources:
    limits:
      cpus: '2'
      memory: 2G
    reservations:
      cpus: '1'
      memory: 1G
```

#### 多副本部署

```bash
# 启动多个应用实例
docker-compose up -d --scale web=3
```

## 监控和维护

### 健康检查

```bash
# 检查容器健康状态
docker-compose ps

# 测试应用健康检查端点
curl http://localhost:8080/health
```

### 容器维护

```bash
# 清理未使用的镜像
docker image prune -a

# 清理未使用的容器
docker container prune

# 清理所有未使用的资源
docker system prune -a
```

## 故障排除

### 容器无法启动

```bash
# 查看详细日志
docker-compose logs web

# 检查容器状态
docker-compose ps

# 重新构建镜像
docker-compose build --no-cache
docker-compose up -d
```

### 数据库文件权限问题

```bash
# 修复权限
sudo chown -R 1000:1000 data/
```

### 端口冲突

```bash
# 检查端口占用
sudo netstat -tulpn | grep :8080

# 修改 docker-compose.yml 中的端口映射
ports:
  - "8081:80"  # 将本地端口改为 8081
```

## 安全建议

1. **使用 HTTPS**: 在生产环境中务必配置 SSL/TLS
2. **定期更新**: 保持 Docker 镜像和依赖包更新
3. **限制访问**: 使用防火墙限制对服务器的访问
4. **备份数据**: 定期备份数据库文件
5. **监控日志**: 定期检查应用和 Nginx 日志
6. **环境隔离**: 开发、测试和生产环境分离

## 性能优化

1. **启用 Gzip 压缩**: Nginx 配置已包含
2. **静态文件缓存**: 配置浏览器缓存
3. **数据库优化**: 定期清理和优化 SQLite 数据库
4. **负载均衡**: 使用 Nginx 进行负载均衡

## 支持

如有问题，请查看：
- [Docker 文档](https://docs.docker.com/)
- [Docker Compose 文档](https://docs.docker.com/compose/)
- [ASP.NET Core 部署文档](https://docs.microsoft.com/aspnet/core/host-and-deploy/)

## 许可证

请参阅 LICENSE 文件了解详情。
