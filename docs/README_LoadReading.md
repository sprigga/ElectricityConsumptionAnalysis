# 負載讀數導入系統使用說明

## 📋 概述

本系統實現了從 Excel「負載交叉表」工作表到資料庫的自動化數據導入功能，採用正規化的資料模型設計。

## 🎯 功能特點

- ✅ 將 Excel 交叉表格式轉換為正規化的時間序列數據
- ✅ 支援每 30 分鐘間隔的負載數據
- ✅ RESTful API 接口
- ✅ 自動數據驗證與錯誤處理
- ✅ 批量導入優化（1488 筆記錄約 566ms）
- ✅ SQLite/SQL Server 雙資料庫支援

## 📊 數據模型

### LoadReading 模型

```csharp
public class LoadReading
{
    public int Id { get; set; }                    // 主鍵
    public DateTime Timestamp { get; set; }        // 時間戳記（精確到分鐘）
    public decimal LoadValue { get; set; }         // 負載值
    public string? DataSource { get; set; }        // 數據來源
    public DateTime ImportedAt { get; set; }       // 導入時間
    public string? Remarks { get; set; }           // 備註
}
```

### 數據轉換

**Excel 格式（交叉表）：**
```
Time  | 01/10/2024 | 02/10/2024 | ...
00:00 | 535.972    | 284.596    | ...
00:30 | 505.920    | 231.542    | ...
```

**資料庫格式（正規化）：**
```
Id | Timestamp           | LoadValue | DataSource
1  | 2024-10-01 00:00:00 | 535.972   | 負載交叉表
2  | 2024-10-01 00:30:00 | 505.920   | 負載交叉表
3  | 2024-10-02 00:00:00 | 284.596   | 負載交叉表
```

## 🚀 快速開始

### 1. 安裝依賴套件

```bash
dotnet restore
```

### 2. 建立資料庫

```bash
# 創建遷移（如果尚未創建）
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# 更新資料庫
dotnet ef database update
```

### 3. 啟動應用程式

```bash
dotnet run
```

應用程式將在 `http://localhost:5254` 啟動。

### 4. 導入數據

使用 API 導入預設 Excel 文件：

```bash
curl -X POST http://localhost:5254/api/LoadReading/import
```

## 📡 API 端點

### 1. 導入數據

**從預設 Excel 文件導入**
```http
POST /api/LoadReading/import
```

**回應範例：**
```json
{
  "isSuccess": true,
  "importedCount": 1488,
  "skippedCount": 0,
  "errorMessage": null,
  "messages": ["成功導入 1488 筆記錄"],
  "elapsedMilliseconds": 566
}
```

**從自訂 Excel 文件導入**
```http
POST /api/LoadReading/import/custom?filePath=/path/to/file.xlsx&sheetName=負載交叉表
```

### 2. 查詢數據

**取得所有記錄**
```http
GET /api/LoadReading
```

**根據日期範圍查詢**
```http
GET /api/LoadReading/range?startDate=2024-10-01&endDate=2024-10-02
```

**取得記錄總數**
```http
GET /api/LoadReading/count
```

### 3. 刪除數據

**刪除指定日期範圍的記錄**
```http
DELETE /api/LoadReading/range?startDate=2024-10-01&endDate=2024-10-31
```

### 4. 驗證 Excel 格式

```http
POST /api/LoadReading/validate?filePath=/path/to/file.xlsx&sheetName=負載交叉表
```

## 🧪 測試

### 使用 curl 測試

```bash
# 1. 檢查記錄數
curl http://localhost:5254/api/LoadReading/count

# 2. 導入數據
curl -X POST http://localhost:5254/api/LoadReading/import

# 3. 查詢部分數據
curl 'http://localhost:5254/api/LoadReading/range?startDate=2024-10-01&endDate=2024-10-02'
```

### 使用測試腳本

```bash
chmod +x test-import-api.sh
./test-import-api.sh
```

## 📁 項目結構

```
PowerAnalysis/
├── Models/
│   ├── LoadReading.cs              # 負載讀數模型
│   └── ErrorViewModel.cs
├── Data/
│   ├── ApplicationDbContext.cs     # 資料庫上下文
│   ├── Migrations/                 # EF Core 遷移文件
│   └── reference/
│       └── ElectricityConsumptionDifferenceTable.xlsx  # Excel 數據源
├── Repositories/
│   ├── ILoadReadingRepository.cs   # Repository 介面
│   └── LoadReadingRepository.cs    # Repository 實作
├── Services/
│   ├── ILoadReadingImportService.cs    # 導入服務介面
│   └── LoadReadingImportService.cs     # 導入服務實作
├── Controllers/
│   └── LoadReadingController.cs    # API Controller
├── Program.cs                      # 應用程式入口
└── PowerAnalysis.csproj            # 項目配置
```

## 🔧 配置說明

### 資料庫配置

預設使用 SQLite，資料庫文件位於：`PowerAnalysis.db`

**切換到 SQL Server：**

1. 在 [Program.cs](Program.cs#L14-L26) 中，註解 SQLite 配置並取消註解 SQL Server 配置

2. 在 `appsettings.json` 中添加連接字串：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PowerAnalysis;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

## 📊 導入結果驗證

導入成功後，你可以驗證以下內容：

- **總記錄數**：1488 筆（48 個時間點 × 31 天）
- **時間範圍**：2024-10-01 00:00 ~ 2024-10-31 23:30
- **時間間隔**：每 30 分鐘一筆記錄
- **數據示例**：
  - 2024-10-01 00:00 → 535.972
  - 2024-10-01 00:30 → 505.920
  - 2024-10-01 01:00 → 490.536

## 🎨 設計優勢

### 1. 正規化設計
- ✅ 消除數據冗餘
- ✅ 提升查詢效能
- ✅ 易於維護和擴展

### 2. 時間戳記索引
- ✅ 唯一約束防止重複
- ✅ 複合索引優化查詢
- ✅ 支援高效時間範圍查詢

### 3. Repository 模式
- ✅ 關注點分離
- ✅ 易於單元測試
- ✅ 可替換數據源

### 4. 批量導入優化
- ✅ 使用 `AddRangeAsync` 批量插入
- ✅ 一次事務處理所有記錄
- ✅ 1488 筆記錄約 566ms

## 🔍 常見問題

### Q1: 如何處理重複數據？

資料庫已設置 Timestamp 唯一約束，重複導入會拋出異常。建議在導入前先刪除舊數據：

```bash
curl -X DELETE 'http://localhost:5254/api/LoadReading/range?startDate=2024-10-01&endDate=2024-10-31'
```

### Q2: 如何自訂 Excel 文件路徑？

使用自訂導入 API：

```bash
curl -X POST 'http://localhost:5254/api/LoadReading/import/custom?filePath=/path/to/your/file.xlsx&sheetName=YourSheetName'
```

### Q3: 支援哪些日期格式？

服務支援以下日期格式：
- `dd/MM/yyyy`（預設）
- `yyyy/MM/dd`
- `yyyy-MM-dd`
- `dd-MM-yyyy`

### Q4: 如何查詢特定日期的數據？

```bash
curl 'http://localhost:5254/api/LoadReading/range?startDate=2024-10-15T00:00:00&endDate=2024-10-15T23:59:59'
```

## 🛠️ 技術棧

- **框架**：ASP.NET Core 8.0
- **ORM**：Entity Framework Core 8.0
- **資料庫**：SQLite（開發）/ SQL Server（生產）
- **Excel 處理**：EPPlus 7.0
- **設計模式**：Repository Pattern, Dependency Injection

## 📝 授權

本項目使用 EPPlus 的非商業授權。如需商業用途，請購買 EPPlus 商業授權。

---

**最後更新**: 2025-11-12
**版本**: 1.0.0
