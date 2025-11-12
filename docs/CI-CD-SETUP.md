# CI/CD 设置指南

本文档详细说明如何为 PowerAnalysis 项目配置 CI/CD 管道。

## 概述

CI/CD 管道使用 GitHub Actions 实现以下功能：
- 自动构建和测试
- Docker 镜像构建和推送
- 自动部署到开发和生产环境
- 安全漏洞扫描

## 工作流程图

```
代码提交/合并
    ↓
构建和测试 (build-and-test)
    ↓
Docker 镜像构建 (docker-build)
    ↓
    ├─→ 开发环境部署 (develop 分支)
    ├─→ 生产环境部署 (main 分支/tag)
    └─→ 安全扫描 (security-scan)
```

## GitHub Actions 配置

### 1. 触发条件

管道在以下情况下触发：

- **Push 事件**:
  - `main` 分支: 触发生产部署
  - `develop` 分支: 触发开发部署
  - 版本标签 (`v*`): 触发生产部署

- **Pull Request**:
  - 针对 `main` 或 `develop` 分支的 PR
  - 只执行构建和测试，不部署

### 2. GitHub Secrets 配置

#### 访问 GitHub Secrets

1. 进入 GitHub 仓库
2. 点击 `Settings` → `Secrets and variables` → `Actions`
3. 点击 `New repository secret`

#### 必需的 Secrets

**容器仓库** (已自动配置使用 GitHub Container Registry):
- `GITHUB_TOKEN`: 自动提供，无需配置

**开发环境**:
```
DEV_HOST          - 开发服务器 IP 或域名 (例: dev.example.com)
DEV_USERNAME      - SSH 登录用户名 (例: deploy)
DEV_SSH_KEY       - SSH 私钥内容 (完整的私钥文本)
DEV_PORT          - SSH 端口 (可选，默认 22)
```

**生产环境**:
```
PROD_HOST         - 生产服务器 IP 或域名 (例: prod.example.com)
PROD_USERNAME     - SSH 登录用户名 (例: deploy)
PROD_SSH_KEY      - SSH 私钥内容 (完整的私钥文本)
PROD_PORT         - SSH 端口 (可选，默认 22)
PROD_URL          - 生产环境 URL (例: https://poweranalysis.com)
```

### 3. SSH 密钥配置

#### 生成 SSH 密钥对

在本地机器上执行：

```bash
# 生成新的 SSH 密钥对 (使用 ED25519 算法)
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/deploy_key

# 查看私钥 (将内容添加到 GitHub Secrets)
cat ~/.ssh/deploy_key

# 查看公钥 (将内容添加到服务器)
cat ~/.ssh/deploy_key.pub
```

#### 在服务器上配置公钥

在目标服务器上执行：

```bash
# 创建 .ssh 目录
mkdir -p ~/.ssh
chmod 700 ~/.ssh

# 添加公钥到 authorized_keys
echo "your-public-key-content" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

#### 测试 SSH 连接

```bash
# 测试连接
ssh -i ~/.ssh/deploy_key username@server-ip

# 如果成功，你应该能够登录到服务器
```

## 服务器端配置

### 1. 安装 Docker

在部署目标服务器上安装 Docker 和 Docker Compose：

```bash
# 安装 Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# 将用户添加到 docker 组
sudo usermod -aG docker $USER

# 安装 Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# 验证安装
docker --version
docker-compose --version
```

### 2. 准备部署目录

```bash
# 创建部署目录
sudo mkdir -p /opt/poweranalysis
sudo chown $USER:$USER /opt/poweranalysis
cd /opt/poweranalysis

# 创建必要的子目录
mkdir -p data backups nginx/ssl Data/reference
```

### 3. 复制配置文件

将以下文件从项目复制到服务器：

```bash
# 在本地执行
scp docker-compose.prod.yml user@server:/opt/poweranalysis/docker-compose.yml
scp nginx/nginx.conf user@server:/opt/poweranalysis/nginx/
scp .env.example user@server:/opt/poweranalysis/.env

# 在服务器上编辑 .env 文件
nano /opt/poweranalysis/.env
```

### 4. 配置环境变量

编辑 `/opt/poweranalysis/.env` 文件：

```bash
DOCKER_REGISTRY=ghcr.io
DOCKER_USERNAME=your-github-username
TAG=latest
ASPNETCORE_ENVIRONMENT=Production
```

## 部署流程详解

### 开发环境部署

当代码推送到 `develop` 分支时：

1. 代码检出和构建
2. 运行单元测试
3. 构建 Docker 镜像并打标签 (如: `develop-abc123`)
4. 推送镜像到容器仓库
5. SSH 连接到开发服务器
6. 执行部署脚本：
   ```bash
   cd /opt/poweranalysis
   docker-compose pull
   docker-compose up -d
   docker-compose logs --tail=50
   ```

### 生产环境部署

当代码推送到 `main` 分支或创建版本标签时：

1. 执行所有开发环境的步骤
2. 额外打标签 `latest` 和版本号标签
3. SSH 连接到生产服务器
4. 执行部署脚本：
   ```bash
   cd /opt/poweranalysis
   docker-compose -f docker-compose.prod.yml pull
   docker-compose -f docker-compose.prod.yml up -d
   docker-compose -f docker-compose.prod.yml logs --tail=50
   ```
5. 执行健康检查
6. 如果健康检查失败，部署失败

### 安全扫描

每次构建后会自动运行 Trivy 扫描：

1. 扫描构建的 Docker 镜像
2. 检测已知的安全漏洞
3. 生成 SARIF 报告
4. 将结果上传到 GitHub Security 标签

## 监控和日志

### 查看 GitHub Actions 日志

1. 进入 GitHub 仓库
2. 点击 `Actions` 标签
3. 选择工作流运行记录
4. 查看各个 job 的详细日志

### 查看部署日志

在服务器上查看应用日志：

```bash
# 查看实时日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs web

# 查看最近的日志
docker-compose logs --tail=100
```

## 回滚策略

### 快速回滚

使用特定版本的镜像回滚：

```bash
# 在服务器上
cd /opt/poweranalysis

# 修改 .env 文件中的 TAG
echo "TAG=v1.2.3" > .env

# 重新部署
docker-compose pull
docker-compose up -d
```

### 使用 Git 标签回滚

1. 在 GitHub 上找到稳定的 commit
2. 创建新的标签：
   ```bash
   git tag -a v1.2.4-hotfix -m "Rollback to stable version"
   git push origin v1.2.4-hotfix
   ```
3. GitHub Actions 会自动部署该版本

## 环境管理

### GitHub Environments

建议配置 GitHub Environments 以获得更好的部署控制：

1. 进入 `Settings` → `Environments`
2. 创建环境：
   - `development`
   - `production`

3. 为生产环境配置保护规则：
   - Required reviewers: 需要审批才能部署
   - Wait timer: 等待时间
   - Deployment branches: 限制可部署的分支

### 多环境配置

在 `.github/workflows/ci-cd.yml` 中已配置环境：

```yaml
environment:
  name: production
  url: https://poweranalysis.example.com
```

这允许你：
- 在 GitHub UI 中查看部署历史
- 配置部署审批流程
- 查看每个环境的当前状态

## 故障排除

### 部署失败

1. **检查 GitHub Actions 日志**:
   - 查看失败的步骤
   - 检查错误消息

2. **检查 SSH 连接**:
   ```bash
   # 验证 SSH 密钥
   ssh -vvv user@server
   ```

3. **检查服务器空间**:
   ```bash
   df -h
   docker system df
   ```

4. **检查 Docker 日志**:
   ```bash
   docker-compose logs
   ```

### 镜像拉取失败

1. **验证容器仓库访问权限**:
   ```bash
   docker login ghcr.io -u USERNAME
   ```

2. **检查镜像是否存在**:
   ```bash
   docker pull ghcr.io/username/poweranalysis:latest
   ```

### 健康检查失败

1. **检查应用是否正常运行**:
   ```bash
   docker-compose ps
   curl http://localhost:8080/health
   ```

2. **检查防火墙规则**:
   ```bash
   sudo ufw status
   ```

## 最佳实践

1. **使用版本标签**: 为发布版本创建 Git 标签
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

2. **保护主分支**: 在 GitHub 设置中启用分支保护
   - 要求 PR 审查
   - 要求状态检查通过
   - 要求分支是最新的

3. **定期备份**: 在部署前自动备份数据库
   ```yaml
   - name: Backup database
     run: |
       timestamp=$(date +%Y%m%d_%H%M%S)
       docker-compose exec -T web cp /app/data/PowerAnalysis.db /app/backups/backup_$timestamp.db
   ```

4. **监控资源使用**: 定期检查服务器资源
   ```bash
   docker stats
   ```

5. **日志轮转**: 配置 Docker 日志大小限制
   ```yaml
   logging:
     driver: "json-file"
     options:
       max-size: "10m"
       max-file: "3"
   ```

## 扩展功能

### 添加 Slack 通知

在 workflow 中添加：

```yaml
- name: Slack Notification
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    text: 'Deployment to production completed!'
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
  if: always()
```

### 添加性能测试

```yaml
- name: Performance Test
  run: |
    docker run --rm -i loadimpact/k6 run - <tests/load-test.js
```

### 添加数据库迁移

```yaml
- name: Run Migrations
  run: |
    docker-compose exec -T web dotnet ef database update
```

## 参考资源

- [GitHub Actions 文档](https://docs.github.com/en/actions)
- [Docker 文档](https://docs.docker.com/)
- [Docker Compose 文档](https://docs.docker.com/compose/)
- [Trivy 安全扫描](https://github.com/aquasecurity/trivy)

## 总结

通过本指南，您应该能够：
- 配置完整的 CI/CD 管道
- 自动化构建、测试和部署流程
- 实现多环境部署策略
- 监控和维护部署流程

如有问题，请查看 [DEPLOYMENT.md](DEPLOYMENT.md) 了解更多部署细节。
