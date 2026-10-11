# QuizPractice — SWP391 nhóm G2

Hệ thống luyện thi trắc nghiệm (Quiz Practicing System). Backend ASP.NET Core 10 Web API + EF Core 10 + SQL Server; frontend React 19 + TypeScript + Ant Design 6 (thư mục `quizpractice-web/`), gọi API bằng axios + TanStack Query.

Thiết kế nằm ở SRS v1.5 và SDS v1.0 của nhóm. Code bám đúng SDS: kiến trúc ở I.1, package và quy ước đặt tên ở I.2, 19 bảng ở I.3, 5 sequence diagram ở II.

Cả nhóm làm trên **một nhánh `main`**, commit theo dạng `UC-xx: <message tiếng Anh>`, mỗi commit một use case. Chi tiết trong `CONTRIBUTING.md`.

## Cần cài

| Công cụ | Phiên bản | Ghi chú |
|---|---|---|
| .NET SDK | 10.0.1xx trở lên | `global.json` ghim SDK 10 |
| SQL Server | 2022 trở lên, bản Express là đủ | Đăng nhập bằng Windows |
| IDE | Visual Studio 2026, Rider 2025.3+ hoặc VS Code + C# Dev Kit | **VS 2022 không mở được project `net10.0`** |
| Node.js | 22.22 trở lên (khuyên dùng 24 LTS) | React Router 8 đòi tối thiểu 22.22 |

## Chạy lần đầu

```bash
# 1. Cấu hình máy mình (file này không lên Git)
cp src/QuizPractice.Api/appsettings.Development.example.json src/QuizPractice.Api/appsettings.Development.json
#    Sửa "Server=." nếu SQL Server của bạn là .\SQLEXPRESS

# 2. Cài dotnet-ef đúng bản của nhóm (ghi trong dotnet-tools.json)
dotnet tool restore

# 3. Tạo database QuizPracticeDB với đủ 19 bảng
dotnet ef database update --project src/QuizPractice.Infrastructure --startup-project src/QuizPractice.Api

# 4. Chạy API
dotnet run --project src/QuizPractice.Api --launch-profile http
```

Mở `http://localhost:5031/scalar` để thử API trên trình duyệt. Chạy test: `dotnet test`.

### Dữ liệu mẫu và người dùng giả lập

Lần đầu API chạy ở môi trường Development trên database đã migrate mà bảng `Settings` còn trống, nó tự ghi dữ liệu mẫu (`Infrastructure/Persistence/Seed/DevSeedData.cs`). Log in dòng `Sample data seeded: 38 settings, 5 accounts, 2 subjects, 18 questions.` Các lần sau nó bỏ qua.

| Nội dung | Chi tiết |
|---|---|
| Settings | Đủ 8 type (BR-16, D24): 5 role, 14 mục System Menu, post/subject category (Soft Skills để Inactive), Simulation / Lesson quiz, Easy / Medium / Hard, Subject Topic / Lesson / Quiz, Domain / Group |
| Tài khoản | Mỗi role một tài khoản, đều Active, **mật khẩu chung `Qps@12345`** (chỉ dùng khi phát triển) |
| Môn học | *Java Object-Oriented Programming*: giao cho `expert@qps.vn`, featured, 3 chủ đề × 2 bài, 6 dimension, 4 gói giá (gói 1 tháng Inactive), 12 câu hỏi (1 câu Hidden như vừa import). *SQL Fundamentals*: **chưa giao Expert**, 2 chủ đề × 2 bài, 5 dimension, 2 gói, 6 câu |
| Tài khoản nhận tiền | Một dòng `BankAccounts`, số tài khoản giả `0000000000` — Admin sửa ở UC-70 |

| Email | Role |
|---|---|
| `admin@qps.vn` | Admin |
| `expert@qps.vn` | Expert |
| `marketing@qps.vn` | Marketing |
| `sale@qps.vn` | Sale |
| `customer@qps.vn` | Customer |

Chưa có quiz, đăng ký môn, hoá đơn, bài làm, post: mỗi feature tự tạo qua màn của mình.

**Đổi góc nhìn = đăng nhập tài khoản khác.** Bấm **Login** trên header, dùng một tài khoản ở bảng trên. Marketing, Sale, Admin vào Dashboard; Expert vào Questions; Customer ở lại trang đang xem và có menu **My Learning**. Mục `DevUser` cũ trong `appsettings.Development.json` không còn được đọc, xoá đi cũng được.

**Mail ở máy dev.** Mục `Email:Host` trong `appsettings.json` để trống, nên không mail nào ra khỏi máy: mỗi mail (link xác thực, mật khẩu, link đặt lại mật khẩu) là một file `.eml` trong `src/QuizPractice.Api/App_Data/mails/`. Mở bằng Outlook hoặc Notepad rồi bấm link. Muốn gửi thật thì điền `Email:Host`, `Port`, `UserName`, `Password` vào `appsettings.Development.json`, **không** vào `appsettings.json`.

**Muốn seed lại từ đầu** (xoá **toàn bộ** dữ liệu trên máy mình):

```bash
dotnet ef database drop --force --project src/QuizPractice.Infrastructure --startup-project src/QuizPractice.Api
dotnet ef database update --project src/QuizPractice.Infrastructure --startup-project src/QuizPractice.Api
```

Rồi chạy API như thường.

### Chạy hằng ngày trong VS Code: F5

Ba bước ở trên chỉ làm **một lần** trên mỗi máy (cộng `npm install` trong `quizpractice-web`). Từ đó về sau:

1. Mở panel **Run and Debug** (`Ctrl+Shift+D`), chọn **API + Web** ở ô trên cùng.
2. Bấm **F5**. VS Code build backend và chạy Vite cùng lúc, chạy API (cổng 5031) ở chế độ debug, rồi mở `http://localhost:5173` bằng trình duyệt mặc định khi API sẵn sàng (khoảng 10–20 giây).
3. Breakpoint trong file `.cs` dừng được ngay trong VS Code. Code React thì debug bằng công cụ của trình duyệt: **F12 → Sources**.
4. **Shift+F5** tắt cả API lẫn Vite.

**API only** chỉ chạy backend và mở trang thử API `/scalar`. Cấu hình nằm ở `.vscode/launch.json` và `.vscode/tasks.json`. Sửa file `.tsx` thì trình duyệt tự cập nhật; sửa file `.cs` thì Shift+F5 rồi F5.

### Chạy bằng lệnh (không dùng VS Code)

Frontend chạy ở terminal thứ hai, API vẫn chạy ở terminal đầu:

```bash
cd quizpractice-web
npm install        # lần đầu, hoặc khi package.json đổi
npm run dev
```

Mở `http://localhost:5173`. Vite tự chuyển `/api` và `/uploads` sang API ở cổng 5031, nên trình duyệt chỉ thấy một địa chỉ, giống lúc chạy thật. Màn mẫu: `http://localhost:5173/manage/sliders`.

| Lệnh | Việc |
|---|---|
| `npm run dev` | Máy chủ dev, sửa code là trình duyệt tự cập nhật |
| `npm run build` | Kiểm kiểu TypeScript (strict) rồi build ra `dist/` |
| `npm run lint` | oxlint soát code |

## Cấu trúc

```
src/
  QuizPractice.Api/             Controllers/<Feature>/, Program.cs, wwwroot/uploads/
  QuizPractice.Application/     Services/<Feature>/, Dtos/<Feature>/, Validators/<Feature>/, Abstractions/
  QuizPractice.Domain/          Entities/ (19), Enums/, Repositories/, Constants/FieldLength.cs
  QuizPractice.Infrastructure/  Persistence/ (AppDbContext, Configurations/, Migrations/), Repositories/, Storage/
tests/
  QuizPractice.Tests/           Application/, Infrastructure/
quizpractice-web/src/
  pages/<feature>/              XxxPage.tsx, XxxPopup.tsx
  layouts/                      PublicLayout (SRS III 0.2), ManagementLayout (III 0.1)
  components/                   DataTable, ImageUpload, PageHeader, StatusTag (bọc Ant Design), RichTextEditor (TipTap)
  api/                          client.ts (axios dùng chung), xxxApi.ts
  hooks/                        useXxx.ts (TanStack Query), useListParams.ts
  routes/router.tsx             mọi route
```

Phụ thuộc chỉ đi một chiều: Api → Application → Domain. Infrastructure implement các interface của Application và Domain. Application **không bao giờ** tham chiếu Infrastructure hay EF Core.

## Một request đi qua những đâu

```
React page → Controller → Service → Repository → AppDbContext
                 │           │
                 │           └─ validate (FluentValidation) → đổi entity → IUnitOfWork.SaveChangesAsync() một lần
                 └─ map Result → HTTP: 200/201, 400 (lỗi theo field), 404, 409, 403
```

- **Controller** mỏng: nhận request, gọi một service, trả `FromResult(...)`.
- **Service** chứa logic use case và trả `Result<T>`. Lỗi dự kiến (sai dữ liệu, không tìm thấy, xung đột trạng thái) thì trả `Error`, không `throw`.
- **Repository** chỉ đọc hoặc `Add`. Không bao giờ gọi `SaveChanges`; service commit qua `IUnitOfWork`.

## Thêm một tính năng: chép mẫu Sliders

Sliders (UC-38→40) là mẫu đầu-cuối duy nhất trong khung. Muốn làm một cặp màn List + Details mới, tạo các file tương ứng:

| Tầng | File mẫu | Ghi chú |
|---|---|---|
| Domain | `Repositories/ISliderRepository.cs` | Kèm record tiêu chí tìm kiếm |
| Infrastructure | `Repositories/SliderRepository.cs` | Tự đăng ký DI theo tên `XxxRepository` ↔ `IXxxRepository` |
| Application | `Dtos/Marketing/SliderDtos.cs` | Request vào, Dto ra, map viết tay `XxxDto.From(entity)` |
| Application | `Validators/Marketing/SliderValidators.cs` | Độ dài lấy từ `FieldLength`, không gõ số trực tiếp |
| Application | `Services/Marketing/SliderService.cs` | Tự đăng ký DI theo tên `XxxService` ↔ `IXxxService` |
| Api | `Controllers/Marketing/SlidersController.cs` | Route `/api/<số nhiều>`, kế thừa `ApiControllerBase` |
| Tests | `Application/SliderServiceTests.cs` | Fake viết tay, không cần database |
| Web | `api/slidersApi.ts` | Kiểu dữ liệu khớp DTO; form có file thì gửi `FormData` |
| Web | `hooks/useSliders.ts` | Query key `['sliders', …]`; lưu xong thì `invalidateQueries` để danh sách tự tải lại |
| Web | `pages/marketing/SlidersListPage.tsx` | Tìm kiếm (Enter), lọc (áp ngay), `DataTable`, Edit / Show / Hide |
| Web | `pages/marketing/SliderDetailsPage.tsx` | Một form cho cả thêm và sửa; lỗi từ API hiện dưới đúng ô bằng `applyFieldErrors` |
| Web | `routes/router.tsx` | Thêm route của màn mới |

Không phải sửa `Program.cs` hay `DependencyInjection.cs` khi thêm service hoặc repository, nên 5 người không đụng nhau ở các file đó.

**Quy tắc frontend:** chỉ dùng control của Ant Design và các component trong `components/`; không viết CSS riêng cho màn của mình. Bảng danh sách luôn là `DataTable`, trạng thái luôn là `StatusTag`, nên 45 màn trông như một hệ thống.

**Nội dung HTML** (nội dung post, mô tả môn, bài học HTML, giải thích câu hỏi): nhập bằng `RichTextEditor`, và service **luôn** gọi `IHtmlSanitizer.Sanitize` trước khi lưu, như `PostService`. Bộ lọc chỉ giữ H2–H4, đậm/nghiêng/gạch, danh sách, trích dẫn, khối code, link http/https/mailto và ảnh trong `/uploads/post-content/`; mọi thứ khác (script, `on…`, `style`, iframe, ảnh từ site khác) bị bỏ. Hiển thị HTML đã lọc này ở trang public thì dùng `dangerouslySetInnerHTML` được, vì nó chỉ chứa thứ bộ lọc cho qua.

## Quy tắc database

- **19 bảng và mọi cột đã có sẵn** trong migration `InitialCreate`, khớp từng cột với SDS I.3: tên, kiểu, độ dài, NULL. `AppDbContext` cũng đã có đủ 19 `DbSet`. Bạn không cần tạo bảng.
- Tên cột theo SRS (snake_case), do `NamingConvention` tự đổi: `PassRate` → `pass_rate`, `Id` → `subject_id`. Cột lệch quy tắc thì đặt tay trong `Configurations/` (vd `RoleId` → `role`).
- Trạng thái lưu bằng chữ của BR-23 (`'In Progress'`, `'Shown'`), mỗi cột có CHECK. Trong C# dùng enum ở `Domain/Enums/`.
- Khoá ngoại là `ON DELETE NO ACTION`, trừ `QuizQuestions` và `QuizRules`.
- **Đổi schema phải báo nhóm trước.** Mỗi lần chỉ một người tạo migration, và chỉ sau khi `git pull --rebase` lấy migration mới nhất về. Hai migration tạo song song sẽ xung đột ở `AppDbContextModelSnapshot.cs`.
- Giá trị setting mà code phải nhận ra (role, loại bài học Subject Topic / Lesson / Quiz, loại quiz Simulation / Lesson quiz) là hằng số ở `Domain/Constants/Roles.cs` và `SettingValues.cs`. So với hằng số, đừng gõ chuỗi. Đổi tên các giá trị này ở màn Settings là hỏng hành vi đọc chúng.
- Độ dài và giới hạn của BR-17 nằm ở `Domain/Constants/FieldLength.cs`. Thầy duyệt OI-11 mà đổi số thì sửa ở đó một lần rồi tạo migration.

## Đăng nhập và phân quyền

- **Cookie** `qps.auth` (HttpOnly, đóng trình duyệt là hết, 8 giờ tự gia hạn khi đang dùng). Mỗi request API kiểm lại tài khoản còn Active và đúng role; Admin khoá tài khoản là người đó bị đăng xuất ngay.
- **Antiforgery.** Mọi POST/PUT/DELETE phải có header `X-XSRF-TOKEN`. `GET /api/account/me` phát token qua cookie `XSRF-TOKEN`, axios (`api/client.ts`) tự gửi, màn hình không phải làm gì. Gọi API bằng script thì GET `/api/account/me` trước rồi copy cookie `XSRF-TOKEN` vào header.
- **Bảng phân quyền** ở mục `Permissions` của `appsettings.json`: mỗi mục System Menu → các role được vào (F#4, D23). Mỗi mục là một policy cùng tên:

  ```csharp
  [Authorize(Policy = SettingValues.MenuItems.Registrations)]
  public class RegistrationsController ...
  ```

  Menu của role = mục System Menu đang Active ∩ bảng này. Gõ URL màn không có trong menu thì web báo "Your role cannot open this screen" và API trả 403. Sai tên mục hay tên role trong bảng thì API dừng ngay lúc khởi động.
- **Làm màn mới:** gắn `[Authorize(Policy = ...)]` lên controller, thêm route vào `router.tsx`, rồi đặt `ready: true` cho mục đó trong `quizpractice-web/src/constants/menu.tsx` (mục chưa `ready` vẫn hiện trong menu, mờ, kèm chữ "coming soon").
- Service hỏi "ai đang gọi" qua `ICurrentUser`, không nhận user id từ client.
- **Link trong mail** không lưu trong database: token ký bằng Data Protection, khoá nằm ở `App_Data/keys/` (không lên Git, xoá thư mục này là mọi link đã gửi và mọi phiên đăng nhập đều hết hiệu lực). Hạn link ở mục `Links` của `appsettings.json` (BR-01, BR-02).
- Login, register và hai loại link bị giới hạn **10 lần/phút mỗi IP**; quá thì nhận 429.

## Upload file

- Ảnh public lưu ở `wwwroot/uploads/<loại>/<guid>.png`, database chỉ giữ đường dẫn tương đối. Thư mục này không lên Git.
- Chỉ nhận JPG/PNG tối đa 2 MB (BR-17). `ImageUploadValidator` kiểm cả **chữ ký byte đầu file**, không tin phần mở rộng, nên file SVG/HTML đổi tên thành `.png` bị từ chối.
- Form có file gửi `multipart/form-data`. PUT gửi lại **đủ mọi field**: field bỏ trống nghĩa là xoá giá trị đó (vd backlink); riêng ảnh bỏ trống thì giữ ảnh cũ.

## Chưa có trong khung

- API host chưa phục vụ bản build React (`dist/`); hiện chỉ chạy kiểu dev qua Vite. Thêm khi chuẩn bị bản demo.
- Bundle 1,36 MB (gzip 436 KB) vì Ant Design; khi số màn tăng thì tải route theo kiểu lazy để chia nhỏ.
