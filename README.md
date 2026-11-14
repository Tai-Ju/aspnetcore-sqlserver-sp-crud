# ProductApi — ASP.NET Core Web API + SQL Server Stored Procedure CRUD

🌐 專案介紹：https://tai-ju.github.io/aspnetcore-sqlserver-sp-crud/
⚡ 互動 Demo：https://tai-ju.github.io/aspnetcore-sqlserver-sp-crud/demo.html

一個很小的練習專案：ASP.NET Core 8 Web API，透過 **一支預存程序 `dbo.usp_Products_CRUD`** 對 `dbo.Products` 做新增／查詢／修改／刪除。資料存取用純 ADO.NET（`Microsoft.Data.SqlClient`），不用 EF Core，方便看清楚 SP 怎麼被呼叫。

## 技術

- .NET 8 / ASP.NET Core Web API（Controllers）
- SQL Server（LocalDB 即可）
- Microsoft.Data.SqlClient、Swagger (Swashbuckle)

## 專案結構

```
ProductApi.sln
database/
  01_schema_and_sp.sql      # 建 DB、建表、建 SP、塞範例資料
src/ProductApi/
  Controllers/ProductsController.cs
  Data/ProductRepository.cs # 所有查詢都走 usp_Products_CRUD
  Models/Product.cs
  Program.cs
  ProductApi.http           # VS 內建的 HTTP 測試檔
  wwwroot/index.html        # 連線版 CRUD 頁（API 跑起來後開根網址）
docs/
  index.html                # GitHub Pages：專案介紹
  demo.html                 # GitHub Pages：瀏覽器內互動 Demo（模擬 API 與 SP）
```

## 執行步驟（Visual Studio 2022）

1. 開啟 `ProductApi.sln`
2. **建資料庫**：在 SSMS 或 VS 的「SQL Server 物件總管」連到 `(localdb)\MSSQLLocalDB`，執行 `database/01_schema_and_sp.sql`
3. 連線字串在 `src/ProductApi/appsettings.json` 的 `ConnectionStrings:Default`，預設指向 LocalDB；用其他 SQL Server 改這裡即可
4. F5 執行，瀏覽器會開 demo 頁面（`/`）：可直接新增／編輯／刪除，右側即時顯示每個 HTTP 請求對應執行的 `EXEC dbo.usp_Products_CRUD ...`
5. Swagger 在 `/swagger`；也可以打開 `ProductApi.http` 逐一送 request

> demo 頁面若偵測不到後端（例如直接用瀏覽器開檔案），會自動切換成「展示模式」，用頁面內的模擬資料操作。

## API

| Method | Route | SP @Action | 說明 |
|---|---|---|---|
| GET | `/api/products` | `LIST` | 查全部 |
| GET | `/api/products/{id}` | `GET` | 查一筆，找不到回 404 |
| POST | `/api/products` | `CREATE` | 新增，回 201 + 新資料 |
| PUT | `/api/products/{id}` | `UPDATE` | 修改，找不到回 404 |
| DELETE | `/api/products/{id}` | `DELETE` | 刪除，成功回 204 |

Request body（POST / PUT）：

```json
{ "name": "N95 口罩", "price": 35, "stock": 500 }
```

## SP 設計

`dbo.usp_Products_CRUD` 以 `@Action` 分派五種操作；`CREATE` / `UPDATE` 用 `OUTPUT inserted.*` 直接回傳異動後的資料列，API 不必再查一次。全部參數都以 `SqlParameter` 傳入，沒有字串拼接 SQL。
