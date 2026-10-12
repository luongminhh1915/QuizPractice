namespace QuizPractice.Application.Services.Payments;

/// <summary>A bank of the VietQR list: its 6-digit NAPAS code (BIN) and the short name banking apps show.</summary>
public sealed record VietQrBank(string Bin, string ShortName, string Name);

/// <summary>
/// The banks Bank Account Settings offers (UC-70 step 3). Kept in the code rather than read
/// from api.vietqr.io: the list changes rarely and the page must not depend on an outside
/// service. Add a bank here when the Admin needs one that is missing.
/// </summary>
public static class VietQrBanks
{
    public static readonly IReadOnlyList<VietQrBank> All =
    [
        new("970436", "Vietcombank", "Ngân hàng TMCP Ngoại thương Việt Nam"),
        new("970415", "VietinBank", "Ngân hàng TMCP Công thương Việt Nam"),
        new("970418", "BIDV", "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam"),
        new("970405", "Agribank", "Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam"),
        new("970407", "Techcombank", "Ngân hàng TMCP Kỹ thương Việt Nam"),
        new("970422", "MBBank", "Ngân hàng TMCP Quân đội"),
        new("970416", "ACB", "Ngân hàng TMCP Á Châu"),
        new("970432", "VPBank", "Ngân hàng TMCP Việt Nam Thịnh Vượng"),
        new("970423", "TPBank", "Ngân hàng TMCP Tiên Phong"),
        new("970403", "Sacombank", "Ngân hàng TMCP Sài Gòn Thương Tín"),
        new("970437", "HDBank", "Ngân hàng TMCP Phát triển Thành phố Hồ Chí Minh"),
        new("970441", "VIB", "Ngân hàng TMCP Quốc tế Việt Nam"),
        new("970443", "SHB", "Ngân hàng TMCP Sài Gòn - Hà Nội"),
        new("970426", "MSB", "Ngân hàng TMCP Hàng Hải"),
        new("970448", "OCB", "Ngân hàng TMCP Phương Đông"),
        new("970440", "SeABank", "Ngân hàng TMCP Đông Nam Á"),
        new("970431", "Eximbank", "Ngân hàng TMCP Xuất Nhập khẩu Việt Nam"),
        new("970449", "LPBank", "Ngân hàng TMCP Lộc Phát Việt Nam"),
        new("970428", "NamABank", "Ngân hàng TMCP Nam Á"),
        new("970409", "BacABank", "Ngân hàng TMCP Bắc Á"),
        new("970412", "PVcomBank", "Ngân hàng TMCP Đại Chúng Việt Nam"),
        new("970425", "ABBANK", "Ngân hàng TMCP An Bình"),
        new("970419", "NCB", "Ngân hàng TMCP Quốc Dân"),
        new("970452", "KienLongBank", "Ngân hàng TMCP Kiên Long"),
        new("970438", "BaoVietBank", "Ngân hàng TMCP Bảo Việt"),
        new("970433", "VietBank", "Ngân hàng TMCP Việt Nam Thương Tín"),
        new("970400", "SaigonBank", "Ngân hàng TMCP Sài Gòn Công Thương"),
    ];

    public static VietQrBank? Find(string bin) => All.FirstOrDefault(b => b.Bin == bin);
}
