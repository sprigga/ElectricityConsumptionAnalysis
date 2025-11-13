#!/bin/bash

echo "=== 測試負載圖表的新功能 ==="
echo ""

echo "1. 測試獲取日期範圍 API"
curl -s http://localhost:5000/api/loadreading/daterange | jq '.'
echo ""

echo "2. 測試帶時間的聚合查詢 API (2024-11-01 08:30 到 2024-11-02 17:00)"
curl -s "http://localhost:5000/api/loadreading/aggregated?startDate=2024-11-01%2008:30&endDate=2024-11-02%2017:00&days=2" | jq '.' | head -20
echo ""

echo "3. 測試完整 URL"
echo "請在瀏覽器中訪問: http://localhost:5000/Home/LoadReadingChart"
echo ""
echo "功能測試項目:"
echo "  ✓ 時間選擇器（30分鐘間隔，預設 00:00）"
echo "  ✓ 查詢數據按鈕"
echo "  ✓ 圖表顯示"
echo "  ✓ 數據報表列表（帶分頁功能，每頁50筆）"
echo ""
