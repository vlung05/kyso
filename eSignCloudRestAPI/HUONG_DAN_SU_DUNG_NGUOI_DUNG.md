# HƯỚNG DẪN SỬ DỤNG HỆ THỐNG KÝ SỐ ĐIỆN TỬ (eSignCloud Portal)
**Dành cho Người Dùng Ký Số / Khách Hàng (Non-Admin)**

---

## 1. TỔNG QUAN HỆ THỐNG

**eSignCloud Portal** là cổng dịch vụ hỗ trợ người dùng tải lên và thực hiện ký số điện tử trên các định dạng tài liệu số thông qua giải pháp Chữ ký số từ xa (*Remote Signing - RSSP*). 

Hệ thống cho phép:
* Ký số trực tuyến cho tài liệu định dạng **PDF** (`.pdf`) và **Microsoft Word** (`.doc`, `.docx`).
* Tùy chỉnh linh hoạt vị trí hiển thị con dấu / chữ ký trên tài liệu.
* Tra cứu, xem trực tiếp (preview), kiểm tra chứng thư số và tải về các tài liệu đã ký hoàn tất.

---

## 2. ĐĂNG NHẬP VÀO HỆ THỐNG

### 2.1. Đăng nhập với mã UID & Passcode

1. Mở trình duyệt web và truy cập vào địa chỉ Cổng ký số **eSignCloud Portal**.
2. Tại màn hình Đăng nhập, điền đầy đủ các thông tin:
   * **UID (`agreementUUID`)**: Mã định danh tài khoản ký số cá nhân/doanh nghiệp được cấp (Ví dụ: `562449E1-090C-4573-B978-76A740B9F50A`).
   * **Passcode ký số**: Mật mã PIN bảo mật cho chữ ký số (nhấn biểu tượng con mắt 👁️ để ẩn/hiện mật mã khi nhập).
3. Nhấn nút **"ĐĂNG NHẬP VÀO HỆ THỐNG"**.

---

### 2.2. Xử lý khi Quên hoặc Muốn Đổi Passcode

* **Đổi Passcode**:
  1. Nhấn vào liên kết **"Đổi Passcode"** ở dưới nút Đăng nhập.
  2. Điền mã **UID**, **Passcode cũ** và **Passcode mới**.
  3. Nhấn **"Xác Nhận Đổi Passcode"**.
* **Quên Passcode**:
  1. Nhấn vào liên kết **"Quên Passcode?"**.
  2. Điền mã **UID** của bạn.
  3. Nhấn **"Gửi Yêu Cầu Khôi Phục"** để hệ thống gửi hướng dẫn khôi phục qua email hoặc số điện thoại đã liên kết.

---

## 3. GIAO DIỆN LÀM VIỆC (DASHBOARD)

Sau khi đăng nhập, màn hình làm việc được chia thành các khu vực chính:

```
┌────────────────────────────────────────────────────────────────────────┐
│ [Logo] eSignCloud Portal                [Tên Chủ Tài Khoản] [Đăng xuất]│
├────────────────────────────────────┬───────────────────────────────────┤
│ [Box 1] TẢI LÊN & GỬI KÝ TÀI LIỆU │ [Box 3] TIN TỨC & QUẢNG CÁO       │
│ - Khu vực kéo/thả file             │ - Slide hiển thị thông báo,      │
│ - Tùy chỉnh vị trí ký (Metadata)   │   tính năng mới của dịch vụ       │
│ - Nút "GỬI KÝ SỐ TÀI LIỆU"         │                                   │
├────────────────────────────────────┴───────────────────────────────────┤
│ [Box 2] DANH SÁCH TÀI LIỆU ĐÃ KÝ CỦA TÀI KHOẢN (LỊCH SỬ)               │
│ - Tìm kiếm tài liệu, Xem trước (PDF), Tải về máy, Chi tiết Chứng thư   │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 4. HƯỚNG DẪN KÝ SỐ TÀI LIỆU (BOX 1)

### Bước 1: Chọn hoặc tải file cần ký
* **Cách 1**: Kéo và thả tệp trực tiếp từ máy tính vào ô **"Kéo & Thả tài liệu cần ký vào đây"**.
* **Cách 2**: Nhấp chuột vào khu vực khung nét đứt để chọn tệp từ máy tính.
* **Định dạng cho phép**: Tệp PDF (`.pdf`), Tệp Word (`.docx`, `.doc`).

> *Sau khi chọn file thành công, thẻ thông tin tên file và dung lượng sẽ xuất hiện. Nếu chọn nhầm file, bạn có thể nhấn nút **dấu X** để chọn lại.*

---

### Bước 2 (Tùy chọn): Tùy chỉnh vị trí & thông tin chữ ký
Nhấn vào thanh **"Tùy chỉnh vị trí & thông tin chữ ký"** để mở rộng cài đặt nếu cần:

| Tham số | Giá trị gợi ý | Ý nghĩa |
| :--- | :--- | :--- |
| **Căn lề vị trí ký (`ALIGNMENT`)** | `center-below` | **`🎯 Căn giữa dưới từ khóa`** *(Mặc định văn bản)*: Tự động căn giữa con dấu ngay dưới từ khóa.<br>**`⬅️ Căn lề trái dưới từ khóa`**: Thẳng hàng theo lề trái của từ khóa.<br>**`➡️ Căn lề phải dưới từ khóa`**: Thẳng hàng theo lề phải của từ khóa.<br>**`👉 Nằm bên phải từ khóa`**: Con dấu nằm ngang hàng bên phải từ khóa.<br>**`⬆️ Nằm phía trên từ khóa`**: Con dấu nằm phía trên từ khóa.<br>**`Các góc trang`**: `Góc dưới-phải`, `Góc dưới-trái`, `Dưới-chính giữa`, `Chính giữa trang`... (khi ký theo trang). |
| **Số trang ký (`PAGENO`)** | `-1` hoặc `ALL` | **`-1`**: Ký ở trang cuối cùng *(Mặc định)*.<br>**`ALL`** hoặc `*`: Ký tại **TẤT CẢ các trang** có chứa từ khóa vị trí.<br>**`1,2,5`**: Ký trên các trang cụ thể được liệt kê.<br>**`1-4`**: Ký trên dải trang liên tiếp (từ trang 1 đến 4). |
| **Từ khóa vị trí (`POSITIONIDENTIFIER`)** | `ĐẠI DIỆN BÊN B` | Từ khóa trong văn bản để hệ thống tự động dò tìm vị trí đặt chữ ký (ví dụ: `ĐẠI DIỆN BÊN B`, `(Ký tên, đóng dấu)`). |
| **Độ lệch tinh chỉnh (`RECTANGLEOFFSET`)** | `0,0` | Tinh chỉnh thêm tọa độ X,Y sau khi đã căn lề (*X: âm sang trái, dương sang phải; Y: âm xuống dưới, dương lên trên*). |
| **Kích thước khung ký (`RECTANGLESIZE`)** | `170,70` | Chiều rộng và chiều cao khung chữ ký hiển thị (*Width, Height*). |
| **Lý do ký (`SIGNREASON`)** | *(Tùy chọn)* | Ví dụ: *Đồng ý ký duyệt, Xác nhận thanh toán...* |
| **Địa điểm (`LOCATION`)** | *(Tùy chọn)* | Ví dụ: *Hà Nội, TP. Hồ Chí Minh...* |

#### 💡 Mẹo: Hướng Dẫn Ký Nhiều Trang Với Từ Khóa Vị Trí
* **Ký duyệt tất cả các trang có từ khóa (ví dụ nhiều phụ lục hoặc hợp đồng nhiều bên)**:
  * Đặt `PAGENO = ALL`
  * Đặt `POSITIONIDENTIFIER = (Ký tên, đóng dấu)` hoặc `ĐẠI DIỆN BÊN B`.
  * Hệ thống sẽ tự động dò tìm mọi trang có chứa từ khóa đó và đóng dấu chữ ký vào tất cả các vị trí tìm thấy.
* **Ký nháy (paraf) trên mọi trang của tài liệu**:
  * Đặt `PAGENO = ALL`
  * Để trống hoặc đặt từ khóa ở chân trang.
* **Ký trên các trang chọn lọc**:
  * Đặt `PAGENO = 1,3,5` (hoặc `1-3`) kết hợp với từ khóa vị trí.

---

### Bước 3: Thực hiện ký số
1. Nhấn nút **"GỬI KÝ SỐ TÀI LIỆU"**.
2. Hệ thống sẽ kết nối đến dịch vụ Cloud RSSP và tiến hành ký bảo mật cho tài liệu.
3. Khi ký thành công:
   * Thông báo thành công sẽ hiển thị.
   * Cửa sổ xem trước (Preview) file PDF đã ký sẽ tự động mở lên để bạn kiểm tra chữ ký.
   * Bản ghi sẽ tự động được thêm vào danh sách tại **Box 2**.

---

## 5. QUẢN LÝ DANH SÁCH TÀI LIỆU ĐÃ KÝ (BOX 2)

Danh sách này lưu lại toàn bộ các tài liệu đã ký của tài khoản hiện tại. Các tính năng bao gồm:

* **Tìm kiếm nhanh**: Nhập tên file, mã Bill, hoặc lý do ký vào ô *"Tìm kiếm tài liệu..."* ở góc trên bên phải bảng.
* **Xem trước (Xem)**: Nhấn nút **"Xem"** (đối với file PDF) để xem toàn bộ nội dung và vị trí con dấu ký số trực tiếp trên trình duyệt.
* **Tải về (Tải)**: Nhấn nút **"Tải"** để lưu file đã ký về máy tính cá nhân.
* **Chi tiết Chứng thư số (Biểu tượng Certificate)**: Xem thông tin kỹ thuật của chứng thư số đã dùng để ký (*Chủ thể, Serial Number, Đơn vị chứng thực CA, Mã Bill*).
* **Xóa bản ghi (Biểu tượng Thùng rác)**: Xóa thông tin tài liệu khỏi danh sách hiển thị trên Cổng web.

---

## 6. ĐĂNG XUẤT AN TOÀN

Sau khi hoàn tất công việc ký số:
* Nhấn nút **"Đăng xuất"** màu đỏ ở góc trên cùng bên phải màn hình để đóng phiên làm việc, đảm bảo an toàn cho chữ ký số của bạn.

---

## 7. MỘT SỐ CÂU HỎI THƯỜNG GẶP (FAQ)

### Q1: Tôi có thể ký cùng lúc nhiều file không?
> **Trả lời**: Hiện tại hệ thống hỗ trợ ký từng tài liệu một để đảm bảo kiểm soát chính xác vị trí và nội dung của từng văn bản.

### Q2: Chữ ký số trên hệ thống có giá trị pháp lý không?
> **Trả lời**: Có. Hệ thống sử dụng dịch vụ chứng thực chữ ký số từ xa chuẩn Mobile-ID RSSP, đáp ứng đầy đủ tiêu chuẩn eIDAS và Thông tư 16/2019/BTTTT của Bộ Thông tin & Truyền thông.

### Q3: Báo lỗi "Passcode không chính xác" hoặc "Tài khoản bị khóa" thì làm sao?
> **Trả lời**: Hãy kiểm tra lại mã PIN Passcode hoặc sử dụng tính năng **"Quên Passcode"**. Trường hợp vẫn không được, vui lòng liên hệ Quản trị viên để kiểm tra trạng thái kích hoạt tài khoản của bạn.
