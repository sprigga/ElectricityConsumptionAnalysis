#!/bin/bash

echo "=== 負載交叉表導入 API 測試腳本 ==="
echo ""

# API 基礎 URL - 修正為正確的端口 8765
# 原有程式碼使用 5254 端口，已修改為 Docker 容器實際使用的 8765 端口
# API_BASE="http://localhost:5254/api/LoadReading"
API_BASE="http://localhost:8765/api/LoadReading"

# 等待應用程式啟動
echo "等待應用程式啟動..."
sleep 3

# 1. 檢查目前記錄數
echo ""
echo "1. 檢查目前資料庫記錄數..."
curl -s "${API_BASE}/count"

# 2. 執行導入
echo ""
echo ""
echo "2. 執行從預設 Excel 文件導入..."
curl -X POST -s "${API_BASE}/import"

# 3. 再次檢查記錄數
echo ""
echo ""
echo "3. 導入後的記錄數..."
curl -s "${API_BASE}/count"

# 4. 查詢日期範圍
echo ""
echo ""
echo "4. 查詢資料日期範圍..."
curl -s "${API_BASE}/daterange"

# 5. 查詢部分數據（查詢 2024-10-01 的資料）
echo ""
echo ""
echo "5. 查詢 2024-10-01 的資料..."
curl -s "${API_BASE}/range?startDate=2024-10-01&endDate=2024-10-01" | head -c 500

echo ""
echo ""
echo "測試完成！"
