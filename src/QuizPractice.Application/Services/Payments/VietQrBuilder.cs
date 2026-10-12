using System.Globalization;
using System.Text;

namespace QuizPractice.Application.Services.Payments;

/// <summary>
/// Builds the text of a VietQR code (SDS, Payments): the EMVCo QR format that every
/// Vietnamese banking app scans to fill a transfer to an account, with the amount and
/// the transfer content already in place (UC-18 step 6). Plain code, no library: the page
/// draws the QR image from this text.
///
/// Each field is ID (2 digits) + length (2 digits) + value; the last field is the
/// CRC-16/CCITT-FALSE checksum of everything before it, "6304" included.
/// </summary>
public static class VietQrBuilder
{
    /// <summary>NAPAS id that marks the account block as VietQR.</summary>
    private const string NapasGuid = "A000000727";

    /// <summary>Service code: transfer to an account number (not to a card).</summary>
    private const string ToAccount = "QRIBFTTA";

    public static string Build(string bankBin, string accountNumber, decimal amount, string content)
    {
        var beneficiary = Field("00", bankBin) + Field("01", accountNumber);
        var account = Field("00", NapasGuid) + Field("01", beneficiary) + Field("02", ToAccount);

        var payload = new StringBuilder()
            .Append(Field("00", "01"))                    // payload format version
            .Append(Field("01", "12"))                    // dynamic: made for one payment, with an amount
            .Append(Field("38", account))                 // receiving account
            .Append(Field("53", "704"))                   // currency: VND
            .Append(Field("54", decimal.Truncate(amount).ToString(CultureInfo.InvariantCulture)))
            .Append(Field("58", "VN"))
            .Append(Field("62", Field("08", content)))    // transfer content: the invoice code
            .Append("6304")
            .ToString();
        return payload + Crc16(payload).ToString("X4", CultureInfo.InvariantCulture);
    }

    private static string Field(string id, string value) =>
        id + value.Length.ToString("00", CultureInfo.InvariantCulture) + value;

    /// <summary>CRC-16/CCITT-FALSE: polynomial 0x1021, start 0xFFFF, no reflection ("123456789" gives 29B1).</summary>
    private static ushort Crc16(string text)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(text))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }

        return crc;
    }
}
