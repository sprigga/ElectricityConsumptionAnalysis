# 负载交叉表数据模型规划文档

## 项目概述
此文档描述了如何将 "ElectricityConsumptionDifferenceTable.xlsx" 中的 "負載交叉表" 工作表规范化存储到模型中的计划。

## 数据模型设计

### 1. 模型实体设计

#### LoadCategory (负载类别)
- 存储不同类型的负载信息
- 属性：
  - Id: 主键，整数类型
  - Name: 负载名称，必需，最多100字符
  - Description: 负载描述，可选，最多500字符
  - LoadCrossEntriesFrom: 从此负载类别开始的交叉条目集合
  - LoadCrossEntriesTo: 到此负载类别的交叉条目集合

#### LoadCrossEntry (负载交叉条目)
- 存储负载之间的交叉数据
- 属性：
  - Id: 主键，整数类型
  - FromLoadCategoryId: 起始负载类别ID (外键)
  - ToLoadCategoryId: 目标负载类别ID (外键)
  - DifferenceValue: 负载差异值
  - TimePeriodId: 时间周期ID (外键)，可选
  - CreatedDate: 创建日期
  - UpdatedDate: 更新日期

#### TimePeriod (时间周期)
- 存储数据的时间周期信息
- 属性：
  - Id: 主键，整数类型
  - PeriodName: 周期名称，如"2023年Q1"
  - StartDate: 开始日期
  - EndDate: 结束日期
  - Description: 描述信息

### 2. 实体关系
- LoadCategory 与 LoadCrossEntry 是一对多关系 (一个负载类别可以出现在多个交叉条目中)
- LoadCrossEntry 与 TimePeriod 是多对一关系 (多个交叉条目可以属于同一个时间周期)

## 规范化优势

### 1. 避免数据冗余
- 负载类别信息只存储一次
- 时间周期信息只存储一次
- 相比Excel表格中的重复数据，模型中每条数据只存储一次

### 2. 数据一致性
- 更新负载类别名称只需修改一个位置
- 维护参照完整性，防止孤立数据

### 3. 查询效率
- 通过外键关系快速查询相关数据
- 支持复杂的数据分析查询

### 4. 扩展性
- 可以轻松添加新属性到实体中
- 支持添加新的负载类别和时间周期

## 实施步骤

### 步骤 1: 创建模型类
1. 创建 LoadCategory.cs 模型
2. 创建 LoadCrossEntry.cs 模型
3. 创建 TimePeriod.cs 模型

### 步骤 2: 配置数据库上下文
1. 更新 ApplicationDbContext 以包含新实体
2. 配置实体关系

### 步骤 3: 创建数据访问层
1. 创建 Repository 模式接口和实现
2. 实现数据导入方法

### 步骤 4: 导入 Excel 数据
1. 读取 "負載交叉表" 工作表
2. 解析数据并映射到模型
3. 将数据保存到数据库

### 步骤 5: 验证和测试
1. 验证数据完整性
2. 测试查询功能
3. 性能优化

## 注意事项

1. 确保在导入数据前验证 Excel 文件格式
2. 处理可能的数据类型转换错误
3. 考虑大数据量导入的性能问题
4. 实现适当的数据验证规则

## 预期成果

通过此规范化模型设计，我们将能够：
- 高效存储和查询负载交叉表数据
- 维护数据完整性
- 支持复杂的数据分析需求
- 为未来的功能扩展提供坚实的基础