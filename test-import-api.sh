#!/bin/bash

echo "=== 負載交叉表導入 API 測試腳本 ==="
echo ""

# API 基礎 URL
API_BASE="http://localhost:5254/api/LoadReading"

# 等待應用程式啟動
echo "等待應用程式啟動..."
sleep 3

# 1. 檢查目前記錄數
echo ""
echo "1. 檢查目前資料庫記錄數..."
curl -s "${API_BASE}/count" | python3 -m json.tool

# 2. 執行導入
echo ""
echo ""
echo "2. 執行從預設 Excel 文件導入..."
curl -X POST -s "${API_BASE}/import" | python3 -m json.tool

# 3. 再次檢查記錄數
echo ""
echo ""
echo "3. 導入後的記錄數..."
curl -s "${API_BASE}/count" | python3 -m json.tool

# 4. 查詢部分數據（取得前10筆）
echo ""
echo ""
echo "4. 查詢前 10 筆記錄..."
curl -s "${API_BASE}" | python3 -c "import sys, json; data = json.load(sys.stdin); print(json.dumps(data[:10], indent=2, ensure_ascii=False))"

echo ""
echo ""
echo "測試完成！"
