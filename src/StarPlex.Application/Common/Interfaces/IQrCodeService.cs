namespace StarPlex.Application.Common.Interfaces;

public interface IQrCodeService
{
    byte[] GenerateQrCode(string text);
}