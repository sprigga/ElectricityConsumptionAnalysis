# 数据访问层实现计划

## 概述
此文档详细描述了如何实现数据访问层，以便将 "負載交叉表" 数据规范化存储到数据库中。

## 技术架构

### 依赖项
- Entity Framework Core 用于 ORM 映射
- EPPlus 或类似库用于读取 Excel 文件
- Microsoft.EntityFrameworkCore.SqlServer (或适合的数据库提供者)

### 架构模式
- Repository 模式: 封装数据访问逻辑
- Unit of Work 模式: 管理事务和变更跟踪

## 数据访问层设计

### 1. 接口定义

#### ILoadCategoryRepository
- GetAllAsync(): 获取所有负载类别
- GetByIdAsync(int id): 根据ID获取负载类别
- AddAsync(LoadCategory entity): 添加负载类别
- Update(LoadCategory entity): 更新负载类别
- DeleteAsync(int id): 删除负载类别
- FindByNameAsync(string name): 根据名称查找负载类别

#### ILoadCrossEntryRepository
- GetAllAsync(): 获取所有负载交叉条目
- GetByIdAsync(int id): 根据ID获取负载交叉条目
- AddAsync(LoadCrossEntry entity): 添加负载交叉条目
- Update(LoadCrossEntry entity): 更新负载交叉条目
- DeleteAsync(int id): 删除负载交叉条目
- GetByLoadCategoryIdsAsync(int fromId, int toId): 根据起始和目标ID获取条目
- GetByTimePeriodAsync(int timePeriodId): 根据时间周期获取条目

#### ITimePeriodRepository
- GetAllAsync(): 获取所有时间周期
- GetByIdAsync(int id): 根据ID获取时间周期
- AddAsync(TimePeriod entity): 添加时间周期
- Update(TimePeriod entity): 更新时间周期
- DeleteAsync(int id): 删除时间周期
- FindByNameAsync(string name): 根据名称查找时间周期

### 2. 实现类

#### LoadCategoryRepository
- 实现 ILoadCategoryRepository 接口
- 包含与 LoadCategory 相关的特定数据访问逻辑

#### LoadCrossEntryRepository
- 实现 ILoadCrossEntryRepository 接口
- 包含与 LoadCrossEntry 相关的特定数据访问逻辑

#### TimePeriodRepository
- 实现 ITimePeriodRepository 接口
- 包含与 TimePeriod 相关的特定数据访问逻辑

### 3. 单元工作模式

#### IUnitOfWork
- LoadCategoryRepository: 负载类别仓库
- LoadCrossEntryRepository: 负载交叉条目仓库
- TimePeriodRepository: 时间周期仓库
- SaveChangesAsync(): 保存所有变更到数据库

## Excel 数据导入流程

### 1. 文件解析阶段
- 读取 "ElectricityConsumptionDifferenceTable.xlsx" 文件
- 定位 "負載交叉表" 工作表
- 验证工作表结构和标题行

### 2. 数据验证阶段
- 验证数据格式和数据类型
- 检查必需字段是否存在
- 验证数据范围和约束

### 3. 数据转换阶段
- 将 Excel 数据映射到模型对象
- 转换日期和数值格式
- 建立实体间的关系

### 4. 数据存储阶段
- 使用 Repository 模式存储数据
- 处理重复数据检测
- 实现事务管理确保数据一致性

## 服务层设计

### ILoadCrossTableService
- ImportFromExcel(string filePath): 从Excel文件导入数据
- ExportToExcel(string filePath): 导出数据到Excel文件
- GetCrossTableData(): 获取交叉表数据
- CalculateDifferences(): 计算差异值

## 错误处理策略

### 1. 数据验证错误
- 记录验证失败的行号和列号
- 提供详细的错误信息

### 2. 数据库错误
- 实现重试机制
- 记录数据库操作错误

### 3. 文件访问错误
- 检查文件是否存在和访问权限
- 处理文件锁和并发访问

## 性能优化考虑

### 1. 批量操作
- 使用 BulkInsert 提高数据导入性能
- 分批处理大数据集

### 2. 索引策略
- 在经常查询的字段上建立索引
- 考虑复合索引以优化特定查询

### 3. 连接池
- 配置适当的数据库连接池大小
- 使用异步操作减少线程阻塞

## 安全考虑

### 1. 数据验证
- 验证用户上传的文件类型
- 防止恶意内容注入

### 2. 访问控制
- 实施适当的权限控制
- 记录数据访问日志

## 测试计划

### 1. 单元测试
- 测试每个 Repository 方法
- 测试数据验证逻辑

### 2. 集成测试
- 测试 Excel 文件导入功能
- 测试数据完整性和一致性

### 3. 性能测试
- 测试大数据量导入性能
- 测试查询性能

## 部署注意事项

### 1. 数据库迁移
- 创建数据库迁移脚本
- 准备初始数据种子

### 2. 配置管理
- 配置数据库连接字符串
- 设置上传文件大小限制

### 3. 监控和日志
- 实施操作日志记录
- 监控数据导入性能