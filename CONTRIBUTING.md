# Quy trình làm việc với Git — nhóm G2

## Một nhánh duy nhất: `main`

Cả nhóm commit thẳng lên `main`, không tạo nhánh riêng. Vì 5 người cùng đẩy vào một nhánh, **luôn lấy code mới về trước khi đẩy lên**:

```bash
git pull --rebase          # đầu buổi, và ngay trước mỗi lần push
# ... làm, commit ...
git pull --rebase          # gặp xung đột thì sửa file, git add <file>, git rebase --continue
git push
```

`--rebase` đặt commit của bạn lên sau commit của người khác, lịch sử thành một đường thẳng, không có commit "Merge branch…".

## Ai làm phần nào

Chia theo 9 feature của file Excel thầy, khớp cột In Charge ở ProjectTracking:

| Người | Feature | Use case |
|---|---|---|
| Kiệt | Common, Public | UC-01→14: đăng nhập, đăng ký, mật khẩu, hồ sơ, trang chủ, blog, môn học public, đăng ký môn |
| Tùng | Customer, Sale | UC-15→23, UC-67 · UC-59→61, UC-68, UC-69 |
| Hiếu | Learning | UC-24→33: Quiz Handle, Quiz Review |
| Minh | Course Content, Admin | UC-41→51 · UC-62→66, UC-70 |
| Đức | Test Content, Marketing | UC-52→58 · UC-34→40 |

Mỗi người chỉ thêm file trong thư mục `<Feature>` của mình ở `Controllers/`, `Services/`, `Dtos/`, `Validators/`, `pages/`. Cần sửa file dùng chung (`Domain/Entities`, `Configurations`, `ApiControllerBase`, `Result`, `components/`) thì báo nhóm trước và commit riêng.

## Định dạng commit

**Commit message viết bằng tiếng Anh.**

| Loại commit | Định dạng |
|---|---|
| Làm cho một use case | `UC-xx: <what changed>` |
| Phần dùng chung, không thuộc use case nào | `<feat\|fix\|chore>-common: <setup\|database\|seed\|docs\|dev>: <what changed>` |

Quy tắc:

- **Một commit chỉ thuộc một use case.** Không viết `UC-35, UC-36: …`. Một file phục vụ nhiều UC (service, controller, màn danh sách) thì thêm dần: commit của UC-35 chỉ thêm phần của UC-35, commit của UC-36 thêm phần của UC-36.
- **Một use case thường có 3–4 commit cùng mã UC**, ví dụ: repository + DTO + validator → service + controller + test → màn hình. Không cần chia nhỏ hơn.
- **Message nói rõ thêm hay sửa gì**, không viết chung chung kiểu `UC-36: update post`.
- Số UC hai chữ số như SRS: `UC-09`, `UC-38`.

Ví dụ:

```
UC-38: add slider search repository, list request and list validator
UC-38: add SliderService list, GET /api/sliders endpoint and unit test
UC-38: add Sliders List page with search, status filter, sorting and paging
UC-61: fix status change of a paid registration being accepted (BR-22)
feat-common: database: add AddQuizRuleIndex migration
chore-common: docs: add README section on running the front end
```

## Mỗi commit phải build được

Trước khi commit: `dotnet build` và `dotnet test`; có sửa web thì thêm `npm run build` trong `quizpractice-web`. File do máy sinh (`package-lock.json`, migration của EF) đi cùng đúng thay đổi sinh ra nó, ghi rõ trong message. Không bao giờ thêm dòng `Co-Authored-By`.

## Database

- Đổi schema phải báo nhóm trước. Mỗi lần chỉ một người tạo migration, và chỉ sau khi `git pull --rebase` lấy migration mới nhất về. Hai migration tạo song song sẽ xung đột ở `AppDbContextModelSnapshot.cs`.
- Không sửa migration đã push. Muốn đổi thì tạo migration mới.

## Không bao giờ

- Commit `appsettings.Development.json`, mật khẩu, chuỗi kết nối thật, khoá API.
- `git push --force` lên `main`.
- Commit thư mục `bin/`, `obj/`, `node_modules/`, `wwwroot/uploads/` (đã có trong `.gitignore`).
