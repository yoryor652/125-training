# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

沒變, 根據練習順序處理

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）
在修復練習2的bug
目前系統顯示訂單列表共有 11 頁，每頁 20 筆。回到第一頁時，我的新訂單並不在裡面，
要翻到第 11 頁才找到。點到分頁器顯示的最後一頁（第 11 頁）時，表格是空白的，沒有任何資料，
但分頁器仍然顯示這是有效的一頁。

以上提問明確説明了問題和想要的結果

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）
沒發現agent 修改錯誤, 測試也沒錯誤

### 4. 我會帶回日常工作的一招
- 改任何會讀寫同一個狀態欄位的函式前，先手動追蹤這個欄位的「讀 / 寫時間軸」，再判斷 guard 邏輯。具體做法：
  a. 在函式裡搜尋所有對同一個欄位（例如 order.Status）的賦值與條件判斷，依程式碼由上到下的執行順序列成一行一行的清單（標行號）。
  b. 檢查每一個 if (欄位 == ...) 的 guard，往上找同一個欄位最近一次被賦值的地方——確認這個 guard 執行時讀到的是「賦值前的舊值」還是「賦值後的新值」。
  c. 如果 guard 是在賦值之後才執行，而且判斷的正是剛被覆蓋的那個欄位，就要立刻懷疑：這個條件是不是永遠不會成立（或永遠成立），變成事實上的死代碼。
  d. 找到後，補一個能重現該分支的測試（例如「取消一筆 Pending 訂單後，庫存應該要加回去」），讓它先紅燈，再修正順序（把 guard 移到賦值之前，或改成讀取賦值前先存下來的舊值）。

這一招不是「多驗證」的口號，而是這次抓到 Bug 3（CancelOrderAsync 裡 order.Status = Cancelled 先執行，guard 才檢查 order.Status == Pending/Confirmed，導致還原庫存的邏輯永遠不會跑）的具體診斷步驟，可以直接套用在任何「先改狀態、再用同一個狀態做判斷」的程式碼上。

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
可以
2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**
是的
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方
是的

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式
是的
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文
是的
3. 每個修復都回到頁面驗證過症狀消失
是的
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠
是的
5. 三個獨立 commit，message 說明症狀與根因
push commit 時沒看到這段, 在這邊補上
- Bug 1（分頁）：page 當成 0-index 用，Skip(page*pageSize) 多跳過一頁。
- Bug 2（Gold 折扣）：同一個折扣算了兩次——建立訂單時先打折存進 UnitPriceSnapshot，CalculateTotal 又打一次折。
- Bug 3（取消訂單不還庫存）：先把 order.Status 改成 Cancelled，guard 才檢查 Status == Pending/Confirmed，條件永遠不成立。
6. （思考題）為什麼原本的測試沒抓到這三個 bug？
- Bug 1：測試只斷言 TotalCount/TotalPages，沒檢查 Items 實際內容，Skip 錯了也看不出來。
- Bug 2：測試用的是 Standard 客戶（折扣 0%），折扣算一次或兩次結果都一樣，蓋住了 bug。
- Bug 3：測試只斷言 Status == Cancelled，沒檢查庫存有沒有還原，guard 死代碼不影響狀態斷言。

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
-
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
-
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
-
4. 停售（已停售 badge）商品不出現在列表
-
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）
-
6. 至少 3 個新測試，`dotnet test` 全綠
-

練習 4

1. 重構後 `dotnet test` 全綠
是的
2. 我能說出這次重構「改善了什麼、沒有改變什麼」
**改善了什麼**
- `CreateOrderAsync` 從驗證與業務邏輯糾在一起，變成呼叫 `OrderValidator.ValidateOrderRequest(...)` / `ValidateLine(...)` 兩個純函式，可讀性提升。
- 驗證規則獨立到 `OrderValidator`，不依賴 repository/DbContext，未來更容易單獨測試。

**沒有改變什麼**
- 所有錯誤訊息文字、檢查順序、短路 vs 累積的錯誤收集風格。
- 逐行迴圈裡「扣庫存、建立 OrderItem」的副作用時機（含失敗時庫存已扣但未存檔的細節）。
- `IOrderService` 介面、`ServiceResult<Order>` 回傳型別、Controller 呼叫方式。
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）
有

---

## 附錄：值得留下的對話片段
**情境**：對 `OrderService.CreateOrderAsync` 做「抽取驗證邏輯」的重構前，要求先出計畫、不動任何檔案。

> 在動手之前，先不要修改任何檔案，給我一份重構計畫，包含：
> 1. 目前 CreateOrderAsync 裡有哪幾種驗證邏輯？逐條列出，並指出各自的檔案位置與行號
> 2. 打算怎麼抽：抽成私有方法？還是獨立的 OrderValidator class？
> 3. 這次重構「只」處理驗證邏輯抽取——如果發現其他可以順便重構的地方，先列出來但不要動

這段對話示範了「先計畫、列出範圍外事項、確認後才執行」的流程——重構前逐條列出驗證邏輯與行號、明確劃出「這次不動」的項目


Activity 2 -  練習 3

差異在於沒有mcp的話, agent 自己寫 SQL 直接查資料庫，取得資料 （比較慢），有 mcp的話會 呼叫 low_stock 工具，工具內部呼叫系統既有的 ProductRepository 邏輯查詢。
核心差異是沒有 MCP時，agent 是自己臨時拼湊查詢邏輯，有 MCP 時 答案保證跟系統其他地方（如 /Products 頁面）用的是同一套邏輯，不會兩邊對不上。

Activity 2 -  練習 4
全部執行成功並和expected result 相同

Activity 2 -  練習 5

Tool 是	agent 可以呼叫的動作， Resource 是背景資料，由 client 決定何時放進 context，prompty 則是寫好的範本，像 slash command 一樣觸發
